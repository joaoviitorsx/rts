#!/usr/bin/env python3
"""Reads playtest session CSVs (user://playtest/session_*.csv) and writes docs/reports/playtest_<date>.md.

Per session and on average: time to the first construction, the first decree suggestion, the first accepted /
refused delegation, crises and when they happened, clicks (commands) per minute over time, the longest stretches
without a decision, families lost and decrees active at the end — the GDD §8.3 / UI_UX_guide §9.3 metrics.

Formats (see src/Ironvale.Sim/Scripting/SessionCsv.cs):
  v1  playtest-2A candidate build: command + event rows only. Crises are not logged (only families leaving, with
      the reason); decrees at the end are estimated from the commands.
  v2  adds a meta row, a daily state row (population, decrees, CA, stocks) and crisis rows.

Usage:
  python3 tools/analyze_playtest.py SESSIONS_DIR_OR_FILES... [--out docs/reports/playtest_2026-10-10.md] [--title TEXT]
  Windows sessions: %APPDATA%\\Godot\\app_userdata\\Ironvale\\playtest  ·  Linux: ~/.local/share/godot/app_userdata/Ironvale/playtest
"""
from __future__ import annotations

import argparse
import csv
import datetime as dt
import re
import statistics
import sys
from dataclasses import dataclass, field
from pathlib import Path

WINDOW_S = 5 * 60          # clicks/min buckets
GAP_REPORT = 3             # longest gaps listed per session
DECISION_GAP_S = 120       # feel rule: never more than 2 min without a relevant decision
CRISES = {"firewood": "Crise 1 — lenha", "tools": "Crise 2 — ferramentas", "winter_hunger": "Crise 3 — fome no inverno"}


@dataclass
class Row:
    t: float
    day: int
    kind: str
    detail: str

    @property
    def command(self) -> str:
        return self.detail.split(" ", 1)[0] if self.kind == "command" else ""


@dataclass
class Session:
    name: str
    rows: list[Row]
    version: int = 1
    duration: float = 0.0
    first_build: float | None = None
    first_suggestion: float | None = None
    first_accept: float | None = None
    first_refuse: float | None = None
    first_manual_decree: float | None = None
    suggestions: int = 0
    accepted: int = 0
    refused: int = 0
    crises: dict[str, float] = field(default_factory=dict)
    lost: list[tuple[float, str]] = field(default_factory=list)
    decrees_end: int | None = None
    decrees_estimated: bool = False
    clicks_per_min: list[tuple[int, float]] = field(default_factory=list)
    gaps: list[tuple[float, float]] = field(default_factory=list)
    gaps_over_limit: int = 0


def read(path: Path) -> Session:
    with path.open(newline="", encoding="utf-8") as f:
        rows = [Row(float(r["real_s"]), int(r["game_day"]), r["kind"], r["detail"]) for r in csv.DictReader(f)]
    s = Session(path.stem, rows)
    meta = next((r for r in rows if r.kind == "meta"), None)
    if meta and (m := re.search(r"format=(\d+)", meta.detail)):
        s.version = int(m.group(1))
    return s


def first(rows: list[Row], pred) -> float | None:
    return next((r.t for r in rows if pred(r)), None)


