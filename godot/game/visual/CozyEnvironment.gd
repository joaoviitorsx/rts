extends RefCounted
## Shared cozy lighting/post setup (approved in the ground look-dev, 2026-10-08):
## warm low sun with defined shadows, warm-green ambient (no cyan shadows), light SSAO, ACES,
## slight saturation/contrast boost, warm grade, very light distance fog.


static func build(parent: Node) -> Dictionary:
	var env := Environment.new()
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color("6f9fd0")
	sky_mat.sky_horizon_color = Color("d9e4e8")
	sky_mat.ground_horizon_color = Color("c9d3b8")
	sky_mat.ground_bottom_color = Color("6b7a52")
	var sky := Sky.new()
	sky.sky_material = sky_mat
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("b9c3a0")
	env.ambient_light_energy = 0.6
	env.reflected_light_source = Environment.REFLECTION_SOURCE_DISABLED
	env.tonemap_mode = Environment.TONE_MAPPER_ACES
	env.tonemap_exposure = 0.9
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 1.6
	env.ssao_power = 1.4
	env.fog_enabled = true
	env.fog_light_color = Color("cfdbe0")
	env.fog_density = 0.0008
	env.fog_sky_affect = 0.2
	env.fog_aerial_perspective = 0.08
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.15
	env.adjustment_contrast = 1.08
	var grad := Gradient.new()
	grad.set_color(0, Color(0.02, 0.03, 0.05))
	grad.set_color(1, Color(1.0, 0.98, 0.94))
	var lut := GradientTexture1D.new()
	lut.gradient = grad
	env.adjustment_color_correction = lut
	env.glow_enabled = true
	env.glow_intensity = 0.1
	var we := WorldEnvironment.new()
	we.name = "CozyEnvironment"
	we.environment = env
	parent.add_child(we)
	var sun := DirectionalLight3D.new()
	sun.name = "Sun"
	sun.rotation_degrees = Vector3(-36, -140, 0)
	sun.light_color = Color(1.0, 0.92, 0.8)
	sun.light_energy = 1.2
	sun.shadow_enabled = true
	sun.shadow_blur = 0.6
	sun.directional_shadow_max_distance = 120.0
	parent.add_child(sun)
	return {"environment": env, "sun": sun}
