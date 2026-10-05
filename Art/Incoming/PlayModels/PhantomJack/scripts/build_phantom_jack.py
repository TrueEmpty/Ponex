"""
Phantom Jack Cararrow — ghost/raft → fleet PLAY pack (separate alt playable).

CRITICAL LOCKS:
  - Playable name: Phantom Jack Cararrow. Story identity still Jack Cararrow.
  - HERO/person = 2D fullbody only later — NO 3D person FBX in this package.
  - PLAY = stage meshes only (raft→fleet). Distinct from living Jack_Ship / Jack_Raft.
  - Stage 6 form label "Fleet commander" ≠ separate playable Fleet Commander.
  - Ghost vibe: desaturated blue-gray / blackish wood + faint cyan / pale glow.

Stages (bible):
  1 Phantom_Raft          move only
  2 Phantom_LittleBoat    faster, no attack
  3 Phantom_Sailboat      1 cannon Empty
  4 Phantom_Warship       triple cannon Empties
  5 Phantom_Carrier       hangar + plane Empties (+ cannon stubs OK)
  6 Phantom_Fleet         carrier + sentry presence (chunky silhouette)

Blender 4.x:
  /usr/bin/blender -b -P /workspace/ponex-playmodels/phantom-jack/scripts/build_phantom_jack.py
"""
import bpy
import bmesh
import math
import os
import shutil
import subprocess
from mathutils import Vector

WORK = "/workspace/ponex-playmodels/phantom-jack"
BLEND_DIR = os.path.join(WORK, "blend")
EXPORT_DIR = os.path.join(WORK, "export")
PREVIEW_DIR = os.path.join(WORK, "preview")
LIVE_FEED = "/workspace/ponex-forest-kit/live/latest.png"

# Ghost / undead palette — distinct from warm living Jack
COLORS = {
    "GhostWood":    (0.28, 0.32, 0.38),   # #47515F desat blue-gray wood
    "GhostDark":    (0.10, 0.11, 0.14),   # #1A1C24 blackish
    "GhostDeck":    (0.36, 0.40, 0.46),   # #5C6675 lighter deck
    "GhostHull":    (0.22, 0.26, 0.32),   # #384252 mid hull
    "GhostSail":    (0.55, 0.62, 0.70),   # #8C9EB2 pale sail
    "GhostMetal":   (0.18, 0.20, 0.24),   # #2E333D metal/ports
    "CyanGlow":     (0.35, 0.85, 0.95),   # #59D9F2 faint cyan accent
    "PaleGlow":     (0.70, 0.88, 0.95),   # #B2E0F2 pale glow
    "Blackish":     (0.06, 0.07, 0.09),   # #0F1217 accent black
    "RopeGhost":    (0.40, 0.42, 0.38),   # #666B61 muted rope
}
ROUGH = {
    "GhostWood": 0.72, "GhostDark": 0.78, "GhostDeck": 0.68, "GhostHull": 0.74,
    "GhostSail": 0.55, "GhostMetal": 0.55, "CyanGlow": 0.35, "PaleGlow": 0.30,
    "Blackish": 0.85, "RopeGhost": 0.75,
}
# Subtle emission strength for glow mats
EMISSION = {
    "CyanGlow": 0.45,
    "PaleGlow": 0.30,
}


def hard_clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.objects,
                 bpy.data.cameras, bpy.data.lights, bpy.data.curves):
        for b in list(coll):
            coll.remove(b)


