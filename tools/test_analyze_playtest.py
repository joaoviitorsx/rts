#!/usr/bin/env python3
"""Tests for tools/analyze_playtest.py (stdlib unittest): python3 tools/test_analyze_playtest.py"""
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import analyze_playtest as ap  # noqa: E402

V1 = '''real_s,game_day,kind,detail
0.0,60,command,"PlaceBuilding { DefId = house }"
30.0,61,command,"AssignHousehold { HouseholdId = 2, BuildingId = 1 }"
400.0,150,event,"suggestion_offered 99"
410.0,150,command,"AcceptSuggestion { SuggestionId = 99 }"
500.0,170,command,"CreatePolicy { DefId = keep_above }"
501.0,170,event,"rejected CreatePolicy já existe"
900.0,270,event,"household_left Roth frio"
'''

V2 = '''real_s,game_day,kind,detail
0.0,60,meta,"format=2 scenario=mvp_start seed=42"
10.0,62,command,"PlaceBuilding { DefId = field }"
480.0,180,crisis,"firewood"
600.0,210,command,"DismissSuggestion { SuggestionId = 5, Forever = False }"
700.0,235,state,"pop=6 decrees=2 ca=2/4 food=100 firewood=50 tools=6"
'''


def session(text: str, name: str):
    d = Path(tempfile.mkdtemp())
    f = d / f"{name}.csv"
    f.write_text(text, encoding="utf-8")
    return ap.analyze(ap.read(f))


class AnalyzerTests(unittest.TestCase):
    def test_v1_candidate_format(self):
        s = session(V1, "v1")
        self.assertEqual(s.version, 1)
        self.assertEqual(s.first_build, 0.0)
        self.assertEqual(s.first_suggestion, 400.0)
        self.assertEqual(s.first_accept, 410.0)
        self.assertEqual((s.suggestions, s.accepted, s.refused), (1, 1, 0))
        self.assertEqual(s.decrees_end, 1)            # accepted + created - rejected
        self.assertTrue(s.decrees_estimated)
        self.assertEqual(s.crises, {"winter_hunger": 900.0})   # only signal in v1: leaving from cold/hunger
        self.assertEqual(len(s.lost), 1)
        self.assertGreaterEqual(s.gaps_over_limit, 1)  # 410 -> 500 is fine, 30 -> 410 is not

    def test_v2_state_and_crisis_rows(self):
        s = session(V2, "v2")
        self.assertEqual(s.version, 2)
        self.assertEqual(s.crises, {"firewood": 480.0})
        self.assertEqual(s.first_refuse, 600.0)
        self.assertEqual(s.decrees_end, 2)
        self.assertFalse(s.decrees_estimated)

    def test_report_mentions_targets(self):
        text = ap.report([session(V1, "a"), session(V2, "b")], "Teste")
        self.assertIn("Formato v1", text)
        self.assertIn("> 50%", text)
        self.assertIn("| a |", text)


if __name__ == "__main__":
    unittest.main()
