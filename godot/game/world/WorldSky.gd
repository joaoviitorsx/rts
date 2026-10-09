extends Node3D
## Sky, light and weather of a generated map (world look step 4c): Sky3D (TokisanGames, MIT) for sun, moon, stars,
## atmosphere and clouds, with the approved cozy grade (ACES, SSAO, warm adjustments). Driven by the sim — never the
## other way round: set_clock(day_fraction, today, tomorrow) each frame from SimHost.
##   * time: the sim day (10 s at 1x) maps to 08:00 → 17:00 for most of it, then dusk, a short night and dawn;
##   * clouds: clear / cloudy / rain, and a front building up the day before rain ("chuva chegando");
##   * rain and snow: particles following the camera; a cooler, dimmer light while it falls.

const SKY3D := preload("res://addons/sky_3d/src/Sky3D.gd")

var sky: Node
var _rain: CPUParticles3D
var _snow: CPUParticles3D
var _coverage := 0.3
var _wet := 0.0
var _first := true


func _ready() -> void:
	sky = SKY3D.new()
	sky.name = "Sky3D"
	add_child(sky)   # Sky3D builds its environment, sun, moon and dome on entering the tree
	var env: Environment = sky.environment
	env.tonemap_exposure = 0.8
	# Warm-green ambient of the approved look-dev (no cyan cast from the sky dome on the meadow).
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("b9c3a0")
	env.ambient_light_energy = 0.65
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 1.6
	env.ssao_power = 1.4
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.25
	env.adjustment_contrast = 1.06
	env.glow_enabled = true
	env.glow_intensity = 0.1
	sky.set("game_time_enabled", false)
	sky.set("editor_time_enabled", false)
	sky.set("current_time", 10.0)
	sky.set("fog_enabled", false)   # the dome's fog washes the meadow out at this camera height
	sky.set("ambient_energy", 1.0)
	sky.set("sky_contribution", 0.55)
	if sky.get("sun") is DirectionalLight3D:
		var sun: DirectionalLight3D = sky.get("sun")
		sun.shadow_enabled = true
		sun.shadow_blur = 0.6
		sun.directional_shadow_max_distance = 140.0
	_rain = _particles(Color(0.62, 0.68, 0.8, 0.6), Vector2(0.05, 1.3), 22.0, 3200)
	_snow = _particles(Color(1, 1, 1, 0.9), Vector2(0.12, 0.12), 3.0, 1800)


func _particles(color: Color, size: Vector2, speed: float, amount: int) -> CPUParticles3D:
	var p := CPUParticles3D.new()
	var quad := QuadMesh.new()
	quad.size = size
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.albedo_color = color
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_FIXED_Y
	quad.material = mat
	p.mesh = quad
	p.amount = amount
	p.lifetime = 2.2
	p.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	p.emission_box_extents = Vector3(45, 1, 45)
	p.direction = Vector3(0.15, -1, 0.05)
	p.spread = 3.0
	p.initial_velocity_min = speed
	p.initial_velocity_max = speed * 1.2
	p.gravity = Vector3(0, -2 if speed > 5 else -0.4, 0)
	p.local_coords = false
	p.emitting = false
	add_child(p)
	return p


## day_fraction 0..1 of the sim day; today/tomorrow: "Clear" | "Cloudy" | "Rain" | "Snow"; focus: where the camera looks.
func set_clock(day_fraction: float, today: String, tomorrow: String, focus: Vector3, delta: float) -> void:
	# Readable daylight most of the day (08:00 → 17:00), then dusk, a short night and dawn.
	var hour := 8.0 + day_fraction / 0.8 * 9.0 if day_fraction < 0.8 else fmod(17.0 + (day_fraction - 0.8) / 0.2 * 15.0, 24.0)
	sky.set("current_time", hour)
	var target := 0.25
	if today == "Cloudy": target = 0.6
	elif today == "Rain" or today == "Snow": target = 0.9
	if tomorrow == "Rain" or tomorrow == "Snow": target = maxf(target, 0.55 + day_fraction * 0.3)   # the front comes in
	var falling := today == "Rain" or today == "Snow"
	if _first:   # start in the right state (loading a save on a rainy day, dev captures)
		_first = false
		_coverage = target
		_wet = 1.0 if falling else 0.0
	_coverage = lerpf(_coverage, target, clampf(delta * 0.6, 0.0, 1.0))
	var dome = sky.get("sky")
	if dome:
		dome.set("cumulus_coverage", _coverage)
	_wet = lerpf(_wet, 1.0 if falling else 0.0, clampf(delta * 0.8, 0.0, 1.0))
	sky.set("sun_energy", lerpf(0.85, 0.35, _wet))
	sky.set("cloud_intensity", lerpf(0.6, 0.35, _wet))
	var env: Environment = sky.environment
	env.adjustment_saturation = lerpf(1.25, 0.95, _wet)
	env.ambient_light_energy = lerpf(0.65, 0.5, _wet)
	_rain.emitting = today == "Rain"
	_snow.emitting = today == "Snow"
	_rain.global_position = focus + Vector3(0, 28, 0)
	_snow.global_position = focus + Vector3(0, 22, 0)