def make_material(name, color, roughness=0.65, emission=0.0):
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
    # Emission (Blender 4 Principled has Emission Color + Strength)
    if emission > 0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*color, 1.0)
            bsdf.inputs["Emission Strength"].default_value = emission
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = (*color, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def get_or_make(name):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    return make_material(
        name, COLORS[name], ROUGH.get(name, 0.65), EMISSION.get(name, 0.0)
    )


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


def add_cone(name, loc, radius1, depth, mat, verts=10, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=verts, radius1=radius1, radius2=0.0, depth=depth, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    apply_mat(obj, mat)
    return obj


def taper_hull_mesh(obj, bow_scale=0.35, stern_scale=0.55):
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    xs = [v.co.x for v in bm.verts]
    x_min, x_max = min(xs), max(xs)
    x_span = max(x_max - x_min, 1e-6)
    for v in bm.verts:
        t = (v.co.x - x_min) / x_span
        if t < 0.22:
            u = t / 0.22
            s = stern_scale + (1.0 - stern_scale) * u
        elif t > 0.72:
            u = (t - 0.72) / 0.28
            s = 1.0 + (bow_scale - 1.0) * u
        else:
            s = 1.0
        mid = 1.0 - abs((t - 0.45) / 0.45) * 0.08
        s *= mid
        v.co.y *= s
        if v.co.z < 0:
            v.co.y *= 0.85 + 0.15 * s
            v.co.z *= 0.92
        if t > 0.85:
            v.co.z += 0.04 * ((t - 0.85) / 0.15)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def make_empty(name, parent, loc, display='PLAIN_AXES', size=0.08):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = display
    emp.empty_display_size = size
    bpy.context.collection.objects.link(emp)
    emp.parent = parent
    emp.location = loc
    return emp


def finish_root(obj, empties):
    """Origin waterline center; attach empties as (name, loc, display, size)."""
    bi = bounds_info(obj)
    origin = (bi["center"][0], bi["center"][1], 0.0)
    set_origin_at_world_point(obj, origin)
    for spec in empties:
        n, loc = spec[0], spec[1]
        disp = spec[2] if len(spec) > 2 else 'PLAIN_AXES'
        sz = spec[3] if len(spec) > 3 else 0.08
        make_empty(n, obj, loc, display=disp, size=sz)
    bi2 = bounds_info(obj)
    print(f"{obj.name} size={bi2['size']} verts={mesh_stats(obj)[0]} faces={mesh_stats(obj)[1]}")
    return obj


# ===================== STAGE BUILDERS =====================

def build_phantom_raft(name="Phantom_Raft"):
    """Stage 1 — move-only silhouette ~1.2 m ghost raft."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    for i, py in enumerate((-0.26, -0.13, 0.0, 0.13, 0.26)):
        mat = mats["GhostWood"] if i % 2 == 0 else mats["GhostDeck"]
        p = add_cube(f"PPlank_{i}", (0.0, py, 0.05), (1.05, 0.11, 0.07), mat)
        shade_flat(p)
        parts.append(p)
    for i, px in enumerate((-0.38, 0.0, 0.38)):
        parts.append(add_cube(f"PBeam_{i}", (px, 0.0, 0.0), (0.07, 0.58, 0.05), mats["GhostDark"]))
    for i, loc in enumerate([(-0.40, 0.28, -0.05), (-0.40, -0.28, -0.05),
                             (0.40, 0.28, -0.05), (0.40, -0.28, -0.05)]):
        log = add_cylinder(f"PLog_{i}", loc, 0.08, 0.26, mats["GhostHull"], verts=10,
                           rotation=(0, math.radians(90), 0))
        shade_smooth(log)
        parts.append(log)
    for side, sy in (("L", 0.32), ("R", -0.32)):
        r = add_cylinder(f"PRun_{side}", (0.0, sy, -0.02), 0.065, 0.95, mats["GhostDark"],
                         verts=10, rotation=(0, math.radians(90), 0))
        shade_smooth(r)
        parts.append(r)
    # Cyan glow seam
    glow = add_cube("GlowSeam", (0.0, 0.0, 0.09), (0.90, 0.02, 0.02), mats["CyanGlow"])
    parts.append(glow)
    mast = add_cylinder("PMast", (-0.12, 0.0, 0.38), 0.022, 0.48, mats["GhostDark"], verts=10)
    parts.append(mast)
    sail = add_cube("PSail", (-0.09, 0.0, 0.38), (0.025, 0.26, 0.24), mats["GhostSail"])
    parts.append(sail)
    tip = add_cube("GlowTip", (-0.12, 0.0, 0.64), (0.04, 0.04, 0.04), mats["PaleGlow"])
    parts.append(tip)
    root = join_objects(parts, name)
    shade_flat(root)
    return finish_root(root, [
        ("AnchorPoint", (0.42, 0.0, 0.18), 'PLAIN_AXES', 0.08),
    ])


def build_phantom_little_boat(name="Phantom_LittleBoat"):
    """Stage 2 — small open boat, faster silhouette, no attack mounts."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    hull = add_cube("LB_Hull", (0.0, 0.0, 0.05), (1.20, 0.42, 0.28), mats["GhostHull"])
    bpy.ops.object.select_all(action='DESELECT')
    hull.select_set(True)
    bpy.context.view_layer.objects.active = hull
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=1)
    bpy.ops.object.mode_set(mode='OBJECT')
    taper_hull_mesh(hull, bow_scale=0.30, stern_scale=0.55)
    shade_smooth(hull)
    parts.append(hull)
    keel = add_cube("LB_Keel", (0.0, 0.0, -0.12), (1.00, 0.10, 0.10), mats["GhostDark"])
    taper_hull_mesh(keel, bow_scale=0.28, stern_scale=0.50)
    parts.append(keel)
    deck = add_cube("LB_Deck", (0.0, 0.0, 0.16), (1.05, 0.32, 0.04), mats["GhostDeck"])
    taper_hull_mesh(deck, bow_scale=0.40, stern_scale=0.65)
    parts.append(deck)
    # Gunwales
    for side, sy in (("L", 0.18), ("R", -0.18)):
        parts.append(add_cube(f"LB_Rail_{side}", (0.0, sy, 0.22), (0.95, 0.03, 0.06), mats["GhostWood"]))
    prow = add_cone("LB_Prow", (0.68, 0.0, 0.08), 0.10, 0.22, mats["GhostHull"], verts=10,
                    rotation=(0, math.radians(90), 0))
    shade_smooth(prow)
    parts.append(prow)
    # Pale glow waterline stripe
    parts.append(add_cube("LB_Glow", (0.0, 0.0, 0.02), (1.10, 0.43, 0.03), mats["CyanGlow"]))
    # Tiny thwart seats
    for i, px in enumerate((-0.25, 0.20)):
        parts.append(add_cube(f"LB_Seat_{i}", (px, 0.0, 0.20), (0.06, 0.28, 0.04), mats["GhostWood"]))
    rudder = add_cube("LB_Rudder", (-0.65, 0.0, 0.0), (0.06, 0.03, 0.18), mats["GhostDark"])
    parts.append(rudder)
    root = join_objects(parts, name)
    return finish_root(root, [
        ("AnchorPoint", (0.45, 0.0, 0.22), 'PLAIN_AXES', 0.08),
    ])


def build_phantom_sailboat(name="Phantom_Sailboat"):
    """Stage 3 — sailboat/ship with 1 cannon Empty."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    for hn, hloc, hsc in [
        ("SB_Aft", (-0.40, 0.0, 0.0), (0.50, 0.38, 0.34)),
        ("SB_Mid", (0.05, 0.0, 0.0), (0.50, 0.42, 0.36)),
        ("SB_Fwd", (0.45, 0.0, 0.0), (0.40, 0.34, 0.34)),
    ]:
        h = add_cube(hn, hloc, hsc, mats["GhostHull"])
        bpy.ops.object.select_all(action='DESELECT')
        h.select_set(True)
        bpy.context.view_layer.objects.active = h
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.subdivide(number_cuts=1)
        bpy.ops.object.mode_set(mode='OBJECT')
        taper_hull_mesh(h, bow_scale=0.34, stern_scale=0.58)
        shade_smooth(h)
        parts.append(h)
    keel = add_cube("SB_Keel", (0.0, 0.0, -0.14), (1.20, 0.24, 0.12), mats["GhostDark"])
    taper_hull_mesh(keel, bow_scale=0.30, stern_scale=0.52)
    parts.append(keel)
    deck = add_cube("SB_Deck", (0.0, 0.0, 0.18), (1.15, 0.32, 0.04), mats["GhostDeck"])
    taper_hull_mesh(deck, bow_scale=0.42, stern_scale=0.68)
    parts.append(deck)
    parts.append(add_cube("SB_GlowStripe", (0.0, 0.0, 0.05), (1.22, 0.42, 0.04), mats["CyanGlow"]))
    cabin = add_cube("SB_Cabin", (-0.20, 0.0, 0.36), (0.38, 0.28, 0.26), mats["GhostWood"])
    parts.append(cabin)
    parts.append(add_cube("SB_Roof", (-0.20, 0.0, 0.50), (0.42, 0.32, 0.04), mats["GhostDark"]))
    mast = add_cylinder("SB_Mast", (0.12, 0.0, 0.78), 0.035, 1.10, mats["GhostDark"], verts=12)
    shade_smooth(mast)
    parts.append(mast)
    yard = add_cylinder("SB_Yard", (0.12, 0.0, 0.95), 0.018, 0.80, mats["RopeGhost"], verts=8,
                        rotation=(math.radians(90), 0, 0))
    parts.append(yard)
    sail = add_cube("SB_Sail", (0.12, 0.0, 0.65), (0.035, 0.68, 0.60), mats["GhostSail"])
    shade_smooth(sail)
    parts.append(sail)
    # Glow sail edge
    parts.append(add_cube("SB_SailGlow", (0.14, 0.0, 0.65), (0.015, 0.70, 0.04), mats["PaleGlow"]))
    prow = add_cone("SB_Prow", (0.72, 0.0, 0.08), 0.12, 0.28, mats["GhostHull"], verts=10,
                    rotation=(0, math.radians(90), 0))
    shade_smooth(prow)
    parts.append(prow)
    parts.append(add_cube("SB_Stern", (-0.68, 0.0, 0.10), (0.08, 0.30, 0.28), mats["GhostWood"]))
    parts.append(add_cube("SB_Rudder", (-0.76, 0.0, -0.02), (0.07, 0.035, 0.22), mats["GhostDark"]))
    # Single port ring (silhouette)
    ring = add_cylinder("SB_Port", (0.12, 0.22, 0.04), 0.05, 0.035, mats["GhostMetal"], verts=12,
                        rotation=(math.radians(90), 0, 0))
    shade_smooth(ring)
    parts.append(ring)
    tip = add_cube("SB_GlowTip", (0.12, 0.0, 1.35), (0.05, 0.05, 0.05), mats["PaleGlow"])
    parts.append(tip)
    root = join_objects(parts, name)
    return finish_root(root, [
        ("Cannon_01", (0.12, 0.24, 0.04), 'SPHERE', 0.07),
        ("AnchorPoint", (0.48, 0.0, 0.28), 'PLAIN_AXES', 0.08),
    ])


def build_phantom_warship(name="Phantom_Warship"):
    """Stage 4 — chunkier warship, triple cannon Empties."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    for hn, hloc, hsc in [
        ("WS_Aft", (-0.55, 0.0, 0.02), (0.60, 0.48, 0.42)),
        ("WS_Mid", (0.05, 0.0, 0.02), (0.65, 0.54, 0.44)),
        ("WS_Fwd", (0.60, 0.0, 0.02), (0.50, 0.42, 0.40)),
    ]:
        h = add_cube(hn, hloc, hsc, mats["GhostHull"])
        bpy.ops.object.select_all(action='DESELECT')
        h.select_set(True)
        bpy.context.view_layer.objects.active = h
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.subdivide(number_cuts=1)
        bpy.ops.object.mode_set(mode='OBJECT')
        taper_hull_mesh(h, bow_scale=0.38, stern_scale=0.62)
        shade_smooth(h)
        parts.append(h)
    keel = add_cube("WS_Keel", (0.0, 0.0, -0.18), (1.55, 0.30, 0.14), mats["GhostDark"])
    taper_hull_mesh(keel, bow_scale=0.32, stern_scale=0.55)
    parts.append(keel)
    deck = add_cube("WS_Deck", (0.0, 0.0, 0.24), (1.50, 0.40, 0.05), mats["GhostDeck"])
    taper_hull_mesh(deck, bow_scale=0.45, stern_scale=0.70)
    parts.append(deck)
    parts.append(add_cube("WS_Glow", (0.0, 0.0, 0.06), (1.55, 0.52, 0.045), mats["CyanGlow"]))
    # Raised forecastle / aftcastle
    parts.append(add_cube("WS_AftCastle", (-0.55, 0.0, 0.48), (0.45, 0.38, 0.32), mats["GhostWood"]))
    parts.append(add_cube("WS_ForeCastle", (0.50, 0.0, 0.42), (0.35, 0.34, 0.24), mats["GhostWood"]))
    # Twin masts
    for i, px in enumerate((-0.20, 0.30)):
        m = add_cylinder(f"WS_Mast_{i}", (px, 0.0, 0.95), 0.038, 1.25, mats["GhostDark"], verts=12)
        shade_smooth(m)
        parts.append(m)
        y = add_cylinder(f"WS_Yard_{i}", (px, 0.0, 1.15), 0.016, 0.85, mats["RopeGhost"], verts=8,
                         rotation=(math.radians(90), 0, 0))
        parts.append(y)
        s = add_cube(f"WS_Sail_{i}", (px, 0.0, 0.80), (0.03, 0.72, 0.55), mats["GhostSail"])
        parts.append(s)
    prow = add_cone("WS_Prow", (0.95, 0.0, 0.10), 0.14, 0.32, mats["GhostHull"], verts=10,
                    rotation=(0, math.radians(90), 0))
    shade_smooth(prow)
    parts.append(prow)
    parts.append(add_cube("WS_Ram", (1.05, 0.0, -0.02), (0.18, 0.08, 0.10), mats["GhostMetal"]))
    # Triple port rings
    port_specs = [
        ("WS_Port_01", (0.20, 0.28, 0.06)),
        ("WS_Port_02", (0.20, -0.28, 0.06)),
        ("WS_Port_03", (-0.25, 0.28, 0.06)),
    ]
    for pname, ploc in port_specs:
        ring = add_cylinder(pname, ploc, 0.055, 0.04, mats["GhostMetal"], verts=12,
                            rotation=(math.radians(90), 0, 0))
        shade_smooth(ring)
        parts.append(ring)
        stub = add_cylinder(pname.replace("Port", "Stub"), ploc, 0.028, 0.07, mats["Blackish"],
                            verts=10, rotation=(math.radians(90), 0, 0))
        parts.append(stub)
    parts.append(add_cube("WS_GlowTip", (-0.20, 0.0, 1.55), (0.06, 0.06, 0.06), mats["PaleGlow"]))
    root = join_objects(parts, name)
    return finish_root(root, [
        ("Cannon_01", (0.20, 0.30, 0.06), 'SPHERE', 0.07),
        ("Cannon_02", (0.20, -0.30, 0.06), 'SPHERE', 0.07),
        ("Cannon_03", (-0.25, 0.30, 0.06), 'SPHERE', 0.07),
        ("AnchorPoint", (0.70, 0.0, 0.35), 'PLAIN_AXES', 0.09),
    ])


