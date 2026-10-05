"""
Jack Cararrow Play Model — SHIP scaffold only (standard Jack; NOT Phantom).

CRITICAL LOCKS:
  - PLAY / match avatar = Jack_Ship (this drop). Lifeline = the ship. Movement L↔R.
  - Person-form stays 2D fullbody Fullbody_JackCarrow.png — do NOT build Jack person 3D.
  - Standard Jack only — NOT Phantom Jack raft→fleet path.
  - Cannons later — scaffold empties only (Cannon_01 / _02 / _03). Prefer Empty parents;
    tiny dark port rings OK for silhouette; no detailed cannon barrels.
  - AnchorPoint empty near bow/deck for later anchor-throw treasure upgrades.
  - Start kit: 1 cannon (Cannon_01 is the primary mount).

Style: kiddy Smash-lite, readable at Pong scale, solid Principled BSDF (no PBR maps),
modest poly (~800–2500 verts). Compact caravel/sloop ~1.7 m length.

Blender 4.x — rebuild:
  /usr/bin/blender -b -P /workspace/ponex-playmodels/jack/scripts/build_jack_ship.py
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

# Team-neutral pirate vibe matching Jack coat (warm brown) + cream shirt
COLORS = {
    "HullWood":     (0.55, 0.28, 0.12),   # #8C471F warm brown hull
    "HullDark":     (0.28, 0.15, 0.08),   # #472614 darker hull / keel
    "DeckWood":     (0.68, 0.48, 0.28),   # #AD7A47 lighter deck
    "CabinWood":    (0.45, 0.26, 0.13),   # #734221 cabin
    "SailCream":    (0.96, 0.92, 0.82),   # #F5EBD1 cream sail
    "StripeCream":  (0.94, 0.88, 0.72),   # #F0E0B8 hull stripe
    "StripeOrange": (0.95, 0.48, 0.12),   # #F27A1F soft orange stripe accent
    "AccentBlack":  (0.08, 0.07, 0.07),   # #141212 trim / ports / mast top
    "RopeTan":      (0.62, 0.50, 0.32),   # #9E8052 yard / ropes
    "MastWood":     (0.40, 0.24, 0.12),   # #663D1F mast
    "FlagBlack":    (0.10, 0.09, 0.09),   # #1A1717 tiny stern flag
}

ROUGH = {
    "HullWood": 0.70,
    "HullDark": 0.75,
    "DeckWood": 0.65,
    "CabinWood": 0.70,
    "SailCream": 0.55,
    "StripeCream": 0.55,
    "StripeOrange": 0.50,
    "AccentBlack": 0.80,
    "RopeTan": 0.75,
    "MastWood": 0.65,
    "FlagBlack": 0.70,
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


def taper_hull_mesh(obj, bow_scale=0.35, stern_scale=0.55, mid_half=0.55):
    """Taper cube hull along +X (bow) / -X (stern) in Y and slightly in Z."""
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    xs = [v.co.x for v in bm.verts]
    x_min, x_max = min(xs), max(xs)
    x_span = max(x_max - x_min, 1e-6)
    for v in bm.verts:
        t = (v.co.x - x_min) / x_span  # 0 stern → 1 bow
        # Mid beam widest; taper both ends
        # Piecewise: stern (0..0.25) stern_scale→1, mid (0.25..0.7) ~1, bow (0.7..1) → bow_scale
        if t < 0.22:
            u = t / 0.22
            s = stern_scale + (1.0 - stern_scale) * u
        elif t > 0.72:
            u = (t - 0.72) / 0.28
            s = 1.0 + (bow_scale - 1.0) * u
        else:
            s = 1.0
        # Slight mid bulge
        mid = 1.0 - abs((t - 0.45) / 0.45) * 0.08
        s *= mid
        v.co.y *= s
        # Raise bow slightly, round bilge a bit by shrinking lower verts more
        if v.co.z < 0:
            v.co.y *= 0.85 + 0.15 * s
            v.co.z *= 0.92
        if t > 0.85:
            v.co.z += 0.04 * ((t - 0.85) / 0.15)  # slight bow lift
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


# -------------------- SHIP --------------------

def build_ship(name="Jack_Ship"):
    """
    Kiddy caravel/sloop ~1.70 m along +X (bow +X, stern −X).
    Origin at geometric / waterline center for balanced L↔R paddle feel.
    Beam along Y; up = Z. Side-view-friendly silhouette.
    """
    mats = {k: get_or_make(k) for k in COLORS.keys()}
    parts = []

    # --- Layout (meters) ---
    # Hull length ~1.55, bowsprit/prow to ~1.70 total
    # Waterline center ≈ origin after shift
    # Deck at z ≈ +0.18, keel at z ≈ -0.22

    # 1) Main hull body (warm brown) — multi-segment cubes then tapered (poly-friendly)
    # Target length ~1.85 m after bowsprit; waterline center origin.
    hull_segs = [
        ("Hull_Aft",   (-0.45, 0.0, 0.0), (0.55, 0.40, 0.36)),
        ("Hull_Mid",   ( 0.05, 0.0, 0.0), (0.55, 0.44, 0.38)),
        ("Hull_Fwd",   ( 0.50, 0.0, 0.0), (0.45, 0.36, 0.36)),
    ]
    for hn, hloc, hsc in hull_segs:
        h = add_cube(hn, hloc, hsc, mats["HullWood"])
        # light subdivide for smoother taper / poly budget
        bpy.ops.object.select_all(action='DESELECT')
        h.select_set(True)
        bpy.context.view_layer.objects.active = h
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.subdivide(number_cuts=1)
        bpy.ops.object.mode_set(mode='OBJECT')
        taper_hull_mesh(h, bow_scale=0.35, stern_scale=0.60)
        shade_smooth(h)
        parts.append(h)

    # 2) Darker lower hull / waterline band
    keel = add_cube("Hull_Keel", (0.0, 0.0, -0.15), (1.30, 0.28, 0.14), mats["HullDark"])
    bpy.ops.object.select_all(action='DESELECT')
    keel.select_set(True)
    bpy.context.view_layer.objects.active = keel
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=1)
    bpy.ops.object.mode_set(mode='OBJECT')
    taper_hull_mesh(keel, bow_scale=0.30, stern_scale=0.55)
    shade_smooth(keel)
    parts.append(keel)

    # Thin keel fin
    fin = add_cube("KeelFin", (0.05, 0.0, -0.25), (0.85, 0.04, 0.10), mats["HullDark"])
    parts.append(fin)

    # 3) Cream + orange hull stripes (readable team-neutral pirate accent)
    stripe_c = add_cube("Stripe_Cream", (0.0, 0.0, 0.07), (1.32, 0.445, 0.05), mats["StripeCream"])
    taper_hull_mesh(stripe_c, bow_scale=0.36, stern_scale=0.60)
    parts.append(stripe_c)

    stripe_o = add_cube("Stripe_Orange", (0.0, 0.0, 0.01), (1.28, 0.450, 0.045), mats["StripeOrange"])
    taper_hull_mesh(stripe_o, bow_scale=0.36, stern_scale=0.60)
    parts.append(stripe_o)

    # 4) Deck (flat top) + plank strips for readable poly / detail
    deck = add_cube("Deck", (0.0, 0.0, 0.19), (1.25, 0.34, 0.04), mats["DeckWood"])
    taper_hull_mesh(deck, bow_scale=0.42, stern_scale=0.68)
    shade_flat(deck)
    parts.append(deck)
    for i, px in enumerate((-0.40, -0.15, 0.10, 0.35)):
        plank = add_cube(f"Plank_{i}", (px, 0.0, 0.215), (0.02, 0.30, 0.01), mats["HullDark"])
        parts.append(plank)

    # Gunwale / rail lip + posts
    for side, sy in (("L", 0.175), ("R", -0.175)):
        rail = add_cube(f"Rail_{side}", (0.0, sy, 0.23), (1.10, 0.032, 0.055), mats["HullWood"])
        taper_hull_mesh(rail, bow_scale=0.48, stern_scale=0.72)
        parts.append(rail)
        for j, px in enumerate((-0.35, 0.0, 0.35)):
            post = add_cylinder(f"RailPost_{side}_{j}", (px, sy, 0.30), 0.015, 0.10,
                                mats["MastWood"], verts=8)
            parts.append(post)

    # 5) Cabin block (mid-aft)
    cabin = add_cube("Cabin", (-0.22, 0.0, 0.38), (0.42, 0.30, 0.28), mats["CabinWood"])
    shade_flat(cabin)
    parts.append(cabin)
    # Cabin roof slightly overhanging
    roof = add_cube("CabinRoof", (-0.22, 0.0, 0.54), (0.48, 0.34, 0.05), mats["HullDark"])
    parts.append(roof)
    # Tiny cabin door hint (black)
    door = add_cube("CabinDoor", (-0.22, 0.155, 0.34), (0.12, 0.02, 0.16), mats["AccentBlack"])
    parts.append(door)
    # Cabin windows (both sides)
    for side, sy in (("L", 0.155), ("R", -0.155)):
        win = add_cube(f"CabinWin_{side}", (-0.10, sy, 0.42), (0.08, 0.02, 0.08), mats["StripeCream"])
        parts.append(win)

    # Deck crates / cargo (kiddy prop clutter near cabin)
    crate1 = add_cube("Crate_1", (-0.45, 0.10, 0.30), (0.14, 0.12, 0.12), mats["HullDark"])
    parts.append(crate1)
    crate2 = add_cube("Crate_2", (-0.45, -0.10, 0.28), (0.10, 0.10, 0.10), mats["CabinWood"])
    parts.append(crate2)
    # Barrel near mid deck (cylinder)
    barrel = add_cylinder("Barrel", (0.35, 0.12, 0.30), 0.06, 0.14, mats["HullWood"], verts=12)
    parts.append(barrel)
    barrel_band = add_cylinder("BarrelBand", (0.35, 0.12, 0.30), 0.062, 0.03, mats["AccentBlack"], verts=12)
    parts.append(barrel_band)

    # 6) Mast + crow nest stub
    mast = add_cylinder("Mast", (0.15, 0.0, 0.82), 0.038, 1.20, mats["MastWood"], verts=14)
    shade_smooth(mast)
    parts.append(mast)

    # Mast top ball / truck
    truck = add_cylinder("MastTruck", (0.15, 0.0, 1.42), 0.05, 0.06, mats["AccentBlack"], verts=10)
    parts.append(truck)

    # Crow's nest (simple ring platform)
    nest = add_cylinder("CrowsNest", (0.15, 0.0, 1.10), 0.10, 0.04, mats["DeckWood"], verts=12)
    parts.append(nest)
    nest_rail = add_cylinder("NestRail", (0.15, 0.0, 1.15), 0.11, 0.03, mats["AccentBlack"], verts=12)
    # Hollow-ish: scale inner? just a thin ring look via smaller top — keep solid chunky
    parts.append(nest_rail)

    # 7) Yard (crossbeam) + sail
    yard = add_cylinder(
        "Yard", (0.15, 0.0, 1.00), 0.022, 0.90, mats["RopeTan"], verts=10,
        rotation=(math.radians(90), 0, 0)
    )
    parts.append(yard)

    # Sail — flat plane-ish cube (cream), readable kite silhouette
    sail = add_cube("Sail", (0.15, 0.0, 0.68), (0.04, 0.74, 0.66), mats["SailCream"])
    # Slight trapezoid: widen bottom via bmesh
    mesh = sail.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    for v in bm.verts:
        if v.co.z < 0.72:
            v.co.y *= 1.08
        else:
            v.co.y *= 0.88
        # gentle billow toward +X (forward of mast a bit)
        v.co.x += 0.06 * (1.0 - abs(v.co.y) / 0.45)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    shade_smooth(sail)
    parts.append(sail)

    # Simple sail patch (tattered Jack vibe) — small darker cream square
    patch = add_cube("SailPatch", (0.18, 0.12, 0.74), (0.02, 0.14, 0.12), mats["StripeCream"])
    parts.append(patch)

    # 8) Bow / prow / bowsprit
    prow = add_cone(
        "Prow", (0.78, 0.0, 0.10), 0.13, 0.30, mats["HullWood"], verts=10,
        rotation=(0, math.radians(90), 0)
    )
    shade_smooth(prow)
    parts.append(prow)

    bowsprit = add_cylinder(
        "Bowsprit", (0.90, 0.0, 0.28), 0.022, 0.32, mats["MastWood"], verts=10,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(bowsprit)

    # Tiny figurehead stub (chunky block — not detailed)
    fig = add_cube("Figurehead", (0.74, 0.0, 0.28), (0.10, 0.08, 0.12), mats["AccentBlack"])
    parts.append(fig)

    # 9) Stern / rudder / tiller hint
    stern = add_cube("SternTransom", (-0.72, 0.0, 0.12), (0.10, 0.32, 0.30), mats["HullWood"])
    parts.append(stern)

    rudder = add_cube("Rudder", (-0.82, 0.0, -0.02), (0.08, 0.04, 0.26), mats["HullDark"])
    parts.append(rudder)

    tiller = add_cylinder(
        "Tiller", (-0.64, 0.0, 0.30), 0.018, 0.26, mats["MastWood"], verts=8,
        rotation=(0, math.radians(90), 0)
    )
    parts.append(tiller)

    # Tiny black flag at stern
    flagpole = add_cylinder("Flagpole", (-0.72, 0.0, 0.52), 0.012, 0.28, mats["AccentBlack"], verts=8)
    parts.append(flagpole)
    flag = add_cube("Flag", (-0.66, 0.0, 0.58), (0.10, 0.02, 0.08), mats["FlagBlack"])
    parts.append(flag)

    # 10) Cannon port rings (silhouette only — dark recessed rings, NOT detailed barrels)
    # Port side (+Y) and starboard (−Y). Primary start cannon = mid port.
    port_specs = [
        ("PortRing_01", (0.15, 0.22, 0.05)),   # mid port — Cannon_01
        ("PortRing_02", (0.15, -0.22, 0.05)),  # mid starboard — Cannon_02
        ("PortRing_03", (-0.15, 0.22, 0.05)),  # aft port — Cannon_03
    ]
    for pname, ploc in port_specs:
        ring = add_cylinder(pname, ploc, 0.055, 0.04, mats["AccentBlack"], verts=14,
                            rotation=(math.radians(90), 0, 0))
        shade_smooth(ring)
        parts.append(ring)
        # Tiny inner stub (optional silhouette cylinder — not a detailed barrel)
        stub = add_cylinder(pname.replace("Ring", "Stub"), ploc, 0.028, 0.06, mats["HullDark"],
                            verts=10, rotation=(math.radians(90), 0, 0))
        parts.append(stub)

    # 11) Simple deck hatch + bollard near bow for anchor vibe
    hatch = add_cube("Hatch", (0.38, 0.0, 0.24), (0.16, 0.12, 0.03), mats["HullDark"])
    parts.append(hatch)
    bollard = add_cylinder("Bollard", (0.50, 0.0, 0.28), 0.03, 0.10, mats["AccentBlack"], verts=10)
    parts.append(bollard)

    # Stay ropes (bowsprit → mast, mast → stern) — thin cylinders for poly + silhouette
    stay_fwd = add_cylinder(
        "Stay_Fwd", (0.52, 0.0, 0.70), 0.010, 0.85, mats["RopeTan"], verts=6,
        rotation=(0, math.radians(55), 0)
    )
    parts.append(stay_fwd)
    stay_aft = add_cylinder(
        "Stay_Aft", (-0.25, 0.0, 0.70), 0.010, 0.80, mats["RopeTan"], verts=6,
        rotation=(0, math.radians(-50), 0)
    )
    parts.append(stay_aft)
    for side, sy in (("L", 0.12), ("R", -0.12)):
        shroud = add_cylinder(
            f"Shroud_{side}", (0.15, sy * 0.5, 0.65), 0.009, 0.70, mats["RopeTan"], verts=6,
            rotation=(math.radians(18 if side == "L" else -18), 0, 0)
        )
        parts.append(shroud)

    # Small jib sail triangle hint (forward triangle as thin cube wedge)
    jib = add_cube("JibSail", (0.55, 0.0, 0.55), (0.35, 0.02, 0.40), mats["SailCream"])
    mesh = jib.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    for v in bm.verts:
        # taper toward bowsprit tip
        if v.co.x > 0.55:
            v.co.z = 0.55 + (v.co.z - 0.55) * 0.35
            v.co.y *= 0.5
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    shade_smooth(jib)
    parts.append(jib)

    # Join all mesh parts
    ship = join_objects(parts, name)
    shade_smooth(ship)

    # Origin at waterline / geometric center (X mid, Y mid, Z≈0 waterline)
    bi = bounds_info(ship)
    # Prefer waterline Z=0 rather than full AABB center Z (mast pulls center up)
    origin = (bi["center"][0], bi["center"][1], 0.0)
    set_origin_at_world_point(ship, origin)

    bi2 = bounds_info(ship)
    print(f"SHIP post-origin size={bi2['size']} center={bi2['center']} "
          f"min={bi2['min']} max={bi2['max']}")

    # --- Empty sockets (children) — cannon mounts + anchor ---
    # Local positions after origin at waterline center
    # Mid port (+Y) primary start cannon
    make_empty("Cannon_01", ship, (0.15, 0.24, 0.05), display='SPHERE', size=0.07)
    make_empty("Cannon_02", ship, (0.15, -0.24, 0.05), display='SPHERE', size=0.07)
    make_empty("Cannon_03", ship, (-0.15, 0.24, 0.05), display='SPHERE', size=0.07)
    # Anchor throw attach near bow / deck bollard
    make_empty("AnchorPoint", ship, (0.50, 0.0, 0.30), display='PLAIN_AXES', size=0.09)

    return ship


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

    # Warm rim (pirate sunset vibe)
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


def validate_fbx(path):
    hard_clear()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [(o.name, len(o.data.vertices), len(o.data.polygons),
               [s.name for s in o.material_slots if s.material])
              for o in bpy.data.objects if o.type == 'MESH']
    empties = [o.name for o in bpy.data.objects if o.type == 'EMPTY']
    mats = [m.name for m in bpy.data.materials]
    sz = os.path.getsize(path)
    need = {"Cannon_01", "AnchorPoint"}
    have = set(empties)
    ok = sz > 2000 and len(meshes) >= 1 and need.issubset(have)
    print(f"VALIDATE {os.path.basename(path)}: bytes={sz} meshes={meshes} "
          f"empties={empties} ok={ok}")
    return ok, meshes, empties, mats, sz


def multi_angle_and_turntable(obj, prefix, pad=2.6):
    outs = []
    for a in ("front", "34", "side", "back"):
        fp = os.path.join(PREVIEW_DIR, f"{prefix}_{a}.png")
        render_angle([obj], fp, angle=a, res=900, transparent=True, pad=pad)
        outs.append(fp)
    # Legacy 3/4 alias
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


def rename_empties(ship):
    for c in ship.children:
        if c.type != 'EMPTY':
            continue
        n = c.name
        if n.startswith("Cannon_01"):
            c.name = "Cannon_01"
        elif n.startswith("Cannon_02"):
            c.name = "Cannon_02"
        elif n.startswith("Cannon_03"):
            c.name = "Cannon_03"
        elif n.startswith("AnchorPoint"):
            c.name = "AnchorPoint"


def main():
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(EXPORT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    stats = {}
    preview_files = []

    # --- SHIP export ---
    hard_clear()
    ship = build_ship("Jack_Ship")
    ship.location = (0, 0, 0)
    rename_empties(ship)

    sv, sf = mesh_stats(ship)
    si = bounds_info(ship)
    print(f"SHIP verts={sv} faces={sf} size={si['size']}")
    empty_names = [c.name for c in ship.children if c.type == 'EMPTY']
    print(f"EMPTIES: {empty_names}")

    fbx_path = os.path.join(EXPORT_DIR, "Jack_Ship.fbx")
    export_fbx(ship, fbx_path)
    export_obj(ship, os.path.join(EXPORT_DIR, "Jack_Ship.obj"))
    stats["ship"] = {"verts": sv, "faces": sf, "size": si["size"], "fbx": fbx_path,
                     "empties": empty_names}

    preview_files += multi_angle_and_turntable(ship, "ship", pad=2.7)

    # Hero = ship 3/4 opaque sky
    render_angle([ship], os.path.join(PREVIEW_DIR, "hero.png"), angle="hero",
                 res=1024, transparent=False, pad=2.6,
                 bg=(0.48, 0.60, 0.76, 1.0))
    preview_files.append(os.path.join(PREVIEW_DIR, "hero.png"))

    # --- Shared blend (ship only — no person 3D this drop) ---
    hard_clear()
    ship = build_ship("Jack_Ship")
    rename_empties(ship)
    ship.location = (0, 0, 0)
    # Gentle display tilt for blend viewport readability
    ship.rotation_euler = (math.radians(2), math.radians(-4), math.radians(12))

    blend_path = os.path.join(BLEND_DIR, "JackPlay.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved {blend_path}")

    # Copy hero to Manager live feed
    hero = os.path.join(PREVIEW_DIR, "hero.png")
    if os.path.isfile(hero) and os.path.isdir(os.path.dirname(LIVE_FEED)):
        shutil.copy2(hero, LIVE_FEED)
        print(f"LIVE FEED -> {LIVE_FEED} ({os.path.getsize(LIVE_FEED)} bytes)")

    # --- Validate FBX ---
    print("\n=== VALIDATION ===")
    ok, meshes, empties, mats, sz = validate_fbx(fbx_path)

    print("\n=== STATS ===")
    print("SHIP", stats["ship"])
    print(f"FBX bytes={sz} ok={ok} meshes={meshes} empties={empties}")
    print("ALL_OK=", ok)

    stats_path = os.path.join(WORK, "scripts", "_last_stats.txt")
    with open(stats_path, "w") as f:
        f.write(f"ship_verts={stats['ship']['verts']}\n")
        f.write(f"ship_faces={stats['ship']['faces']}\n")
        f.write(f"ship_size={stats['ship']['size']}\n")
        f.write(f"ship_fbx_bytes={sz}\n")
        f.write(f"ship_meshes={meshes}\n")
        f.write(f"ship_empties={empties}\n")
        f.write(f"mats={mats}\n")
        f.write(f"ok={ok}\n")
    print(f"Wrote {stats_path}")
    print("PREVIEW_FILES:")
    for p in sorted(set(preview_files)):
        print(" ", p, os.path.getsize(p) if os.path.isfile(p) else "MISSING")


if __name__ == "__main__":
    main()
