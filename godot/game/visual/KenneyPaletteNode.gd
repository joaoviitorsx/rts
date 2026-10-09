extends Node3D
## Root script of the Kenney Nature Kit asset scenes (tools/assets/build_kenney_scenes.py): recolours the kit's
## turquoise/orange material roles to the cozy palette when the scene enters the tree.

const KenneyPalette := preload("res://game/visual/KenneyPalette.gd")


func _ready() -> void:
	KenneyPalette.apply(self)
