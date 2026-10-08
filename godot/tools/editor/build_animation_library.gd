@tool
extends RefCounted
## Builds the shared villager AnimationLibrary (res://assets/characters/animations/LIB_Villager.tres)
## from the Quaternius Universal Animation Library 1/2.
##
## Retarget: UAL, Base Characters, Outfits and Hair share the same 65-bone rig and the same node path
## (Armature/Skeleton3D), so tracks apply directly. Only proportions differ, so we:
##   - drop position tracks except root/pelvis (bone lengths come from each character's own rest pose),
##   - drop scale tracks,
##   - scale pelvis positions by body pelvis height / mannequin pelvis height.
## idle_carry does not exist in UAL2: it is blended here (Idle lower body + Walk_Carry arms at t=0).
##
## Run from the editor: load("res://tools/editor/build_animation_library.gd").new().run()

const UAL1 := "res://assets/vendor/quaternius_ual1/UAL1_Standard.glb"
const UAL2 := "res://assets/vendor/quaternius_ual2/UAL2_Standard.glb"
const BODY := "res://assets/characters/source/CHR_Base_Male_HeadOnly.glb"
const OUT_DIR := "res://assets/characters/animations"
const LIB_PATH := OUT_DIR + "/LIB_Villager.tres"

## library name -> [source file, source animation, loop]
const MAP := {
	"idle": [UAL1, "Idle", true],
	"walk": [UAL1, "Walk", true],
	"run": [UAL1, "Jog_Fwd", true],
	"walk_carry": [UAL2, "Walk_Carry", true],
	"harvest": [UAL2, "Farm_Harvest", true],
	"plant_seed": [UAL2, "Farm_PlantSeed", true],
	"watering": [UAL2, "Farm_Watering", true],
	"chop": [UAL2, "TreeChopping", true],
	"mine": [UAL2, "TreeChopping", true],   # approved gap fix: reuse the chop swing with a pickaxe
	"interact": [UAL1, "Interact", false],
	"pickup": [UAL1, "PickUp_Table", false],
	"sit": [UAL1, "Sitting_Idle", true],
	"talk": [UAL1, "Idle_Talking", true],
}

const KEEP_POSITION := ["root", "pelvis"]
const ARM_BONES := ["clavicle", "upperarm", "lowerarm", "hand", "index", "middle", "ring", "pinky", "thumb"]


func run() -> String:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	var sources := {}
	for path in [UAL1, UAL2]:
		var scene: Node = load(path).instantiate()
		sources[path] = scene
	var pelvis_scale := _pelvis_height(load(BODY).instantiate()) / _pelvis_height(sources[UAL1].duplicate())

	var log := PackedStringArray(["pelvis scale %.4f" % pelvis_scale])
	var lib := AnimationLibrary.new()
	var processed := {}
	for lib_name in MAP:
		var entry: Array = MAP[lib_name]
		var player: AnimationPlayer = sources[entry[0]].find_children("*", "AnimationPlayer", true, false)[0]
		var anim := _clean(player.get_animation(entry[1]), pelvis_scale)
		anim.loop_mode = Animation.LOOP_LINEAR if entry[2] else Animation.LOOP_NONE
		processed[lib_name] = anim
		lib.add_animation(lib_name, _save(anim, lib_name))
		log.append("%s <- %s (%d tracks)" % [lib_name, entry[1], anim.get_track_count()])

	var idle_carry := _blend_upper_body(processed["idle"], processed["walk_carry"])
	lib.add_animation("idle_carry", _save(idle_carry, "idle_carry"))
	log.append("idle_carry <- idle + walk_carry arms (%d tracks)" % idle_carry.get_track_count())

	var err := ResourceSaver.save(lib, LIB_PATH)
	log.append("saved %s: %s" % [LIB_PATH, error_string(err)])
	for s in sources.values():
		s.free()
	return "\n".join(log)


func _pelvis_height(scene: Node) -> float:
	var sk: Skeleton3D = scene.find_children("*", "Skeleton3D", true, false)[0]
	# Measure in scene space: the Blender export may rotate/scale the Armature node.
	var to_scene := Transform3D()
	var node: Node = sk
	while node != scene:
		if node is Node3D:
			to_scene = (node as Node3D).transform * to_scene
		node = node.get_parent()
	var h := (to_scene * sk.get_bone_global_rest(sk.find_bone("pelvis")).origin).y
	scene.free()
	return h


func _bone_of(anim: Animation, track: int) -> String:
	var path := str(anim.track_get_path(track))
	return path.substr(path.find(":") + 1)


func _clean(source: Animation, pelvis_scale: float) -> Animation:
	var anim: Animation = source.duplicate(true)
	for i in range(anim.get_track_count() - 1, -1, -1):
		var bone := _bone_of(anim, i)
		match anim.track_get_type(i):
			Animation.TYPE_SCALE_3D:
				anim.remove_track(i)
			Animation.TYPE_POSITION_3D:
				if bone not in KEEP_POSITION:
					anim.remove_track(i)
				elif bone == "pelvis":
					for k in anim.track_get_key_count(i):
						anim.track_set_key_value(i, k, anim.track_get_key_value(i, k) * pelvis_scale)
	return anim


func _is_arm(bone: String) -> bool:
	for prefix in ARM_BONES:
		if bone.begins_with(prefix):
			return true
	return false


func _blend_upper_body(idle: Animation, carry: Animation) -> Animation:
	var anim: Animation = idle.duplicate(true)
	anim.loop_mode = Animation.LOOP_LINEAR
	for i in anim.get_track_count():
		if anim.track_get_type(i) != Animation.TYPE_ROTATION_3D or not _is_arm(_bone_of(anim, i)):
			continue
		var j := carry.find_track(anim.track_get_path(i), Animation.TYPE_ROTATION_3D)
		if j < 0:
			continue
		var pose: Quaternion = carry.rotation_track_interpolate(j, 0.0)
		for k in range(anim.track_get_key_count(i) - 1, 0, -1):
			anim.track_remove_key(i, k)
		anim.track_set_key_value(i, 0, pose)
	return anim


func _save(anim: Animation, lib_name: String) -> Animation:
	var path := "%s/ANIM_%s.res" % [OUT_DIR, lib_name]
	ResourceSaver.save(anim, path)
	return load(path)
