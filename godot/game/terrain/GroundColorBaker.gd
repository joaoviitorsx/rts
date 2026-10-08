extends Node
## Renders ground_bake.gdshader once into a SubViewport and publishes it as the global `ground_color_tex`.
## Re-bake after changing the ground mask or the palette (cost: one small 2D draw).

const SHADER := preload("res://game/terrain/ground_bake.gdshader")

var _viewport: SubViewport
var _rect_node: ColorRect


func bake(size_px: Vector2i) -> void:
	if not _viewport:
		_viewport = SubViewport.new()
		_viewport.disable_3d = true
		_viewport.transparent_bg = true
		_rect_node = ColorRect.new()
		var mat := ShaderMaterial.new()
		mat.shader = SHADER
		_rect_node.material = mat
		_viewport.add_child(_rect_node)
		add_child(_viewport)
	_viewport.size = size_px
	_rect_node.size = Vector2(size_px)
	_viewport.render_target_update_mode = SubViewport.UPDATE_ONCE
	RenderingServer.global_shader_parameter_set("ground_color_tex", _viewport.get_texture())