def build_phantom_carrier(name="Phantom_Carrier"):
    """Stage 5 — flat-top carrier silhouette + hangar + plane Empties."""
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    # Wide flat hull
    hull = add_cube("CV_Hull", (0.0, 0.0, 0.0), (2.00, 0.70, 0.40), mats["GhostHull"])
    bpy.ops.object.select_all(action='DESELECT')
    hull.select_set(True)
    bpy.context.view_layer.objects.active = hull
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=1)
    bpy.ops.object.mode_set(mode='OBJECT')
    taper_hull_mesh(hull, bow_scale=0.55, stern_scale=0.70)
    shade_smooth(hull)
    parts.append(hull)
    keel = add_cube("CV_Keel", (0.0, 0.0, -0.22), (1.80, 0.35, 0.12), mats["GhostDark"])
    parts.append(keel)
    # Flight deck (flat top — readable carrier)
    deck = add_cube("CV_Deck", (0.0, 0.0, 0.28), (2.05, 0.72, 0.06), mats["GhostDeck"])
    shade_flat(deck)
    parts.append(deck)
    # Deck stripe glow (runway)
    parts.append(add_cube("CV_Runway", (0.0, 0.0, 0.32), (1.90, 0.08, 0.02), mats["CyanGlow"]))
    parts.append(add_cube("CV_RunEdgeL", (0.0, 0.28, 0.32), (1.80, 0.02, 0.015), mats["PaleGlow"]))
    parts.append(add_cube("CV_RunEdgeR", (0.0, -0.28, 0.32), (1.80, 0.02, 0.015), mats["PaleGlow"]))
    # Island / bridge (starboard offset)
    island = add_cube("CV_Island", (-0.20, -0.28, 0.55), (0.45, 0.22, 0.40), mats["GhostWood"])
    parts.append(island)
    parts.append(add_cube("CV_Bridge", (-0.20, -0.28, 0.80), (0.35, 0.18, 0.14), mats["GhostDark"]))
    parts.append(add_cube("CV_Radar", (-0.20, -0.28, 0.95), (0.08, 0.08, 0.12), mats["GhostMetal"]))
    tip = add_cube("CV_GlowTip", (-0.20, -0.28, 1.05), (0.05, 0.05, 0.05), mats["PaleGlow"])
    parts.append(tip)
    # Hangar mouth (aft dark recess)
    hangar = add_cube("CV_HangarMouth", (-0.85, 0.0, 0.10), (0.20, 0.40, 0.22), mats["Blackish"])
    parts.append(hangar)
    # Glow hangar lip
    parts.append(add_cube("CV_HangarGlow", (-0.75, 0.0, 0.10), (0.04, 0.42, 0.24), mats["CyanGlow"]))
    # Bow
    prow = add_cone("CV_Prow", (1.10, 0.0, 0.05), 0.18, 0.30, mats["GhostHull"], verts=10,
                    rotation=(0, math.radians(90), 0))
    shade_smooth(prow)
    parts.append(prow)
    # Side cannon stubs (silhouette)
    for i, (px, py) in enumerate([(0.3, 0.38), (0.3, -0.38), (-0.3, 0.38)]):
        ring = add_cylinder(f"CV_Port_{i}", (px, py, 0.05), 0.045, 0.035, mats["GhostMetal"],
                            verts=10, rotation=(math.radians(90), 0, 0))
        parts.append(ring)
    # Chunky parked-plane silhouette blocks (not detailed swarm)
    for i, (px, py) in enumerate([(0.55, 0.18), (0.25, -0.15), (-0.10, 0.12)]):
        body = add_cube(f"CV_PlaneBody_{i}", (px, py, 0.40), (0.22, 0.06, 0.05), mats["GhostMetal"])
        parts.append(body)
        wing = add_cube(f"CV_PlaneWing_{i}", (px, py, 0.40), (0.06, 0.20, 0.02), mats["GhostDark"])
        parts.append(wing)
    root = join_objects(parts, name)
    return finish_root(root, [
        ("HangarPoint", (-0.80, 0.0, 0.12), 'CUBE', 0.10),
        ("Plane_01", (0.55, 0.18, 0.42), 'SPHERE', 0.06),
        ("Plane_02", (0.25, -0.15, 0.42), 'SPHERE', 0.06),
        ("Plane_03", (-0.10, 0.12, 0.42), 'SPHERE', 0.06),
        ("Cannon_01", (0.30, 0.40, 0.05), 'SPHERE', 0.06),
        ("Cannon_02", (0.30, -0.40, 0.05), 'SPHERE', 0.06),
        ("Cannon_03", (-0.30, 0.40, 0.05), 'SPHERE', 0.06),
        ("AnchorPoint", (0.90, 0.0, 0.35), 'PLAIN_AXES', 0.09),
    ])


