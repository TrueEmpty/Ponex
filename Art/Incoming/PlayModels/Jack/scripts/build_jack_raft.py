"""
Jack Cararrow — Jack_Raft play model (standard living kit addition).

CRITICAL LOCKS:
  - PLAY forms for standard Jack = Jack_Ship + Jack_Raft (this drop adds Raft).
  - Person-form = 2D fullbody Fullbody_JackCarrow.png ONLY — NO 3D person mesh.
  - Phantom Jack raft→fleet is a SEPARATE package (phantom-jack/) — not here.
  - Warm wood / blackish pirate accents (living Jack palette).
  - Empty: AnchorPoint (optional mount).

Style: kiddy Smash-lite, readable Pong silhouette, Principled BSDF solid colors,
modest poly. Flat wooden raft ~1.0–1.5 m.

Blender 4.x — rebuild:
  /usr/bin/blender -b -P /workspace/ponex-playmodels/jack/scripts/build_jack_raft.py
"""
import bpy
import bmesh
import math
import os
import shutil
import subprocess
from mathutils import Vector

WORK = "/workspace/ponex-playmodels/jack"
BLEND_DIR = os.path.join(WORK, "blend")
EXPORT_DIR = os.path.join(WORK, "export")
PREVIEW_DIR = os.path.join(WORK, "preview")
LIVE_FEED = "/workspace/ponex-forest-kit/live/latest.png"
SHIP_FBX = os.path.join(EXPORT_DIR, "Jack_Ship.fbx")

# Living Jack warm pirate palette (match ship)
COLORS = {
    "HullWood":     (0.55, 0.28, 0.12),
    "HullDark":     (0.28, 0.15, 0.08),
    "DeckWood":     (0.68, 0.48, 0.28),
    "CabinWood":    (0.45, 0.26, 0.13),
    "AccentBlack":  (0.08, 0.07, 0.07),
    "RopeTan":      (0.62, 0.50, 0.32),
    "MastWood":     (0.40, 0.24, 0.12),
    "StripeCream":  (0.94, 0.88, 0.72),
    "StripeOrange": (0.95, 0.48, 0.12),
    "FlagBlack":    (0.10, 0.09, 0.09),
}
ROUGH = {
    "HullWood": 0.70, "HullDark": 0.75, "DeckWood": 0.65, "CabinWood": 0.70,
    "AccentBlack": 0.80, "RopeTan": 0.75, "MastWood": 0.65,
    "StripeCream": 0.55, "StripeOrange": 0.50, "FlagBlack": 0.70,
}


def hard_clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.objects,
                 bpy.data.cameras, bpy.data.lights, bpy.data.curves):
        for b in list(coll):
            coll.remove(b)


def make_material(name, color, roughness=0.65):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.0
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def get_or_make(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    return make_material(name, COLORS[name], ROUGH.get(name, 0.65))


def apply_mat(obj, mat):
    if obj.data.materials:
        obj.data.materials.clear()
    obj.data.materials.append(mat)


def shade_flat(obj):
    for poly in obj.data.polygons:
        poly.use_smooth = False
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.shade_flat()
    except Exception:
        pass


def shade_smooth(obj):
    for poly in obj.data.polygons:
        poly.use_smooth = True
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.shade_smooth()
    except Exception:
        pass


def join_objects(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    return obj


def set_origin_at_world_point(obj, world_point):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    px, py, pz = world_point
    for v in bm.verts:
        v.co.x -= px
        v.co.y -= py
        v.co.z -= pz
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    obj.location = (0.0, 0.0, 0.0)


def bounds_info(obj):
    coords = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    xs = [c.x for c in coords]
    ys = [c.y for c in coords]
    zs = [c.z for c in coords]
    return {
        "min": (min(xs), min(ys), min(zs)),
        "max": (max(xs), max(ys), max(zs)),
        "size": (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)),
        "center": ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2),
    }


def mesh_stats(obj):
    return len(obj.data.vertices), len(obj.data.polygons)


