extends RefCounted
## World-aligned ground mask read by the ground and grass shaders (ground_common.gdshaderinc):
##   R plowed field · G path · B dirt (entrances, yards) · A shade (under trees, along walls, path margins).
## Layers can come from Terrain3D's painted control map (from_terrain) or from shapes (paint_*).
## Blurs give soft gradients; the shaders add noise to the borders so they look hand-painted.

enum { PLOWED, PATH, DIRT, SHADE }

var rect: Rect2
var ppm: int
var w: int
var h: int
var ch: Array[PackedFloat32Array] = []


func _init(world_rect: Rect2, pixels_per_meter: int = 4) -> void:
	rect = world_rect
	ppm = pixels_per_meter
	w = int(rect.size.x * ppm)
	h = int(rect.size.y * ppm)
	for i in 4:
		var a := PackedFloat32Array()
		a.resize(w * h)
		ch.append(a)


func _px(world: Vector2) -> Vector2:
	return (world - rect.position) * ppm


func _world(x: int, y: int) -> Vector2:
	return rect.position + (Vector2(x, y) + Vector2(0.5, 0.5)) / ppm


## Copies painted Terrain3D layers (1 dirt, 2 path, 3 plowed) into the mask.
func from_terrain(terrain_node: Node) -> void:
	for y in h:
		for x in w:
			var p := _world(x, y)
			var id: int = terrain_node.layer_at(Vector3(p.x, 0, p.y))
			if id == 3: ch[PLOWED][y * w + x] = 1.0
			elif id == 2: ch[PATH][y * w + x] = 1.0
			elif id == 1: ch[DIRT][y * w + x] = 1.0


func paint_polyline(points: PackedVector2Array, width: float, channel: int, value: float = 1.0) -> void:
	for i in points.size() - 1:
		var a := points[i]
		var b := points[i + 1]
		var lo := _px(Vector2(minf(a.x, b.x), minf(a.y, b.y)) - Vector2.ONE * width)
		var hi := _px(Vector2(maxf(a.x, b.x), maxf(a.y, b.y)) + Vector2.ONE * width)
		for y in range(maxi(0, int(lo.y)), mini(h, int(hi.y) + 1)):
			for x in range(maxi(0, int(lo.x)), mini(w, int(hi.x) + 1)):
				var p := _world(x, y)
				var d := p.distance_to(Geometry2D.get_closest_point_to_segment(p, a, b))
				var v := (1.0 - smoothstep(width * 0.5 - 0.2, width * 0.5 + 0.2, d)) * value
				ch[channel][y * w + x] = maxf(ch[channel][y * w + x], v)


## Ellipse-ish blob (entrances, yards). `size` in metres.
func paint_blob(center: Vector2, size: Vector2, channel: int, value: float = 1.0) -> void:
	var lo := _px(center - size)
	var hi := _px(center + size)
	for y in range(maxi(0, int(lo.y)), mini(h, int(hi.y) + 1)):
		for x in range(maxi(0, int(lo.x)), mini(w, int(hi.x) + 1)):
			var q := (_world(x, y) - center) / (size * 0.5)
			var v := (1.0 - smoothstep(0.8, 1.15, q.length())) * value
			ch[channel][y * w + x] = maxf(ch[channel][y * w + x], v)


## Rotated rectangle with soft edges (fields, plazas). Rotation in degrees (Y axis, like Node3D).
func paint_rect(center: Vector2, size: Vector2, rot_deg: float, channel: int, value: float = 1.0, soft: float = 0.4) -> void:
	var r := deg_to_rad(rot_deg)
	var reach := size.length() * 0.5 + soft
	var lo := _px(center - Vector2.ONE * reach)
	var hi := _px(center + Vector2.ONE * reach)
	for y in range(maxi(0, int(lo.y)), mini(h, int(hi.y) + 1)):
		for x in range(maxi(0, int(lo.x)), mini(w, int(hi.x) + 1)):
			var local := (_world(x, y) - center).rotated(r)
			var q := local.abs() - size * 0.5
			var d := maxf(q.x, q.y)
			var v := (1.0 - smoothstep(-soft, soft, d)) * value
			ch[channel][y * w + x] = maxf(ch[channel][y * w + x], v)