def build_phantom_fleet(name="Phantom_Fleet"):
    """
    Stage 6 — Fleet commander form: carrier base + sentry presence chunky silhouette.
    Empty mounts for planes + sentry. NOT the separate Fleet Commander playable.
    """
    mats = {k: get_or_make(k) for k in COLORS}
    parts = []
    # Slightly larger carrier base
    hull = add_cube("FL_Hull", (0.0, 0.0, 0.0), (2.20, 0.78, 0.45), mats["GhostHull"])
    bpy.ops.object.select_all(action='DESELECT')
    hull.select_set(True)
    bpy.context.view_layer.objects.active = hull
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=1)
    bpy.ops.object.mode_set(mode='OBJECT')
    taper_hull_mesh(hull, bow_scale=0.58, stern_scale=0.72)
    shade_smooth(hull)
    parts.append(hull)
    parts.append(add_cube("FL_Keel", (0.0, 0.0, -0.25), (2.00, 0.38, 0.14), mats["GhostDark"]))
    deck = add_cube("FL_Deck", (0.0, 0.0, 0.30), (2.25, 0.80, 0.06), mats["GhostDeck"])
    shade_flat(deck)
    parts.append(deck)
    parts.append(add_cube("FL_Runway", (0.0, 0.0, 0.34), (2.10, 0.10, 0.02), mats["CyanGlow"]))
    parts.append(add_cube("FL_RunL", (0.0, 0.32, 0.34), (2.00, 0.025, 0.015), mats["PaleGlow"]))
    parts.append(add_cube("FL_RunR", (0.0, -0.32, 0.34), (2.00, 0.025, 0.015), mats["PaleGlow"]))
    # Command island
    island = add_cube("FL_Island", (-0.25, -0.32, 0.60), (0.50, 0.24, 0.48), mats["GhostWood"])
    parts.append(island)
    parts.append(add_cube("FL_Bridge", (-0.25, -0.32, 0.90), (0.40, 0.20, 0.16), mats["GhostDark"]))
    # SENTRY presence — chunky turret silhouette (mid-forward deck)
    sentry_base = add_cylinder("FL_SentryBase", (0.55, 0.0, 0.42), 0.12, 0.14, mats["GhostMetal"], verts=12)
    shade_smooth(sentry_base)
    parts.append(sentry_base)
    sentry_body = add_cube("FL_SentryBody", (0.55, 0.0, 0.58), (0.22, 0.22, 0.18), mats["Blackish"])
    parts.append(sentry_body)
    # Twin barrels (chunky stubs, not detailed)
    for i, py in enumerate((0.05, -0.05)):
        barrel = add_cylinder(f"FL_SentryBarrel_{i}", (0.72, py, 0.58), 0.035, 0.28,
                              mats["GhostMetal"], verts=10, rotation=(0, math.radians(90), 0))
        shade_smooth(barrel)
        parts.append(barrel)
    # Cyan sentry glow ring
    glow_ring = add_cylinder("FL_SentryGlow", (0.55, 0.0, 0.50), 0.14, 0.03, mats["CyanGlow"], verts=14)
    parts.append(glow_ring)
    # Hangar
    parts.append(add_cube("FL_Hangar", (-0.95, 0.0, 0.10), (0.22, 0.45, 0.24), mats["Blackish"]))
    parts.append(add_cube("FL_HangarGlow", (-0.84, 0.0, 0.10), (0.04, 0.48, 0.26), mats["CyanGlow"]))
    # Plane silhouettes
    for i, (px, py) in enumerate([(0.80, 0.22), (0.40, -0.20), (0.10, 0.18), (-0.20, -0.10)]):
        body = add_cube(f"FL_Plane_{i}", (px, py, 0.42), (0.20, 0.055, 0.045), mats["GhostMetal"])
        parts.append(body)
        wing = add_cube(f"FL_Wing_{i}", (px, py, 0.42), (0.05, 0.18, 0.018), mats["GhostDark"])
        parts.append(wing)
    # Side ports
    for i, (px, py) in enumerate([(0.2, 0.42), (0.2, -0.42), (-0.35, 0.42)]):
        ring = add_cylinder(f"FL_Port_{i}", (px, py, 0.05), 0.05, 0.04, mats["GhostMetal"],
                            verts=10, rotation=(math.radians(90), 0, 0))
        parts.append(ring)
    prow = add_cone("FL_Prow", (1.22, 0.0, 0.05), 0.20, 0.32, mats["GhostHull"], verts=10,
                    rotation=(0, math.radians(90), 0))
    shade_smooth(prow)
    parts.append(prow)
    tip = add_cube("FL_GlowTip", (-0.25, -0.32, 1.10), (0.06, 0.06, 0.06), mats["PaleGlow"])
    parts.append(tip)
    # Extra ghost "fleet escort" nubs near stern (chunky presence, not separate ships)
    for i, (px, py) in enumerate([(-0.70, 0.55), (-0.70, -0.55)]):
        escort = add_cube(f"FL_Escort_{i}", (px, py, 0.15), (0.35, 0.12, 0.18), mats["GhostDark"])
        parts.append(escort)
        eg = add_cube(f"FL_EscortGlow_{i}", (px, py, 0.22), (0.30, 0.02, 0.02), mats["CyanGlow"])
        parts.append(eg)
    root = join_objects(parts, name)
    return finish_root(root, [
        ("SentryPoint", (0.55, 0.0, 0.60), 'SPHERE', 0.10),
        ("HangarPoint", (-0.90, 0.0, 0.12), 'CUBE', 0.10),
        ("Plane_01", (0.80, 0.22, 0.44), 'SPHERE', 0.06),
        ("Plane_02", (0.40, -0.20, 0.44), 'SPHERE', 0.06),
        ("Plane_03", (0.10, 0.18, 0.44), 'SPHERE', 0.06),
        ("Plane_04", (-0.20, -0.10, 0.44), 'SPHERE', 0.06),
        ("Cannon_01", (0.20, 0.44, 0.05), 'SPHERE', 0.06),
        ("Cannon_02", (0.20, -0.44, 0.05), 'SPHERE', 0.06),
        ("Cannon_03", (-0.35, 0.44, 0.05), 'SPHERE', 0.06),
        ("AnchorPoint", (1.00, 0.0, 0.38), 'PLAIN_AXES', 0.09),
    ])


