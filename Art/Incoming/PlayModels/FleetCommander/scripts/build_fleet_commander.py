"""
Fleet Commander PLAY kit — Ponex (Raymond's kiddy Pong).

CRITICAL LOCKS:
  - PLAY / PROP ships only — mothership + Fast + Heavy + Shield.
  - HERO / person-form stays 2D only (Modeler Fullbody_FleetCommander.png) — NO 3D person mesh.
  - NO Phantom Jack / NO Hyper Test / NO person heroes in this pack.
  - Silhouettes LOCKED to Modeler refs (do not invent):
      /workspace/ponex-ui/fullbody/Fullbody_FleetMothership.png
      /workspace/ponex-ui/fullbody/Fullbody_FleetFast.png
      /workspace/ponex-ui/fullbody/Fullbody_FleetHeavyHitter.png
      /workspace/ponex-ui/fullbody/Fullbody_FleetShield.png
      + Concept_* under /workspace/ponex-ui/fullbody/fleet/
  - Mothership = player / match avatar; spawns smalls via Spawn_Fast / Spawn_Heavy / Spawn_Shield.

Style: kiddy Smash-lite, solid Principled BSDF (no PBR maps).
Palette: dark charcoal/gunmetal + green (MS/Fast), yellow (Heavy), cyan (Shield).
Modest poly: mothership ~1k–3k verts; each small ~300–900 verts.
Scale: mothership ~2.0–2.8 m longest; smalls ~0.6–1.0 m.

Blender 4.x — rebuild:
  /usr/bin/blender -b -P /workspace/ponex-playmodels/fleet-commander/scripts/build_fleet_commander.py
"""
import bpy
import bmesh
import math
import os
import shutil
import subprocess
from mathutils import Vector

WORK = "/workspace/ponex-playmodels/fleet-commander"
BLEND_DIR = os.path.join(WORK, "blend")
EXPORT_DIR = os.path.join(WORK, "export")
PREVIEW_DIR = os.path.join(WORK, "preview")
LIVE_FEED = "/workspace/ponex-forest-kit/live/latest.png"

COLORS = {
    "HullPrimary":  (0.13, 0.14, 0.16),   # #212429 dark charcoal
    "HullPanel":    (0.32, 0.34, 0.37),   # #52565E lighter panels
    "HullDark":     (0.06, 0.07, 0.08),   # #0F1214 recesses
    "HullMid":      (0.20, 0.21, 0.23),   # #33363B mid gunmetal
    "TrimSteel":    (0.42, 0.44, 0.47),   # #6B7078 quiet steel
    "AccentGreen":  (0.35, 0.92, 0.28),   # #59EB47 MS sensors + Fast exhaust
    "AccentYellow": (0.95, 0.78, 0.18),   # #F2C72E Heavy sensor dots
    "AccentCyan":   (0.40, 0.90, 0.95),   # #66E5F2 Shield ring / thrusters
    "DecalWhite":   (0.94, 0.94, 0.95),   # #F0F0F2 crests / FAST / skull / shield
    "CockpitGlass": (0.10, 0.14, 0.20),   # #1A2433 dark canopy
    "HazardYellow": (0.85, 0.70, 0.12),   # #D9B31F hangar lip
    "EngineDark":   (0.05, 0.05, 0.06),   # #0D0D0F thruster interiors
}

ROUGH = {
    "HullPrimary": 0.55, "HullPanel": 0.48, "HullDark": 0.70, "HullMid": 0.55,
    "TrimSteel": 0.40, "AccentGreen": 0.25, "AccentYellow": 0.30, "AccentCyan": 0.25,
    "DecalWhite": 0.45, "CockpitGlass": 0.20, "HazardYellow": 0.50, "EngineDark": 0.75,
}


def hard_clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.objects,
                 bpy.data.cameras, bpy.data.lights, bpy.data.curves):
        for b in list(coll):
            coll.remove(b)