def add_cube(name, loc, scale, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    apply_mat(obj, mat)
    return obj


def add_cylinder(name, loc, radius, depth, mat, verts=12, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=verts, radius=radius, depth=depth, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    apply_mat(obj, mat)
    return obj


def make_empty(name, parent, loc, display='PLAIN_AXES', size=0.08):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = display
    emp.empty_display_size = size
    bpy.context.collection.objects.link(emp)
    emp.parent = parent
    emp.location = loc
    return emp


def build_raft(name="Jack_Raft"):
    """
    Kiddy wooden raft ~1.25 m along +X (bow +X). Flat Pong-readable silhouette.
    Origin at waterline / geometric center. Beam along Y; up = Z.
    """
    mats = {k: get_or_make(k) for k in COLORS.keys()}
    parts = []

    # Deck planks (5 boards along length, side-by-side on Y)
    plank_specs = [
        (-0.28, mats["HullWood"]),
        (-0.14, mats["DeckWood"]),
        (0.00, mats["HullWood"]),
        (0.14, mats["DeckWood"]),
        (0.28, mats["HullWood"]),
    ]
    for i, (py, mat) in enumerate(plank_specs):
        p = add_cube(f"Plank_{i}", (0.0, py, 0.06), (1.10, 0.12, 0.08), mat)
        shade_flat(p)
        parts.append(p)

    # Cross-beams under / across (lash structure)
    for i, px in enumerate((-0.40, 0.0, 0.40)):
        beam = add_cube(f"CrossBeam_{i}", (px, 0.0, 0.01), (0.08, 0.62, 0.06), mats["HullDark"])
        parts.append(beam)

    # Log pontoons / barrels at corners (flotation silhouette)
    log_locs = [
        (-0.42, 0.30, -0.04),
        (-0.42, -0.30, -0.04),
        (0.42, 0.30, -0.04),
        (0.42, -0.30, -0.04),
    ]
    for i, loc in enumerate(log_locs):
        log = add_cylinder(
            f"Log_{i}", loc, 0.09, 0.28, mats["CabinWood"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        shade_smooth(log)
        parts.append(log)
        band = add_cylinder(
            f"LogBand_{i}", loc, 0.095, 0.04, mats["AccentBlack"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(band)

    # Side log runners (lengthwise)
    for side, sy in (("L", 0.34), ("R", -0.34)):
        runner = add_cylinder(
            f"Runner_{side}", (0.0, sy, -0.02), 0.07, 1.00, mats["HullDark"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        shade_smooth(runner)
        parts.append(runner)

    # Rope lashing strips (readable tan bands)
    for i, px in enumerate((-0.35, 0.0, 0.35)):
        rope = add_cube(f"Lash_{i}", (px, 0.0, 0.11), (0.04, 0.58, 0.025), mats["RopeTan"])
        parts.append(rope)

    # Tiny mast stub + scrap sail / flag (kiddy pirate raft vibe)
    mast = add_cylinder("MastStub", (-0.15, 0.0, 0.42), 0.025, 0.55, mats["MastWood"], verts=10)
    shade_smooth(mast)
    parts.append(mast)
    yard = add_cylinder(
        "Yard", (-0.15, 0.0, 0.55), 0.012, 0.36, mats["RopeTan"], verts=8,
        rotation=(math.radians(90), 0, 0)
    )
    parts.append(yard)
    sail = add_cube("ScrapSail", (-0.12, 0.0, 0.42), (0.03, 0.30, 0.28), mats["StripeCream"])
    shade_flat(sail)
    parts.append(sail)
    # Soft orange patch
    patch = add_cube("SailPatch", (-0.10, 0.06, 0.46), (0.02, 0.08, 0.08), mats["StripeOrange"])
    parts.append(patch)

    # Tiny black flag at mast top
    flagpole = add_cylinder("Flagpole", (-0.15, 0.0, 0.72), 0.010, 0.14, mats["AccentBlack"], verts=8)
    parts.append(flagpole)
    flag = add_cube("Flag", (-0.10, 0.0, 0.76), (0.08, 0.015, 0.06), mats["FlagBlack"])
    parts.append(flag)

    # Deck crate + bollard (anchor vibe)
    crate = add_cube("Crate", (0.30, 0.10, 0.18), (0.14, 0.12, 0.12), mats["HullDark"])
    parts.append(crate)
    bollard = add_cylinder("Bollard", (0.45, 0.0, 0.16), 0.03, 0.10, mats["AccentBlack"], verts=10)
    parts.append(bollard)

    # Bow tip board (slight overhang for silhouette)
    bow = add_cube("BowBoard", (0.58, 0.0, 0.06), (0.12, 0.40, 0.06), mats["DeckWood"])
    parts.append(bow)

    raft = join_objects(parts, name)
    shade_flat(raft)

    bi = bounds_info(raft)
    origin = (bi["center"][0], bi["center"][1], 0.0)
    set_origin_at_world_point(raft, origin)

    bi2 = bounds_info(raft)
    print(f"RAFT post-origin size={bi2['size']} center={bi2['center']} "
          f"min={bi2['min']} max={bi2['max']}")

    # Optional AnchorPoint near bow bollard
    make_empty("AnchorPoint", raft, (0.45, 0.0, 0.20), display='PLAIN_AXES', size=0.09)

    return raft


# -------------------- EXPORT / RENDER (shared pattern) --------------------

def export_fbx(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    for child in obj.children:
        child.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True,
        use_space_transform=True,
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        bake_space_transform=True,
        embed_textures=False,
        object_types={'MESH', 'EMPTY'},
        add_leaf_bones=False,
    )
    print(f"FBX -> {filepath} ({os.path.getsize(filepath)} bytes)")


def export_obj(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    for c in obj.children:
        c.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.wm.obj_export(
        filepath=filepath,
        export_selected_objects=True,
        forward_axis='NEGATIVE_Z',
        up_axis='Y',
        export_materials=True,
        apply_modifiers=True,
    )
    print(f"OBJ -> {filepath}")


def setup_world(bg=(0.50, 0.62, 0.78, 1.0), strength=0.95):
    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg_node = world.node_tree.nodes.get("Background")
    if bg_node:
        bg_node.inputs[0].default_value = bg
        bg_node.inputs[1].default_value = strength


def setup_lights():
    bpy.ops.object.light_add(type='SUN', location=(4, -3, 8))
    sun = bpy.context.active_object
    sun.name = "KeySun"
    sun.data.energy = 3.4
    sun.rotation_euler = (math.radians(40), math.radians(15), math.radians(35))
    bpy.ops.object.light_add(type='AREA', location=(-3, -2, 4))
    fill = bpy.context.active_object
    fill.name = "Fill"
    fill.data.energy = 90
    fill.data.size = 4
    fill.rotation_euler = (math.radians(50), math.radians(-20), math.radians(-40))
    bpy.ops.object.light_add(type='AREA', location=(2, 3, 3))
    rim = bpy.context.active_object
    rim.name = "WarmRim"
    rim.data.energy = 50
    rim.data.size = 3
    rim.data.color = (1.0, 0.85, 0.65)


def clear_cams_lights():
    for o in list(bpy.data.objects):
        if o.type in ('CAMERA', 'LIGHT'):
            bpy.data.objects.remove(o, do_unlink=True)


def mesh_center_size(objs):
    bpy.context.view_layer.update()
    all_coords = []
    for obj in objs:
        all_coords.extend([obj.matrix_world @ Vector(c) for c in obj.bound_box])
    center = sum(all_coords, Vector()) / len(all_coords)
    xs = [c.x for c in all_coords]
    ys = [c.y for c in all_coords]
    zs = [c.z for c in all_coords]
    size = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)) * 0.5
    size = max(size, 0.3)
    return center, size, (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))


def place_camera_angle(cam, center, size, angle, pad=2.6):
    dist = size * pad
    z_off = size * 0.35
    angles = {"front": 180, "34": 140, "side": 90, "back": 0, "hero": 145}
    deg = angles.get(angle, 145)
    rad = math.radians(deg)
    cam.location = center + Vector((
        dist * math.sin(rad),
        -dist * math.cos(rad),
        z_off + size * 0.15,
    ))
    direction = center - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


def render_angle(objs, out_path, angle="front", res=900, transparent=True, pad=2.6, bg=None):
    setup_world(*( (bg, 1.0) if bg else ((0.50, 0.62, 0.78, 1.0), 0.95) ))
    clear_cams_lights()
    setup_lights()
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    center, size, _, _ = mesh_center_size(objs)
    place_camera_angle(cam, center, size, angle, pad=pad)
    scene = bpy.context.scene
    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except Exception:
        scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.filepath = out_path
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = transparent
    keep = set(o.name for o in objs)
    for o in bpy.data.objects:
        if o.type == 'MESH':
            o.hide_render = o.name not in keep
    bpy.ops.render.render(write_still=True)
    print(f"PREVIEW [{angle}] -> {out_path} ({os.path.getsize(out_path)} bytes)")


def render_turntable_frames(objs, frames_dir, n_frames=24, res=480, pad=2.5):
    os.makedirs(frames_dir, exist_ok=True)
    setup_world()
    clear_cams_lights()
    setup_lights()
    cam_data = bpy.data.cameras.new("TurnCam")
    cam = bpy.data.objects.new("TurnCam", cam_data)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    center, size, _, _ = mesh_center_size(objs)
    scene = bpy.context.scene
    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except Exception:
        scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = 'PNG'
    keep = set(o.name for o in objs)
    for o in bpy.data.objects:
        if o.type == 'MESH':
            o.hide_render = o.name not in keep
    dist = size * pad
    z_off = size * 0.4
    paths = []
    for i in range(n_frames):
        deg = (360.0 * i) / n_frames
        rad = math.radians(deg)
        cam.location = center + Vector((
            dist * math.sin(rad),
            -dist * math.cos(rad),
            z_off,
        ))
        direction = center - cam.location
        cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        fp = os.path.join(frames_dir, f"frame_{i:03d}.png")
        scene.render.filepath = fp
        bpy.ops.render.render(write_still=True)
        paths.append(fp)
    return paths


def assemble_turntable(frames_dir, out_gif, out_mp4=None, fps=12):
    pattern = os.path.join(frames_dir, "frame_%03d.png")
    palette = os.path.join(frames_dir, "palette.png")
    subprocess.run([
        "ffmpeg", "-y", "-framerate", str(fps), "-i", pattern,
        "-vf", "palettegen=max_colors=128", palette
    ], check=True, capture_output=True)
    subprocess.run([
        "ffmpeg", "-y", "-framerate", str(fps), "-i", pattern, "-i", palette,
        "-lavfi", "paletteuse=dither=bayer:bayer_scale=3",
        "-loop", "0", out_gif
    ], check=True, capture_output=True)
    print(f"GIF -> {out_gif} ({os.path.getsize(out_gif)} bytes)")
    if out_mp4:
        subprocess.run([
            "ffmpeg", "-y", "-framerate", str(fps), "-i", pattern,
            "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "23",
            "-movflags", "+faststart", out_mp4
        ], check=True, capture_output=True)
        print(f"MP4 -> {out_mp4} ({os.path.getsize(out_mp4)} bytes)")


def multi_angle_and_turntable(obj, prefix, pad=2.6):
    outs = []
    for a in ("front", "34", "side", "back"):
        fp = os.path.join(PREVIEW_DIR, f"{prefix}_{a}.png")
        render_angle([obj], fp, angle=a, res=900, transparent=True, pad=pad)
        outs.append(fp)
    legacy = os.path.join(PREVIEW_DIR, f"{prefix}.png")
    shutil.copy2(os.path.join(PREVIEW_DIR, f"{prefix}_34.png"), legacy)
    outs.append(legacy)
    frames_dir = os.path.join(PREVIEW_DIR, f"_tt_{prefix}")
    render_turntable_frames([obj], frames_dir, n_frames=24, res=480, pad=pad)
    gif = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.gif")
    mp4 = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.mp4")
    assemble_turntable(frames_dir, gif, mp4)
    outs.extend([gif, mp4])
    return outs


def rename_empties(root):
    for c in root.children:
        if c.type != 'EMPTY':
            continue
        if c.name.startswith("AnchorPoint"):
            c.name = "AnchorPoint"


def validate_fbx(path, need_empties=None):
    hard_clear()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [(o.name, len(o.data.vertices), len(o.data.polygons),
               [s.name for s in o.material_slots if s.material])
              for o in bpy.data.objects if o.type == 'MESH']
    empties = [o.name for o in bpy.data.objects if o.type == 'EMPTY']
    mats = [m.name for m in bpy.data.materials]
    sz = os.path.getsize(path)
    need = set(need_empties or [])
    have = set(empties)
    ok = sz > 1500 and len(meshes) >= 1 and need.issubset(have)
    print(f"VALIDATE {os.path.basename(path)}: bytes={sz} meshes={meshes} "
          f"empties={empties} ok={ok}")
    return ok, meshes, empties, mats, sz


def main():
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(EXPORT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    stats = {}
    preview_files = []

    # --- RAFT export ---
    hard_clear()
    raft = build_raft("Jack_Raft")
    raft.location = (0, 0, 0)
    rename_empties(raft)

    rv, rf = mesh_stats(raft)
    ri = bounds_info(raft)
    print(f"RAFT verts={rv} faces={rf} size={ri['size']}")
    empty_names = [c.name for c in raft.children if c.type == 'EMPTY']
    print(f"EMPTIES: {empty_names}")

    fbx_path = os.path.join(EXPORT_DIR, "Jack_Raft.fbx")
    export_fbx(raft, fbx_path)
    export_obj(raft, os.path.join(EXPORT_DIR, "Jack_Raft.obj"))
    stats["raft"] = {"verts": rv, "faces": rf, "size": ri["size"], "fbx": fbx_path,
                     "empties": empty_names}

    preview_files += multi_angle_and_turntable(raft, "raft", pad=2.8)

    # Dual hero board: ship + raft side by side if ship exists; else raft only
    hard_clear()
    raft = build_raft("Jack_Raft")
    rename_empties(raft)
    raft.location = (0.9, 0, 0)
    display_objs = [raft]
    if os.path.isfile(SHIP_FBX):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=SHIP_FBX)
        imported = [o for o in bpy.data.objects if o not in before]
        ship_roots = [o for o in imported if o.type == 'MESH' and o.parent is None]
        if not ship_roots:
            ship_roots = [o for o in imported if o.type == 'MESH']
        if ship_roots:
            ship = ship_roots[0]
            ship.name = "Jack_Ship"
            ship.location = (-1.1, 0, 0)
            display_objs.append(ship)

    render_angle(display_objs, os.path.join(PREVIEW_DIR, "kit_board.png"),
                 angle="hero", res=1100, transparent=False, pad=2.4,
                 bg=(0.48, 0.60, 0.76, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "kit_board.png"))

    # Raft-only hero opaque (does not overwrite ship hero.png — use raft_hero)
    hard_clear()
    raft = build_raft("Jack_Raft")
    rename_empties(raft)
    raft.location = (0, 0, 0)
    render_angle([raft], os.path.join(PREVIEW_DIR, "raft_hero.png"), angle="hero",
                 res=1024, transparent=False, pad=2.7,
                 bg=(0.48, 0.60, 0.76, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "raft_hero.png"))

    # --- Shared blend: Ship (from FBX if present) + Raft — no person 3D ---
    hard_clear()
    raft = build_raft("Jack_Raft")
    rename_empties(raft)
    raft.location = (1.2, 0, 0)
    raft.rotation_euler = (math.radians(2), math.radians(-3), math.radians(10))

    if os.path.isfile(SHIP_FBX):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=SHIP_FBX)
        imported = [o for o in bpy.data.objects if o not in before]
        for o in imported:
            if o.type == 'MESH' and (o.parent is None or o.parent.type != 'MESH'):
                if "Jack_Ship" not in o.name and o.name.startswith("Jack_Ship"):
                    pass
        # Place ship root meshes
        mesh_roots = [o for o in imported if o.type == 'MESH' and
                      (o.parent is None or o.parent.type == 'EMPTY')]
        # Prefer object named Jack_Ship
        ship_obj = next((o for o in imported if o.name.startswith("Jack_Ship") and o.type == 'MESH'), None)
        if ship_obj is None:
            ship_obj = next((o for o in imported if o.type == 'MESH'), None)
        if ship_obj is not None:
            # Walk to top parent if empty parent chain odd — keep mesh at root
            root = ship_obj
            while root.parent is not None and root.parent.type == 'MESH':
                root = root.parent
            if root.parent is None:
                root.location = (-1.2, 0, 0)
                root.rotation_euler = (math.radians(2), math.radians(-4), math.radians(12))
            else:
                # unparent visual placement on mesh
                ship_obj.location = (-1.2, 0, 0)
                ship_obj.rotation_euler = (math.radians(2), math.radians(-4), math.radians(12))

    # Lights for viewport
    setup_lights()
    setup_world()

    blend_path = os.path.join(BLEND_DIR, "JackPlay.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved {blend_path} (Ship + Raft dual-form PLAY scene; no person 3D)")

    # Live feed: kit board if available else raft hero
    kit = os.path.join(PREVIEW_DIR, "kit_board.png")
    feed_src = kit if os.path.isfile(kit) else os.path.join(PREVIEW_DIR, "raft_hero.png")
    if os.path.isfile(feed_src) and os.path.isdir(os.path.dirname(LIVE_FEED)):
        shutil.copy2(feed_src, LIVE_FEED)
        print(f"LIVE FEED -> {LIVE_FEED}")

    # --- Validate ---
    print("\n=== VALIDATION ===")
    ok, meshes, empties, mats, sz = validate_fbx(fbx_path, need_empties=["AnchorPoint"])

    print("\n=== STATS ===")
    print("RAFT", stats["raft"])
    print(f"FBX bytes={sz} ok={ok} meshes={meshes} empties={empties}")
    print("ALL_OK=", ok)

    stats_path = os.path.join(WORK, "scripts", "_last_stats_raft.txt")
    with open(stats_path, "w") as f:
        f.write(f"raft_verts={stats['raft']['verts']}\n")
        f.write(f"raft_faces={stats['raft']['faces']}\n")
        f.write(f"raft_size={stats['raft']['size']}\n")
        f.write(f"raft_fbx_bytes={sz}\n")
        f.write(f"raft_meshes={meshes}\n")
        f.write(f"raft_empties={empties}\n")
        f.write(f"mats={mats}\n")
        f.write(f"ok={ok}\n")
    print(f"Wrote {stats_path}")
    print("PREVIEW_FILES:")
    for p in sorted(set(preview_files)):
        print(" ", p, os.path.getsize(p) if os.path.isfile(p) else "MISSING")


if __name__ == "__main__":
    main()