STAGES = [
    ("Phantom_Raft", 1, build_phantom_raft, "raft"),
    ("Phantom_LittleBoat", 2, build_phantom_little_boat, "littleboat"),
    ("Phantom_Sailboat", 3, build_phantom_sailboat, "sailboat"),
    ("Phantom_Warship", 4, build_phantom_warship, "warship"),
    ("Phantom_Carrier", 5, build_phantom_carrier, "carrier"),
    ("Phantom_Fleet", 6, build_phantom_fleet, "fleet"),
]


# -------------------- EXPORT / RENDER --------------------

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


def setup_world(bg=(0.22, 0.26, 0.34, 1.0), strength=0.85):
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
    sun.data.energy = 2.8
    sun.data.color = (0.85, 0.92, 1.0)
    sun.rotation_euler = (math.radians(40), math.radians(15), math.radians(35))
    bpy.ops.object.light_add(type='AREA', location=(-3, -2, 4))
    fill = bpy.context.active_object
    fill.name = "Fill"
    fill.data.energy = 70
    fill.data.size = 4
    fill.data.color = (0.7, 0.85, 1.0)
    bpy.ops.object.light_add(type='AREA', location=(2, 3, 2.5))
    rim = bpy.context.active_object
    rim.name = "CyanRim"
    rim.data.energy = 60
    rim.data.size = 3
    rim.data.color = (0.45, 0.90, 1.0)


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
    return center, size


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
    if bg:
        setup_world(bg, 1.0)
    else:
        setup_world()
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
    print(f"PREVIEW [{angle}] -> {out_path}")