def analyze(s: Session) -> Session:
    rows = s.rows
    s.duration = rows[-1].t if rows else 0.0
    commands = [r for r in rows if r.kind == "command"]
    rejected = [r.detail.split(" ")[1] for r in rows if r.kind == "event" and r.detail.startswith("rejected ")]

    s.first_build = first(commands, lambda r: r.command == "PlaceBuilding")
    s.first_suggestion = first(rows, lambda r: r.kind == "event" and r.detail.startswith("suggestion_offered"))
    s.first_accept = first(commands, lambda r: r.command == "AcceptSuggestion")
    s.first_refuse = first(commands, lambda r: r.command == "DismissSuggestion")
    s.first_manual_decree = first(commands, lambda r: r.command == "CreatePolicy")
    s.suggestions = sum(1 for r in rows if r.kind == "event" and r.detail.startswith("suggestion_offered"))
    s.accepted = sum(1 for r in commands if r.command == "AcceptSuggestion")
    s.refused = sum(1 for r in commands if r.command == "DismissSuggestion")

    for r in rows:
        if r.kind == "crisis" and r.detail in CRISES and r.detail not in s.crises:
            s.crises[r.detail] = r.t
        if r.kind == "event" and r.detail.startswith("household_left"):
            parts = r.detail.split(" ")
            s.lost.append((r.t, parts[-1] if len(parts) > 2 else "?"))
    # v1 has no crisis rows: families leaving from hunger/cold in winter is the only crisis signal.
    if s.version < 2:
        for t, reason in s.lost:
            if reason in ("fome", "frio"):
                s.crises.setdefault("winter_hunger", t)

    states = [r for r in rows if r.kind == "state" and r.detail.startswith("pop=")]
    if states and (m := re.search(r"decrees=(\d+)", states[-1].detail)):
        s.decrees_end = int(m.group(1))
    else:
        created = sum(1 for r in commands if r.command in ("CreatePolicy", "AcceptSuggestion"))
        removed = sum(1 for r in commands if r.command == "RemovePolicy")
        failed = sum(1 for c in rejected if c in ("CreatePolicy", "AcceptSuggestion"))
        s.decrees_end = max(0, created - failed - removed)
        s.decrees_estimated = True

    buckets: dict[int, int] = {}
    for r in commands:
        buckets[int(r.t // WINDOW_S)] = buckets.get(int(r.t // WINDOW_S), 0) + 1
    last_bucket = int(s.duration // WINDOW_S)
    s.clicks_per_min = [(b * WINDOW_S // 60, buckets.get(b, 0) / (WINDOW_S / 60)) for b in range(last_bucket + 1)]

    times = [0.0] + [r.t for r in commands] + [s.duration]
    gaps = sorted(((b - a, a) for a, b in zip(times, times[1:])), reverse=True)
    s.gaps = [(g, start) for g, start in gaps[:GAP_REPORT] if g > 0]
    s.gaps_over_limit = sum(1 for g, _ in gaps if g > DECISION_GAP_S)
    return s


def mmss(t: float | None) -> str:
    if t is None:
        return "—"
    return f"{int(t // 60)}:{int(t % 60):02d}"


def mean(values) -> str:
    v = [x for x in values if x is not None]
    return mmss(statistics.mean(v)) + (f" ({len(v)})" if v else "") if v else "—"


def report(sessions: list[Session], title: str) -> str:
    out = [f"# {title}", "",
           f"> Gerado por `tools/analyze_playtest.py` em {dt.date.today():%d/%m/%Y} · {len(sessions)} sessão(ões).",
           "> Tempos em min:seg desde o início da sessão (tempo real; nas sessões roteirizadas, tempo de jogo a 1x).",
           "> Clique = comando enviado ao jogo. Delegação = decreto aceito de uma sugestão ou criado à mão (tecla P).", ""]
    if any(s.version < 2 for s in sessions):
        out += ["> **Formato v1** (build candidato): crises 1 e 2 não são registradas; a crise 3 aparece só se famílias foram",
                "> embora de fome/frio. Decretos no fim são estimados pelos comandos.", ""]

    out += ["## Resumo por sessão", "",
            "| Sessão | Duração | 1ª construção | 1ª sugestão | 1ª aceita | 1ª recusada | 1º decreto à mão | Sugestões (aceitas/recusadas) | Crises | Famílias perdidas | Decretos no fim | Trechos > 2 min sem decisão |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for s in sessions:
        crises = ", ".join(f"{k.split('_')[0]} {mmss(t)}" for k, t in sorted(s.crises.items(), key=lambda kv: kv[1])) or "—"
        lost = f"{len(s.lost)}" + (f" (1ª {mmss(s.lost[0][0])}, {', '.join(sorted({r for _, r in s.lost}))})" if s.lost else "")
        dec = "—" if s.decrees_end is None else f"{s.decrees_end}{' (estim.)' if s.decrees_estimated else ''}"
        out.append(f"| {s.name} | {mmss(s.duration)} | {mmss(s.first_build)} | {mmss(s.first_suggestion)} | "
                   f"{mmss(s.first_accept)} | {mmss(s.first_refuse)} | {mmss(s.first_manual_decree)} | "
                   f"{s.suggestions} ({s.accepted}/{s.refused}) | {crises} | {lost} | {dec} | {s.gaps_over_limit} |")

    accept_total = sum(s.accepted for s in sessions)
    answered = accept_total + sum(s.refused for s in sessions)
    out += ["", "## Médias", "",
            "| Métrica | Média (sessões com o evento) | Alvo |", "|---|---|---|",
            f"| 1ª construção | {mean(s.first_build for s in sessions)} | — |",
            f"| 1ª sugestão de decreto | {mean(s.first_suggestion for s in sessions)} | ~15–25 min (GDD v0.2 §4.2) |",
            f"| 1ª delegação aceita | {mean(s.first_accept for s in sessions)} | — |",
            f"| 1ª recusada | {mean(s.first_refuse for s in sessions)} | — |",
            f"| Sugestões aceitas | {accept_total}/{answered} ({(100 * accept_total / answered) if answered else 0:.0f}%) | > 50% (guia §9.3) |",
            f"| Decretos ativos no fim | {statistics.mean([s.decrees_end or 0 for s in sessions]):.1f} | ≥ 3 em ~60 min (GDD §8.3) |",
            f"| Crises por sessão | {statistics.mean([len(s.crises) for s in sessions]):.1f} | ≥ 2 em 60 min (GDD §8.3) |",
            f"| Famílias perdidas por sessão | {statistics.mean([len(s.lost) for s in sessions]):.1f} | — |",
            f"| Trechos > 2 min sem decisão por sessão | {statistics.mean([s.gaps_over_limit for s in sessions]):.1f} | 0 (feel) |"]
    for key, label in CRISES.items():
        out.append(f"| {label} | {mean(s.crises.get(key) for s in sessions)} | — |")

    out += ["", "## Cliques por minuto ao longo do tempo (janelas de 5 min)", "",
            "Alvo: **cair** ao longo da partida (guia §9.3 / GDD v0.2 §8).", ""]
    width = max(len(s.clicks_per_min) for s in sessions)
    out.append("| Sessão | " + " | ".join(f"{i * 5}–{i * 5 + 5}" for i in range(width)) + " |")
    out.append("|---|" + "---|" * width)
    for s in sessions:
        cells = [f"{c:.1f}" for _, c in s.clicks_per_min] + [""] * (width - len(s.clicks_per_min))
        out.append(f"| {s.name} | " + " | ".join(cells) + " |")

    out += ["", "## Maiores períodos sem decisão", ""]
    for s in sessions:
        out.append(f"- **{s.name}:** " + "; ".join(f"{g / 60:.1f} min a partir de {mmss(start)}" for g, start in s.gaps))
    return "\n".join(out) + "\n"


def collect(paths: list[str]) -> list[Path]:
    files: list[Path] = []
    for p in map(Path, paths):
        files += sorted(p.glob("*.csv")) if p.is_dir() else [p]
    return files


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("inputs", nargs="+")
    ap.add_argument("--out", default=f"docs/reports/playtest_{dt.date.today():%Y-%m-%d}.md")
    ap.add_argument("--title", default="Relatório de playtest")
    args = ap.parse_args()
    files = collect(args.inputs)
    if not files:
        print("no session CSVs found", file=sys.stderr)
        return 1
    sessions = [analyze(read(f)) for f in files]
    Path(args.out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.out).write_text(report(sessions, args.title), encoding="utf-8")
    print(f"{len(sessions)} sessions -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