## Soft round shade (contact darkening under trees, rocks).
func add_shade_disc(center: Vector2, radius: float, strength: float) -> void:
	var lo := _px(center - Vector2.ONE * radius)
	var hi := _px(center + Vector2.ONE * radius)
	for y in range(maxi(0, int(lo.y)), mini(h, int(hi.y) + 1)):
		for x in range(maxi(0, int(lo.x)), mini(w, int(hi.x) + 1)):
			var d := _world(x, y).distance_to(center) / radius
			var v := (1.0 - smoothstep(0.0, 1.0, d)) * strength
			ch[SHADE][y * w + x] = maxf(ch[SHADE][y * w + x], v)


## Shade hugging the outside of a rectangle (building walls). Rotation in degrees.
func add_shade_walls(center: Vector2, size: Vector2, rot_deg: float, width: float, strength: float) -> void:
	var r := deg_to_rad(rot_deg)
	var reach := size.length() * 0.5 + width
	var lo := _px(center - Vector2.ONE * reach)
	var hi := _px(center + Vector2.ONE * reach)
	for y in range(maxi(0, int(lo.y)), mini(h, int(hi.y) + 1)):
		for x in range(maxi(0, int(lo.x)), mini(w, int(hi.x) + 1)):
			var local := (_world(x, y) - center).rotated(r)
			var q := local.abs() - size * 0.5
			var outside := Vector2(maxf(q.x, 0.0), maxf(q.y, 0.0)).length() + minf(maxf(q.x, q.y), 0.0)
			var v := (1.0 - smoothstep(0.0, width, maxf(outside, 0.0))) * strength
			ch[SHADE][y * w + x] = maxf(ch[SHADE][y * w + x], v)


func blur(channel: int, radius_m: float, passes: int = 2) -> void:
	var r := maxi(1, int(radius_m * ppm))
	for _p in passes:
		ch[channel] = _box(ch[channel], r, true)
		ch[channel] = _box(ch[channel], r, false)


func _box(src: PackedFloat32Array, r: int, horizontal: bool) -> PackedFloat32Array:
	var out := PackedFloat32Array()
	out.resize(src.size())
	var n := w if horizontal else h
	var lines := h if horizontal else w
	for line in lines:
		var acc := 0.0
		var count := 0
		for i in range(-r, r + 1):
			if i >= 0 and i < n:
				acc += src[(line * w + i) if horizontal else (i * w + line)]
				count += 1
		for i in n:
			out[(line * w + i) if horizontal else (i * w + line)] = acc / count
			var add := i + r + 1
			var rem := i - r
			if add < n:
				acc += src[(line * w + add) if horizontal else (add * w + line)]
				count += 1
			if rem >= 0:
				acc -= src[(line * w + rem) if horizontal else (rem * w + line)]
				count -= 1
	return out


## Darker earth / shade along path margins (uses the already blurred path channel).
func shade_path_margins(strength: float) -> void:
	for i in w * h:
		var g := ch[PATH][i]
		var margin := smoothstep(0.05, 0.3, g) * (1.0 - smoothstep(0.35, 0.6, g))
		ch[SHADE][i] = maxf(ch[SHADE][i], margin * strength)


## CPU lookup for scattering (nearest pixel).
func sample(world: Vector3) -> Color:
	var p := _px(Vector2(world.x, world.z))
	var x := int(p.x)
	var y := int(p.y)
	if x < 0 or y < 0 or x >= w or y >= h:
		return Color(0, 0, 0, 0)
	var i := y * w + x
	return Color(ch[PLOWED][i], ch[PATH][i], ch[DIRT][i], ch[SHADE][i])


func publish() -> ImageTexture:
	var data := PackedByteArray()
	data.resize(w * h * 4)
	for i in w * h:
		for c in 4:
			data[i * 4 + c] = int(clampf(ch[c][i], 0.0, 1.0) * 255.0)
	var img := Image.create_from_data(w, h, false, Image.FORMAT_RGBA8, data)
	var tex := ImageTexture.create_from_image(img)
	RenderingServer.global_shader_parameter_set("ground_mask", tex)
	RenderingServer.global_shader_parameter_set("ground_mask_rect",
		Vector4(rect.position.x, rect.position.y, rect.size.x, rect.size.y))
	return tex