def render_turntable_frames(objs, frames_dir, n_frames=20, res=420, pad=2.5):
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


def assemble_turntable(frames_dir, out_gif, out_mp4=None, fps=10):
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
    if out_mp4:
        subprocess.run([
            "ffmpeg", "-y", "-framerate", str(fps), "-i", pattern,
            "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "23",
            "-movflags", "+faststart", out_mp4
        ], check=True, capture_output=True)


def multi_angle_and_turntable(obj, prefix, pad=2.6, do_tt=True):
    outs = []
    for a in ("front", "34", "side", "back"):
        fp = os.path.join(PREVIEW_DIR, f"{prefix}_{a}.png")
        render_angle([obj], fp, angle=a, res=800, transparent=True, pad=pad)
        outs.append(fp)
    legacy = os.path.join(PREVIEW_DIR, f"{prefix}.png")
    shutil.copy2(os.path.join(PREVIEW_DIR, f"{prefix}_34.png"), legacy)
    outs.append(legacy)
    if do_tt:
        frames_dir = os.path.join(PREVIEW_DIR, f"_tt_{prefix}")
        render_turntable_frames([obj], frames_dir, n_frames=20, res=400, pad=pad)
        gif = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.gif")
        mp4 = os.path.join(PREVIEW_DIR, f"{prefix}_turntable.mp4")
        assemble_turntable(frames_dir, gif, mp4)
        outs.extend([gif, mp4])
    return outs