def make_material(name, color, roughness=0.55):
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
        metallic = 0.35 if name in ("TrimSteel", "HullPanel") else (
            0.12 if name.startswith("Accent") or name == "DecalWhite" else 0.05
        )
        bsdf.inputs["Metallic"].default_value = metallic
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def get_or_make(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    return make_material(name, COLORS[name], ROUGH.get(name, 0.55))


def apply_mat(obj, mat):
    if obj.data.materials:
        obj.data.materials.clear()
    obj.data.materials.append(mat)


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


def add_cone(name, loc, radius1, depth, mat, verts=10, radius2=0.0, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=verts, radius1=radius1, radius2=radius2, depth=depth, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    apply_mat(obj, mat)
    return obj


def add_uv_sphere(name, loc, radius, mat, segments=12, ring_count=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=ring_count, radius=radius, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    apply_mat(obj, mat)
    return obj


def add_torus(name, loc, major, minor, mat, major_seg=16, minor_seg=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=major_seg, minor_segments=minor_seg,
        major_radius=major, minor_radius=minor, location=loc
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


def add_winged_crest(tag, loc, mat, scale=1.0):
    """Simple white winged-crest decal (center shield + two wing stubs)."""
    parts = []
    body = add_cube(f"{tag}_CrestBody", loc, (0.06 * scale, 0.02 * scale, 0.08 * scale), mat)
    parts.append(body)
    for side, sy in (("L", 1), ("R", -1)):
        wing = add_cube(
            f"{tag}_CrestWing_{side}",
            (loc[0] - 0.01 * scale, loc[1] + sy * 0.06 * scale, loc[2]),
            (0.04 * scale, 0.08 * scale, 0.03 * scale), mat
        )
        parts.append(wing)
    return parts


# -------------------- MOTHERSHIP (concept-locked) --------------------

def build_mothership(name="FC_Mothership"):
    """
    Elongated industrial carrier ~2.5 m +X.
    Blunt armored bow, LARGE HEX mid hangar, tiered aft bridge towers,
    3 clustered thrusters + 4 X-fins, landing pads, green sensors, white crests.
    """
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []

    # Elongated main hull (long, moderately wide, low) — concept profile
    hull = add_cube("MS_Hull", (0.0, 0.0, 0.05), (2.30, 0.72, 0.48), mats["HullPrimary"])
    bpy.ops.object.select_all(action='DESELECT')
    hull.select_set(True)
    bpy.context.view_layer.objects.active = hull
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=2)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Faceted industrial taper: blunt bow, slight stern taper
    mesh = hull.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    xs = [v.co.x for v in bm.verts]
    x_min, x_max = min(xs), max(xs)
    span = max(x_max - x_min, 1e-6)
    for v in bm.verts:
        t = (v.co.x - x_min) / span
        if t > 0.82:  # blunt bow — keep mass, slight squeeze
            u = (t - 0.82) / 0.18
            v.co.y *= 1.0 - 0.22 * u
            v.co.z *= 1.0 - 0.12 * u
        elif t < 0.12:  # stern
            u = 1.0 - t / 0.12
            v.co.y *= 1.0 - 0.18 * u
        # panel step: flatten deck slightly
        if v.co.z > 0.15 and 0.2 < t < 0.75:
            v.co.z *= 0.96
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    shade_flat(hull)
    parts.append(hull)

    # Upper panel band
    deck = add_cube("MS_Deck", (0.05, 0.0, 0.28), (2.05, 0.62, 0.08), mats["HullPanel"])
    shade_flat(deck)
    parts.append(deck)

    # Blunt armored bow block + chin
    bow = add_cube("MS_Bow", (1.20, 0.0, 0.08), (0.35, 0.58, 0.42), mats["HullMid"])
    shade_flat(bow)
    parts.append(bow)
    chin = add_cube("MS_Chin", (1.28, 0.0, -0.12), (0.28, 0.40, 0.14), mats["HullDark"])
    parts.append(chin)
    # Bow sensor slits
    for i, pz in enumerate((0.12, 0.02, -0.06)):
        slot = add_cube(f"MS_BowSlot_{i}", (1.36, 0.0, pz), (0.04, 0.32, 0.035), mats["HullDark"])
        parts.append(slot)
    # Tiny chin antenna stub
    ant = add_cylinder("MS_ChinAnt", (1.40, 0.0, -0.18), 0.015, 0.08, mats["TrimSteel"], verts=6,
                       rotation=(0, math.radians(90), 0))
    parts.append(ant)

    # ---- LARGE HEXAGONAL mid hangar (concept hero feature) ----
    # Hex frame on both sides (Y) + dark cavity through mid hull
    hangar_dark = add_cube("MS_HangarCavity", (0.05, 0.0, 0.02), (0.70, 0.78, 0.42), mats["HullDark"])
    parts.append(hangar_dark)
    for side, sy in (("L", 0.38), ("R", -0.38)):
        # 6-sided cylinder as hex opening frame
        hexf = add_cylinder(
            f"MS_HangarHex_{side}", (0.05, sy, 0.02), 0.28, 0.06, mats["TrimSteel"], verts=6,
            rotation=(math.radians(90), 0, math.radians(30))
        )
        shade_flat(hexf)
        parts.append(hexf)
        # Inner dark hex lip
        hexi = add_cylinder(
            f"MS_HangarHexIn_{side}", (0.05, sy, 0.02), 0.22, 0.04, mats["HullDark"], verts=6,
            rotation=(math.radians(90), 0, math.radians(30))
        )
        parts.append(hexi)
    # Hazard lip stripes on hangar floor
    for i, px in enumerate((-0.18, -0.06, 0.06, 0.18)):
        stripe = add_cube(f"MS_HangarStripe_{i}", (px, 0.0, -0.16), (0.08, 0.50, 0.02), mats["HazardYellow"])
        parts.append(stripe)
    # Tiny fighter silhouette hint inside hangar (scale prop — not a play ship)
    hint = add_cube("MS_FighterHint", (0.05, 0.0, -0.02), (0.28, 0.08, 0.05), mats["HullPanel"])
    parts.append(hint)
    hint_wing = add_cube("MS_FighterHintWing", (0.0, 0.0, -0.02), (0.14, 0.22, 0.015), mats["HullMid"])
    parts.append(hint_wing)

    # ---- Tiered bridge towers aft (2 large) + smaller forward tower ----
    # Aft twin towers
    for side, sy in (("L", 0.16), ("R", -0.16)):
        t1 = add_cube(f"MS_TowerAft_{side}", (-0.75, sy, 0.48), (0.28, 0.20, 0.40), mats["HullMid"])
        shade_flat(t1)
        parts.append(t1)
        t2 = add_cube(f"MS_TowerAftTop_{side}", (-0.75, sy, 0.72), (0.20, 0.14, 0.14), mats["HullPanel"])
        parts.append(t2)
        # Green sensor tip
        tip = add_cube(f"MS_TowerTip_{side}", (-0.75, sy, 0.82), (0.04, 0.04, 0.06), mats["AccentGreen"])
        parts.append(tip)
        # Window slots
        for i, pz in enumerate((0.42, 0.52, 0.62)):
            w = add_cube(f"MS_TowerWin_{side}_{i}", (-0.60, sy, pz), (0.03, 0.12, 0.04), mats["CockpitGlass"])
            parts.append(w)
    # Forward smaller tower
    tf = add_cube("MS_TowerFwd", (0.55, 0.0, 0.42), (0.22, 0.24, 0.28), mats["HullMid"])
    parts.append(tf)
    tft = add_cube("MS_TowerFwdTop", (0.55, 0.0, 0.58), (0.16, 0.16, 0.10), mats["HullPanel"])
    parts.append(tft)
    tipf = add_cube("MS_TowerFwdTip", (0.55, 0.0, 0.66), (0.035, 0.035, 0.05), mats["AccentGreen"])
    parts.append(tipf)

    # White winged crests (bow side + aft of hangar)
    parts += add_winged_crest("MS_Bow", (0.85, 0.37, 0.12), mats["DecalWhite"], scale=1.1)
    parts += add_winged_crest("MS_Aft", (-0.35, 0.37, 0.12), mats["DecalWhite"], scale=1.0)

    # ---- Rear: 3 clustered thrusters + 4 X-fins ----
    thruster_specs = [
        ("C", 0.0, 0.02),
        ("L", 0.14, -0.02),
        ("R", -0.14, -0.02),
    ]
    for label, sy, sz in thruster_specs:
        pod = add_cylinder(
            f"MS_Thruster_{label}", (-1.25, sy, sz), 0.12, 0.28, mats["HullDark"], verts=12,
            rotation=(0, math.radians(90), 0)
        )
        shade_smooth(pod)
        parts.append(pod)
        ring = add_cylinder(
            f"MS_ThrusterRing_{label}", (-1.38, sy, sz), 0.13, 0.04, mats["TrimSteel"], verts=12,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(ring)
        glow = add_cylinder(
            f"MS_ThrusterGlow_{label}", (-1.42, sy, sz), 0.09, 0.03, mats["AccentGreen"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(glow)

    # 4 fins at ~45° (X / K from rear)
    fin_angles = [
        ("UL", 0.18, 0.18, math.radians(40)),
        ("UR", -0.18, 0.18, math.radians(-40)),
        ("LL", 0.18, -0.14, math.radians(-40)),
        ("LR", -0.18, -0.14, math.radians(40)),
    ]
    for label, sy, sz, roll in fin_angles:
        fin = add_cube(
            f"MS_Fin_{label}", (-1.15, sy, sz), (0.35, 0.04, 0.22), mats["HullPanel"],
            rotation=(roll, 0, 0)
        )
        shade_flat(fin)
        parts.append(fin)

    # Landing pads underside
    for i, (px, py) in enumerate([
        (0.70, 0.22), (0.70, -0.22), (-0.40, 0.22), (-0.40, -0.22), (0.15, 0.0)
    ]):
        pad = add_cube(f"MS_Pad_{i}", (px, py, -0.28), (0.16, 0.12, 0.06), mats["HullDark"])
        parts.append(pad)
        foot = add_cube(f"MS_PadFoot_{i}", (px, py, -0.34), (0.12, 0.09, 0.04), mats["TrimSteel"])
        parts.append(foot)

    # Side armor panels / greeble strips
    for i, px in enumerate((-0.95, -0.50, 0.35, 0.90)):
        strip = add_cube(f"MS_SideStrip_{i}", (px, 0.0, -0.05), (0.08, 0.68, 0.18), mats["HullMid"])
        parts.append(strip)

    # Green hull sensor dots
    for i, (px, py, pz) in enumerate([
        (1.05, 0.30, 0.22), (0.40, 0.32, 0.30), (-0.90, 0.22, 0.55),
        (1.05, -0.30, 0.22), (-0.55, -0.28, 0.20),
    ]):
        dot = add_cube(f"MS_SensorDot_{i}", (px, py, pz), (0.03, 0.03, 0.03), mats["AccentGreen"])
        parts.append(dot)

    ship = join_objects(parts, name)
    shade_flat(ship)
    ship.scale = (0.92, 0.92, 0.92)
    bpy.ops.object.select_all(action='DESELECT')
    ship.select_set(True)
    bpy.context.view_layer.objects.active = ship
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    bi = bounds_info(ship)
    origin = (bi["center"][0], bi["center"][1], bi["center"][2] * 0.4)
    set_origin_at_world_point(ship, origin)
    bi2 = bounds_info(ship)
    print(f"MOTHERSHIP post-origin size={bi2['size']}")

    # Spawn empties in/near hangar + optional HangarBay / Bridge
    make_empty("Spawn_Fast", ship, (0.20, 0.0, -0.05), display='SPHERE', size=0.10)
    make_empty("Spawn_Heavy", ship, (0.05, 0.0, -0.05), display='SPHERE', size=0.10)
    make_empty("Spawn_Shield", ship, (-0.12, 0.0, -0.05), display='SPHERE', size=0.10)
    make_empty("HangarBay", ship, (0.05, 0.0, -0.10), display='PLAIN_AXES', size=0.12)
    make_empty("Bridge", ship, (-0.75, 0.0, 0.70), display='PLAIN_AXES', size=0.08)
    return ship


# -------------------- FAST (concept-locked) --------------------

def build_fast(name="FC_Fast"):
    """Sleek dart/stealth jet: sharp nose, canopy, swept delta wings, twin green exhausts, FAST decal."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []

    # Flat angular fuselage
    body = add_cube("Fast_Body", (0.0, 0.0, 0.0), (0.55, 0.14, 0.10), mats["HullPrimary"])
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=1)
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh = body.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    xs = [v.co.x for v in bm.verts]
    x_min, x_max = min(xs), max(xs)
    span = max(x_max - x_min, 1e-6)
    for v in bm.verts:
        t = (v.co.x - x_min) / span
        if t > 0.55:
            u = (t - 0.55) / 0.45
            v.co.y *= 1.0 - 0.55 * u
            v.co.z *= 1.0 - 0.35 * u
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    shade_flat(body)
    parts.append(body)

    # Sharp needle nose
    nose = add_cone(
        "Fast_Nose", (0.40, 0.0, 0.0), 0.05, 0.28, mats["HullPanel"], verts=8,
        rotation=(0, math.radians(90), 0)
    )
    shade_smooth(nose)
    parts.append(nose)
    tip = add_cone(
        "Fast_Tip", (0.55, 0.0, 0.0), 0.018, 0.08, mats["HullDark"], verts=6,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(tip)

    # Dark canopy
    canopy = add_cube("Fast_Canopy", (0.12, 0.0, 0.07), (0.16, 0.10, 0.05), mats["CockpitGlass"])
    shade_smooth(canopy)
    parts.append(canopy)

    # Top dark charcoal panel (FAST decal sits here)
    top = add_cube("Fast_TopPanel", (-0.02, 0.0, 0.06), (0.22, 0.12, 0.03), mats["HullDark"])
    parts.append(top)

    # White chevrons >> 
    for i, px in enumerate((0.02, -0.04)):
        ch = add_cube(f"Fast_Chevron_{i}", (px, 0.0, 0.085), (0.03, 0.06, 0.01), mats["DecalWhite"],
                      rotation=(0, 0, math.radians(45 if i == 0 else -45)))
        # simpler: two angled bars as forward chevrons
        parts.append(ch)
    # Rebuild chevrons as readable forward arrows
    # Remove the rotated ones' look — add explicit V shapes via two thin cubes each
    # (already appended; add clearer ones)
    for i, px in enumerate((0.06, 0.00)):
        a = add_cube(f"Fast_ChevA_{i}", (px, 0.025, 0.09), (0.04, 0.012, 0.008), mats["DecalWhite"],
                     rotation=(0, 0, math.radians(-35)))
        b = add_cube(f"Fast_ChevB_{i}", (px, -0.025, 0.09), (0.04, 0.012, 0.008), mats["DecalWhite"],
                     rotation=(0, 0, math.radians(35)))
        parts.extend([a, b])

    # Block letter "FAST" as white cubes (readable at Pong scale)
    # F A S T — simplified block glyphs on top panel
    letter_x0 = -0.12
    # F
    parts.append(add_cube("Fast_L_F1", (letter_x0, 0.04, 0.09), (0.01, 0.05, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_F2", (letter_x0 + 0.02, 0.06, 0.09), (0.03, 0.01, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_F3", (letter_x0 + 0.015, 0.04, 0.09), (0.025, 0.01, 0.008), mats["DecalWhite"]))
    # A
    ax = letter_x0 - 0.08
    parts.append(add_cube("Fast_L_A1", (ax, 0.04, 0.09), (0.01, 0.05, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_A2", (ax - 0.03, 0.04, 0.09), (0.01, 0.05, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_A3", (ax - 0.015, 0.06, 0.09), (0.03, 0.01, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_A4", (ax - 0.015, 0.04, 0.09), (0.03, 0.01, 0.008), mats["DecalWhite"]))
    # S (approx)
    sx = letter_x0 - 0.16
    parts.append(add_cube("Fast_L_S1", (sx - 0.015, 0.06, 0.09), (0.04, 0.01, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_S2", (sx - 0.015, 0.04, 0.09), (0.04, 0.01, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_S3", (sx - 0.015, 0.02, 0.09), (0.04, 0.01, 0.008), mats["DecalWhite"]))
    # T
    tx = letter_x0 - 0.24
    parts.append(add_cube("Fast_L_T1", (tx - 0.015, 0.06, 0.09), (0.04, 0.01, 0.008), mats["DecalWhite"]))
    parts.append(add_cube("Fast_L_T2", (tx - 0.015, 0.035, 0.09), (0.01, 0.045, 0.008), mats["DecalWhite"]))

    # Swept delta wings
    for side, sy in (("L", 1), ("R", -1)):
        wing = add_cube(f"Fast_Wing_{side}", (-0.05, sy * 0.22, 0.0), (0.42, 0.28, 0.025), mats["HullMid"])
        mesh = wing.data
        bm = bmesh.new()
        bm.from_mesh(mesh)
        for v in bm.verts:
            # sweep: leading edge forward narrow, trailing wide aft
            if v.co.x > 0:
                v.co.y *= 0.25
                v.co.x *= 0.7
            else:
                v.co.y *= 1.15
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
        shade_flat(wing)
        parts.append(wing)
        # Lighter leading-edge panel
        le = add_cube(f"Fast_LE_{side}", (0.02, sy * 0.18, 0.01), (0.20, 0.12, 0.015), mats["HullPanel"])
        mesh = le.data
        bm = bmesh.new()
        bm.from_mesh(mesh)
        for v in bm.verts:
            if v.co.x > 0.02:
                v.co.y *= 0.4
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
        parts.append(le)

    # Twin vertical fins aft
    for side, sy in (("L", 0.08), ("R", -0.08)):
        fin = add_cube(f"Fast_Fin_{side}", (-0.28, sy, 0.10), (0.14, 0.02, 0.14), mats["HullPanel"])
        parts.append(fin)

    # Twin green exhausts
    for side, sy in (("L", 0.05), ("R", -0.05)):
        eng = add_cylinder(
            f"Fast_Eng_{side}", (-0.32, sy, 0.0), 0.035, 0.10, mats["EngineDark"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(eng)
        glow = add_cylinder(
            f"Fast_Glow_{side}", (-0.40, sy, 0.0), 0.028, 0.08, mats["AccentGreen"], verts=8,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(glow)
        # flame taper tip
        flame = add_cone(
            f"Fast_Flame_{side}", (-0.48, sy, 0.0), 0.022, 0.10, mats["AccentGreen"], verts=6,
            rotation=(0, math.radians(-90), 0)
        )
        parts.append(flame)

    ship = join_objects(parts, name)
    shade_flat(ship)
    ship.scale = (0.82, 0.82, 0.82)
    bpy.ops.object.select_all(action='DESELECT')
    ship.select_set(True)
    bpy.context.view_layer.objects.active = ship
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bi = bounds_info(ship)
    set_origin_at_world_point(ship, bi["center"])
    print(f"FAST post-origin size={bounds_info(ship)['size']}")
    make_empty("HitPoint", ship, (0.40, 0.0, 0.0), display='SPHERE', size=0.04)
    return ship


# -------------------- HEAVY HITTER (concept-locked) --------------------

def build_heavy(name="FC_Heavy"):
    """Chunky front-loaded hull, PROMINENT twin rotary/gatling stubs, stubby wings, twin fins, skull crest, yellow dots."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []

    # Front-loaded chunky body
    body = add_cube("Heavy_Body", (0.05, 0.0, 0.02), (0.55, 0.36, 0.30), mats["HullPrimary"])
    shade_flat(body)
    parts.append(body)
    # Armor plates
    for side, sy in (("L", 0.19), ("R", -0.19)):
        plate = add_cube(f"Heavy_Plate_{side}", (0.05, sy, 0.04), (0.48, 0.05, 0.26), mats["HullPanel"])
        shade_flat(plate)
        parts.append(plate)
    # Raised angular cockpit
    cock = add_cube("Heavy_Cockpit", (0.08, 0.0, 0.20), (0.18, 0.16, 0.10), mats["HullMid"])
    parts.append(cock)
    for i, (px, sy) in enumerate([(0.14, 0.0), (0.10, 0.06), (0.10, -0.06)]):
        pane = add_cube(f"Heavy_Pane_{i}", (px, sy, 0.24), (0.06, 0.05 if i == 0 else 0.03, 0.04), mats["CockpitGlass"])
        parts.append(pane)

    # Nose mass
    nose = add_cube("Heavy_Nose", (0.38, 0.0, 0.0), (0.22, 0.30, 0.24), mats["HullDark"])
    shade_flat(nose)
    parts.append(nose)

    # PROMINENT twin rotary/gatling stubs
    for side, sy in (("L", 0.08), ("R", -0.08)):
        barrel = add_cylinder(
            f"Heavy_Gun_{side}", (0.62, sy, -0.02), 0.045, 0.32, mats["EngineDark"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        shade_smooth(barrel)
        parts.append(barrel)
        # Cooling rings
        for i, ox in enumerate((0.52, 0.58, 0.64)):
            ring = add_cylinder(
                f"Heavy_Ring_{side}_{i}", (ox, sy, -0.02), 0.055, 0.025, mats["HullMid"], verts=10,
                rotation=(0, math.radians(90), 0)
            )
            parts.append(ring)
        # Muzzle tip (multi-hole look via darker tip + small inset)
        tip = add_cylinder(
            f"Heavy_Muzzle_{side}", (0.78, sy, -0.02), 0.048, 0.04, mats["HullDark"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(tip)
        hole = add_cylinder(
            f"Heavy_Bore_{side}", (0.80, sy, -0.02), 0.022, 0.03, mats["EngineDark"], verts=8,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(hole)

    # Stubby side wings
    for side, sy in (("L", 0.28), ("R", -0.28)):
        wing = add_cube(f"Heavy_Wing_{side}", (0.0, sy, 0.0), (0.28, 0.16, 0.08), mats["HullMid"])
        shade_flat(wing)
        parts.append(wing)
        vent = add_cube(f"Heavy_Vent_{side}", (0.0, sy, -0.05), (0.18, 0.10, 0.04), mats["HullDark"])
        parts.append(vent)

    # Twin vertical fins
    for side, sy in (("L", 0.10), ("R", -0.10)):
        fin = add_cube(f"Heavy_Fin_{side}", (-0.28, sy, 0.16), (0.14, 0.03, 0.18), mats["HullPanel"])
        parts.append(fin)

    # Rear block
    rear = add_cube("Heavy_Rear", (-0.30, 0.0, 0.0), (0.16, 0.28, 0.24), mats["HullMid"])
    parts.append(rear)
    for side, sy in (("L", 0.10), ("R", -0.10)):
        glow = add_cylinder(
            f"Heavy_Glow_{side}", (-0.40, sy, 0.0), 0.035, 0.04, mats["AccentGreen"], verts=8,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(glow)

    # White skull + wings emblem (simplified mesh decal on side)
    skull = add_uv_sphere("Heavy_Skull", (0.05, 0.22, 0.08), 0.045, mats["DecalWhite"], segments=8, ring_count=6)
    parts.append(skull)
    for side, oz in (("U", 0.02), ("D", -0.02)):
        jaw = add_cube(f"Heavy_Jaw_{side}", (0.05, 0.22, 0.04 + oz), (0.04, 0.02, 0.02), mats["DecalWhite"])
        parts.append(jaw)
    for side, sy in (("L", 0.28), ("R", 0.16)):
        # wing relative to skull on +Y face — both on same side of hull
        pass
    # Wings flanking skull on hull side (+Y)
    wL = add_cube("Heavy_EmbWing_L", (0.05, 0.22, 0.12), (0.08, 0.015, 0.03), mats["DecalWhite"])
    wR = add_cube("Heavy_EmbWing_R", (0.05, 0.22, 0.04), (0.08, 0.015, 0.03), mats["DecalWhite"])
    # Better: wings left/right of skull along X
    w1 = add_cube("Heavy_EmbW1", (0.12, 0.22, 0.08), (0.06, 0.015, 0.035), mats["DecalWhite"])
    w2 = add_cube("Heavy_EmbW2", (-0.02, 0.22, 0.08), (0.06, 0.015, 0.035), mats["DecalWhite"])
    parts.extend([wL, wR, w1, w2])

    # Yellow sensor dots
    for i, (px, py, pz) in enumerate([
        (0.30, 0.18, 0.14), (0.15, -0.18, 0.12), (-0.10, 0.16, 0.18),
        (0.35, -0.12, -0.08), (-0.20, -0.14, 0.10), (0.0, 0.10, 0.22),
    ]):
        dot = add_cube(f"Heavy_Dot_{i}", (px, py, pz), (0.025, 0.025, 0.025), mats["AccentYellow"])
        parts.append(dot)

    ship = join_objects(parts, name)
    shade_flat(ship)
    ship.scale = (0.78, 0.78, 0.78)
    bpy.ops.object.select_all(action='DESELECT')
    ship.select_set(True)
    bpy.context.view_layer.objects.active = ship
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bi = bounds_info(ship)
    set_origin_at_world_point(ship, bi["center"])
    print(f"HEAVY post-origin size={bounds_info(ship)['size']}")
    make_empty("GunPoint", ship, (0.82, 0.0, -0.02), display='SPHERE', size=0.05)
    make_empty("HitPoint", ship, (0.15, 0.0, 0.05), display='PLAIN_AXES', size=0.04)
    return ship


# -------------------- SHIELD (concept-locked) --------------------

def build_shield(name="FC_Shield"):
    """Front circular dish + concentric rings + cyan glow + shield emblem; blocky hull; side cyan thrusters."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []

    # Blocky main hull
    body = add_cube("Shield_Body", (-0.05, 0.0, 0.0), (0.42, 0.28, 0.26), mats["HullPrimary"])
    shade_flat(body)
    parts.append(body)
    # Tiered armor plates
    for i, (px, sx, sy, sz) in enumerate([
        (0.05, 0.30, 0.30, 0.08),
        (-0.15, 0.22, 0.24, 0.10),
    ]):
        plate = add_cube(f"Shield_Plate_{i}", (px, 0.0, 0.12 + i * 0.02), (sx, sy, sz), mats["HullPanel"])
        shade_flat(plate)
        parts.append(plate)

    # Raised cockpit
    cock = add_cube("Shield_Cockpit", (0.05, 0.0, 0.18), (0.16, 0.14, 0.10), mats["HullMid"])
    parts.append(cock)
    glass = add_cube("Shield_Glass", (0.12, 0.0, 0.20), (0.06, 0.12, 0.07), mats["CockpitGlass"])
    parts.append(glass)
    # Small white shield emblem under cockpit
    emb = add_cube("Shield_CockEmb", (0.14, 0.0, 0.12), (0.02, 0.05, 0.06), mats["DecalWhite"])
    parts.append(emb)

    # ---- Front circular dish / barrier ----
    disk = add_uv_sphere("Shield_Disk", (0.32, 0.0, 0.02), 0.30, mats["HullMid"], segments=20, ring_count=12)
    bpy.ops.object.select_all(action='DESELECT')
    disk.select_set(True)
    bpy.context.view_layer.objects.active = disk
    disk.scale = (0.10, 1.0, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    shade_flat(disk)
    parts.append(disk)

    # Concentric armor ring (darker)
    ring1 = add_torus(
        "Shield_Ring1", (0.34, 0.0, 0.02), major=0.22, minor=0.03,
        mat=mats["HullDark"], major_seg=20, minor_seg=8,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(ring1)
    # Cyan glow ring (segmented look via torus)
    cyan = add_torus(
        "Shield_CyanRing", (0.36, 0.0, 0.02), major=0.26, minor=0.022,
        mat=mats["AccentCyan"], major_seg=20, minor_seg=8,
        rotation=(0, math.radians(90), 0)
    )
    shade_smooth(cyan)
    parts.append(cyan)
    # Outer rim
    outer = add_torus(
        "Shield_Outer", (0.33, 0.0, 0.02), major=0.32, minor=0.025,
        mat=mats["TrimSteel"], major_seg=20, minor_seg=6,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(outer)

    # Center hex medallion + white shield emblem
    hexc = add_cylinder(
        "Shield_Hex", (0.38, 0.0, 0.02), 0.08, 0.04, mats["HullDark"], verts=6,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(hexc)
    # White shield shape (tall rounded rectangle approx)
    emblem = add_cube("Shield_Emblem", (0.40, 0.0, 0.02), (0.02, 0.06, 0.08), mats["DecalWhite"])
    parts.append(emblem)
    emblem_top = add_cube("Shield_EmblemTop", (0.40, 0.0, 0.07), (0.02, 0.05, 0.03), mats["DecalWhite"])
    parts.append(emblem_top)

    # Cyan accent strips on hull sides
    for side, sy in (("L", 0.15), ("R", -0.15)):
        strip = add_cube(f"Shield_CyanStrip_{side}", (0.0, sy, 0.05), (0.20, 0.02, 0.03), mats["AccentCyan"])
        parts.append(strip)

    # Side thruster pods with cyan exhaust
    for side, sy in (("L", 0.28), ("R", -0.28)):
        strut = add_cube(f"Shield_Strut_{side}", (-0.05, sy * 0.7, 0.0), (0.16, 0.08, 0.08), mats["HullMid"])
        parts.append(strut)
        pod = add_cylinder(
            f"Shield_Pod_{side}", (-0.05, sy, 0.0), 0.07, 0.28, mats["HullDark"], verts=12,
            rotation=(0, math.radians(90), 0)
        )
        shade_smooth(pod)
        parts.append(pod)
        # Front-facing cyan glow (concept shows forward apertures glowing)
        glow_f = add_cylinder(
            f"Shield_GlowF_{side}", (0.10, sy, 0.0), 0.05, 0.03, mats["AccentCyan"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(glow_f)
        glow_r = add_cylinder(
            f"Shield_GlowR_{side}", (-0.20, sy, 0.0), 0.05, 0.04, mats["AccentCyan"], verts=10,
            rotation=(0, math.radians(90), 0)
        )
        parts.append(glow_r)
        # Outer fin on pod
        fin = add_cube(f"Shield_PodFin_{side}", (-0.05, sy * 1.15, 0.0), (0.12, 0.03, 0.06), mats["HullPanel"])
        parts.append(fin)

    # Top intakes
    for side, sy in (("L", 0.08), ("R", -0.08)):
        intake = add_cube(f"Shield_Intake_{side}", (-0.12, sy, 0.16), (0.12, 0.06, 0.05), mats["HullDark"])
        parts.append(intake)

    ship = join_objects(parts, name)
    shade_flat(ship)
    bi = bounds_info(ship)
    set_origin_at_world_point(ship, bi["center"])
    print(f"SHIELD post-origin size={bounds_info(ship)['size']}")
    make_empty("ShieldCore", ship, (0.40, 0.0, 0.02), display='SPHERE', size=0.05)
    make_empty("HitPoint", ship, (0.0, 0.0, 0.0), display='PLAIN_AXES', size=0.04)
    return ship


# -------------------- EXPORT / RENDER --------------------

def export_fbx(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    for child in obj.children:
        child.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=filepath, use_selection=True,
        apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True,
        use_space_transform=True, axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', use_mesh_modifiers=True,
        bake_space_transform=True, embed_textures=False,
        object_types={'MESH', 'EMPTY'}, add_leaf_bones=False,
    )
    print(f"FBX -> {filepath} ({os.path.getsize(filepath)} bytes)")


def export_obj(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    for c in obj.children:
        c.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.wm.obj_export(
        filepath=filepath, export_selected_objects=True,
        forward_axis='NEGATIVE_Z', up_axis='Y',
        export_materials=True, apply_modifiers=True,
    )
    print(f"OBJ -> {filepath}")


def setup_world(bg=(0.38, 0.44, 0.54, 1.0), strength=0.95):
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
    sun.data.energy = 3.6
    sun.rotation_euler = (math.radians(40), math.radians(15), math.radians(35))
    bpy.ops.object.light_add(type='AREA', location=(-3, -2, 4))
    fill = bpy.context.active_object
    fill.name = "Fill"
    fill.data.energy = 100
    fill.data.size = 4
    fill.rotation_euler = (math.radians(50), math.radians(-20), math.radians(-40))
    bpy.ops.object.light_add(type='AREA', location=(2, 3, 3))
    rim = bpy.context.active_object
    rim.name = "CoolRim"
    rim.data.energy = 55
    rim.data.size = 3
    rim.data.color = (0.75, 0.85, 1.0)


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
    return center, max(size, 0.3)


def place_camera_angle(cam, center, size, angle, pad=2.6):
    dist = size * pad
    z_off = size * 0.35
    angles = {"front": 180, "34": 140, "side": 90, "back": 0, "hero": 145}
    rad = math.radians(angles.get(angle, 145))
    cam.location = center + Vector((
        dist * math.sin(rad), -dist * math.cos(rad), z_off + size * 0.15,
    ))
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()


def render_angle(objs, out_path, angle="front", res=900, transparent=True, pad=2.6, bg=None):
    setup_world(*( (bg, 1.0) if bg else ((0.38, 0.44, 0.54, 1.0), 0.95) ))
    clear_cams_lights()
    setup_lights()
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    center, size = mesh_center_size(objs)
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
    center, size = mesh_center_size(objs)
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
        rad = math.radians((360.0 * i) / n_frames)
        cam.location = center + Vector((dist * math.sin(rad), -dist * math.cos(rad), z_off))
        cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
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


def validate_fbx(path, required_empties=None):
    hard_clear()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [(o.name, len(o.data.vertices), len(o.data.polygons),
               [s.name for s in o.material_slots if s.material])
              for o in bpy.data.objects if o.type == 'MESH']
    empties = [o.name for o in bpy.data.objects if o.type == 'EMPTY']
    mats = [m.name for m in bpy.data.materials]
    sz = os.path.getsize(path)
    need = set(required_empties or [])
    ok = sz > 2000 and len(meshes) >= 1 and need.issubset(set(empties))
    print(f"VALIDATE {os.path.basename(path)}: bytes={sz} meshes={meshes} "
          f"empties={empties} need={need} ok={ok}")
    return ok, meshes, empties, mats, sz


def multi_angle(obj, prefix, pad=2.6, do_turntable=False):
    outs = []
    for a in ("front", "34", "side", "back"):
        fp = os.path.join(PREVIEW_DIR, f"{prefix}_{a}.png")
        render_angle([obj], fp, angle=a, res=900, transparent=True, pad=pad)
        outs.append(fp)
    legacy = os.path.join(PREVIEW_DIR, f"{prefix}.png")
    shutil.copy2(os.path.join(PREVIEW_DIR, f"{prefix}_34.png"), legacy)
    outs.append(legacy)
    if do_turntable:
        frames_dir = os.path.join(PREVIEW_DIR, f"_tt_{prefix}")
        render_turntable_frames([obj], frames_dir, n_frames=24, res=480, pad=pad)
        gif = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.gif")
        mp4 = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.mp4")
        assemble_turntable(frames_dir, gif, mp4)
        outs.extend([gif, mp4])
    return outs


def rename_child_empties(obj, names):
    for c in obj.children:
        if c.type != 'EMPTY':
            continue
        for want in names:
            if c.name == want or c.name.startswith(want + "."):
                c.name = want
                break


def build_and_export(builder, asset_name, empty_names, fbx_name):
    hard_clear()
    obj = builder(asset_name)
    rename_child_empties(obj, empty_names)
    obj.location = (0, 0, 0)
    v, f = mesh_stats(obj)
    bi = bounds_info(obj)
    empties = [c.name for c in obj.children if c.type == 'EMPTY']
    print(f"{asset_name} verts={v} faces={f} size={bi['size']} empties={empties}")
    fbx = os.path.join(EXPORT_DIR, fbx_name + ".fbx")
    export_fbx(obj, fbx)
    export_obj(obj, os.path.join(EXPORT_DIR, fbx_name + ".obj"))
    return obj, {"verts": v, "faces": f, "size": bi["size"], "fbx": fbx, "empties": empties}


def main():
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(EXPORT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    stats = {}
    preview_files = []

    ms_empties = ["Spawn_Fast", "Spawn_Heavy", "Spawn_Shield", "HangarBay", "Bridge"]

    _, stats["mothership"] = build_and_export(
        build_mothership, "FC_Mothership", ms_empties, "FC_Mothership")
    hard_clear()
    ms = build_mothership("FC_Mothership")
    rename_child_empties(ms, ms_empties)
    ms.location = (0, 0, 0)
    preview_files += multi_angle(ms, "mothership", pad=2.7, do_turntable=True)
    render_angle([ms], os.path.join(PREVIEW_DIR, "hero.png"), angle="hero",
                 res=1024, transparent=False, pad=2.6, bg=(0.28, 0.30, 0.36, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "hero.png"))

    _, stats["fast"] = build_and_export(build_fast, "FC_Fast", ["HitPoint"], "FC_Fast")
    hard_clear()
    fast = build_fast("FC_Fast")
    rename_child_empties(fast, ["HitPoint"])
    fast.location = (0, 0, 0)
    preview_files += multi_angle(fast, "fast", pad=2.8)

    _, stats["heavy"] = build_and_export(
        build_heavy, "FC_Heavy", ["GunPoint", "HitPoint"], "FC_Heavy")
    hard_clear()
    heavy = build_heavy("FC_Heavy")
    rename_child_empties(heavy, ["GunPoint", "HitPoint"])
    heavy.location = (0, 0, 0)
    preview_files += multi_angle(heavy, "heavy", pad=2.8)

    _, stats["shield"] = build_and_export(
        build_shield, "FC_Shield", ["ShieldCore", "HitPoint"], "FC_Shield")
    hard_clear()
    shield = build_shield("FC_Shield")
    rename_child_empties(shield, ["ShieldCore", "HitPoint"])
    shield.location = (0, 0, 0)
    preview_files += multi_angle(shield, "shield", pad=2.8)

    # Kit board + shared blend
    hard_clear()
    ms = build_mothership("FC_Mothership")
    rename_child_empties(ms, ms_empties)
    fast = build_fast("FC_Fast")
    rename_child_empties(fast, ["HitPoint"])
    heavy = build_heavy("FC_Heavy")
    rename_child_empties(heavy, ["GunPoint", "HitPoint"])
    shield = build_shield("FC_Shield")
    rename_child_empties(shield, ["ShieldCore", "HitPoint"])

    ms.location = (0.0, 0.0, 0.0)
    fast.location = (0.0, 1.7, 0.35)
    heavy.location = (0.0, -1.7, 0.30)
    shield.location = (2.0, 0.0, 0.40)

    render_angle([ms, fast, heavy, shield],
                 os.path.join(PREVIEW_DIR, "kit_board.png"),
                 angle="hero", res=1100, transparent=False, pad=2.3,
                 bg=(0.26, 0.28, 0.34, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "kit_board.png"))

    ms.rotation_euler = (math.radians(2), math.radians(-3), math.radians(12))
    fast.rotation_euler = (0, 0, math.radians(18))
    heavy.rotation_euler = (0, 0, math.radians(-12))
    shield.rotation_euler = (0, math.radians(8), math.radians(22))

    blend_path = os.path.join(BLEND_DIR, "FleetCommanderPlay.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved {blend_path}")

    hero = os.path.join(PREVIEW_DIR, "hero.png")
    if os.path.isfile(hero) and os.path.isdir(os.path.dirname(LIVE_FEED)):
        shutil.copy2(hero, LIVE_FEED)
        print(f"LIVE FEED -> {LIVE_FEED} ({os.path.getsize(LIVE_FEED)} bytes)")

    print("\n=== VALIDATION ===")
    ok_m, m_m, e_m, mats_m, sz_m = validate_fbx(
        stats["mothership"]["fbx"], ["Spawn_Fast", "Spawn_Heavy", "Spawn_Shield"])
    ok_f, m_f, e_f, mats_f, sz_f = validate_fbx(stats["fast"]["fbx"], ["HitPoint"])
    ok_h, m_h, e_h, mats_h, sz_h = validate_fbx(stats["heavy"]["fbx"], ["GunPoint"])
    ok_s, m_s, e_s, mats_s, sz_s = validate_fbx(stats["shield"]["fbx"], ["ShieldCore"])
    all_ok = ok_m and ok_f and ok_h and ok_s

    print("\n=== STATS ===")
    for k, v in stats.items():
        print(k.upper(), v)
    print(f"FBX mothership bytes={sz_m} ok={ok_m} empties={e_m}")
    print(f"FBX fast bytes={sz_f} ok={ok_f} empties={e_f}")
    print(f"FBX heavy bytes={sz_h} ok={ok_h} empties={e_h}")
    print(f"FBX shield bytes={sz_s} ok={ok_s} empties={e_s}")
    print("ALL_OK=", all_ok)

    stats_path = os.path.join(WORK, "scripts", "_last_stats.txt")
    with open(stats_path, "w") as f:
        for key in ("mothership", "fast", "heavy", "shield"):
            s = stats[key]
            f.write(f"{key}_verts={s['verts']}\n")
            f.write(f"{key}_faces={s['faces']}\n")
            f.write(f"{key}_size={s['size']}\n")
            f.write(f"{key}_empties={s['empties']}\n")
        f.write(f"mothership_fbx_bytes={sz_m}\nfast_fbx_bytes={sz_f}\n")
        f.write(f"heavy_fbx_bytes={sz_h}\nshield_fbx_bytes={sz_s}\n")
        f.write(f"mothership_meshes={m_m}\nfast_meshes={m_f}\n")
        f.write(f"heavy_meshes={m_h}\nshield_meshes={m_s}\n")
        f.write(f"ok={all_ok}\n")
    print(f"Wrote {stats_path}")
    print("PREVIEW_FILES:")
    for p in sorted(set(preview_files)):
        print(" ", p, os.path.getsize(p) if os.path.isfile(p) else "MISSING")


if __name__ == "__main__":
    main()