def rename_stage_empties(root, expected):
    """Normalize Empty names (Blender may suffix .001)."""
    for c in root.children:
        if c.type != 'EMPTY':
            continue
        base = c.name.split(".")[0]
        for e in expected:
            if base == e or c.name.startswith(e):
                c.name = e
                break


def validate_fbx(path):
    hard_clear()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [(o.name, len(o.data.vertices), len(o.data.polygons),
               [s.name for s in o.material_slots if s.material])
              for o in bpy.data.objects if o.type == 'MESH']
    empties = sorted(o.name for o in bpy.data.objects if o.type == 'EMPTY')
    sz = os.path.getsize(path)
    ok = sz > 1500 and len(meshes) >= 1
    print(f"VALIDATE {os.path.basename(path)}: bytes={sz} meshes={meshes} empties={empties} ok={ok}")
    return ok, meshes, empties, sz


def main():
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(EXPORT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    all_stats = {}
    preview_files = []
    built_roots = []  # (name, stage, builder) rebuilt later for blend

    for asset_name, stage, builder, prefix in STAGES:
        print(f"\n======== STAGE {stage}: {asset_name} ========")
        hard_clear()
        # Ensure mats fresh
        for k in COLORS:
            get_or_make(k)
        obj = builder(asset_name)
        obj.location = (0, 0, 0)
        expected_empties = [c.name.split(".")[0] for c in obj.children if c.type == 'EMPTY']
        rename_stage_empties(obj, expected_empties)
        # Re-collect after rename
        empty_names = sorted(c.name for c in obj.children if c.type == 'EMPTY')
        rv, rf = mesh_stats(obj)
        ri = bounds_info(obj)
        fbx_path = os.path.join(EXPORT_DIR, f"{asset_name}.fbx")
        export_fbx(obj, fbx_path)
        export_obj(obj, os.path.join(EXPORT_DIR, f"{asset_name}.obj"))
        # Turntables for hero stages (1 raft + 6 fleet) + sailboat; angles for all
        do_tt = stage in (1, 3, 6)
        preview_files += multi_angle_and_turntable(obj, prefix, pad=2.7 if stage <= 2 else 2.5,
                                                    do_tt=do_tt)
        # Opaque hero for key stages
        if stage in (1, 6):
            hero_name = "hero.png" if stage == 6 else "raft_hero.png"
            render_angle([obj], os.path.join(PREVIEW_DIR, hero_name), angle="hero",
                         res=1024, transparent=False, pad=2.5,
                         bg=(0.18, 0.22, 0.30, 1.0))
            preview_files.append(os.path.join(PREVIEW_DIR, hero_name))

        all_stats[asset_name] = {
            "stage": stage, "verts": rv, "faces": rf, "size": ri["size"],
            "empties": empty_names, "fbx": fbx_path,
            "fbx_bytes": os.path.getsize(fbx_path),
        }
        built_roots.append((asset_name, stage, builder))

    # Stage lineup board
    print("\n======== KIT BOARD ========")
    hard_clear()
    for k in COLORS:
        get_or_make(k)
    lineup = []
    # Place stages along X spaced by approx size
    x = -6.5
    for asset_name, stage, builder in built_roots:
        o = builder(asset_name)
        bi = bounds_info(o)
        half = bi["size"][0] * 0.5 + 0.35
        x += half
        o.location = (x, 0, 0)
        x += half
        lineup.append(o)
    render_angle(lineup, os.path.join(PREVIEW_DIR, "kit_board.png"), angle="hero",
                 res=1400, transparent=False, pad=2.2,
                 bg=(0.16, 0.20, 0.28, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "kit_board.png"))

    # Shared blend — all stages laid out
    print("\n======== BLEND ========")
    hard_clear()
    for k in COLORS:
        get_or_make(k)
    x = -6.5
    for asset_name, stage, builder in built_roots:
        o = builder(asset_name)
        bi = bounds_info(o)
        half = bi["size"][0] * 0.5 + 0.4
        x += half
        o.location = (x, 0, 0)
        o.rotation_euler = (math.radians(2), math.radians(-3), math.radians(8))
        x += half
    setup_lights()
    setup_world()
    blend_path = os.path.join(BLEND_DIR, "PhantomJackPlay.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved {blend_path}")

    # Live feed = fleet hero
    hero = os.path.join(PREVIEW_DIR, "hero.png")
    if os.path.isfile(hero) and os.path.isdir(os.path.dirname(LIVE_FEED)):
        shutil.copy2(hero, LIVE_FEED)
        print(f"LIVE FEED -> {LIVE_FEED}")

    # Validate all FBX
    print("\n=== VALIDATION ===")
    all_ok = True
    val_lines = []
    for asset_name, stage, builder in built_roots:
        fbx = os.path.join(EXPORT_DIR, f"{asset_name}.fbx")
        ok, meshes, empties, sz = validate_fbx(fbx)
        all_ok = all_ok and ok
        # refresh empties from stats (validate clears)
        val_lines.append(f"{asset_name}: ok={ok} bytes={sz} meshes={meshes} empties={empties}")

    stats_path = os.path.join(WORK, "scripts", "_last_stats.txt")
    with open(stats_path, "w") as f:
        for name, st in all_stats.items():
            f.write(f"[{name}] stage={st['stage']} verts={st['verts']} faces={st['faces']} "
                    f"size={st['size']} empties={st['empties']} fbx_bytes={st['fbx_bytes']}\n")
        f.write(f"all_ok={all_ok}\n")
        for line in val_lines:
            f.write(line + "\n")
    print(f"Wrote {stats_path}")
    print("ALL_OK=", all_ok)
    print("STAGES_SHIPPED=", [n for n, _, _ in built_roots])
    for p in sorted(set(preview_files)):
        print("PREVIEW", p, os.path.getsize(p) if os.path.isfile(p) else "MISSING")


if __name__ == "__main__":
    main()
