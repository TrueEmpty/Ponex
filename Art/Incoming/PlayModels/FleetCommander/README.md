# Fleet Commander Play Models — **SHIP fleet** PLAY kit (Unity-ready)

Kiddy / Smash-lite assets for **Fleet Commander** in Raymond's Ponex (kiddy Pong).  
Readable at Pong scale, solid Principled BSDF colors (no PBR maps), modest poly.

> **CRITICAL — dual-form lock (read this first)**
>
> | Role | Asset | This drop? |
> |------|-------|------------|
> | **HERO / person-form** | 2D only — `ponex-ui/fullbody/Fullbody_FleetCommander.png` (Modeler) | **NO 3D person mesh** |
> | **PLAY** | **Mothership** + **Fast** + **Heavy** + **Shield** | **YES — this package** |
>
> Play / match avatar = **mothership** (not the person). Lifeline = the mothership.  
> Movement = **L↔R** paddle feel. Small ships spawn from mothership empties.  
> Person-form stays **2D fullbody art** — **do NOT** build Fleet Commander person 3D in this pack.  
> **No Phantom Jack / no Hyper Test / no person heroes.**

## Concept refs (Modeler — LOCKED silhouettes)

| Ship | Fullbody | Concept |
|------|----------|---------|
| Mothership | `/workspace/ponex-ui/fullbody/Fullbody_FleetMothership.png` | `/workspace/ponex-ui/fullbody/fleet/Concept_FleetMothership.png` |
| Fast | `/workspace/ponex-ui/fullbody/Fullbody_FleetFast.png` | `/workspace/ponex-ui/fullbody/fleet/Concept_FleetFast.png` |
| Heavy Hitter | `/workspace/ponex-ui/fullbody/Fullbody_FleetHeavyHitter.png` | `/workspace/ponex-ui/fullbody/fleet/Concept_FleetHeavyHitter.png` |
| Shield | `/workspace/ponex-ui/fullbody/Fullbody_FleetShield.png` | `/workspace/ponex-ui/fullbody/fleet/Concept_FleetShield.png` |
| Person HERO (2D only) | `/workspace/ponex-ui/fullbody/Fullbody_FleetCommander.png` | — |

Meshes are built to match these refs (elongated hex-hangar carrier; dart Fast; twin-gatling Heavy; dish Shield).

Built with **Blender 4.3.2** on the shared box.

---

## Asset list

| Asset | File(s) | Role | Approx scale (Unity 1u = 1m) | Poly (verts / faces) |
|-------|---------|------|------------------------------|----------------------|
| **FC_Mothership** | `export/FC_Mothership.fbx` (+ `.obj`/`.mtl`) | **PLAY** match avatar / lifeline paddle | ~2.65 × 0.97 × 1.11 m | **776 / 556** |
| **FC_Fast** | `export/FC_Fast.fbx` (+ `.obj`/`.mtl`) | Spawnable small — sleek dart | ~0.92 × 0.62 × 0.18 m | **336 / 254** |
| **FC_Heavy** | `export/FC_Heavy.fbx` (+ `.obj`/`.mtl`) | Spawnable small — heavy hitter | ~0.96 × 0.56 × 0.30 m | **522 / 370** |
| **FC_Shield** | `export/FC_Shield.fbx` (+ `.obj`/`.mtl`) | Spawnable small — shield dish | ~0.67 × 0.70 × 0.69 m | **930 / 860** |

Materials are solid FBX slots (no textures). Remap to Unity URP Lit / toon using the same hexes.

---

## Origins & orientation

- Unity import: FBX with forward **−Z**, up **Y** (Blender export bake).
- Origin ≈ geometric / flight center for balanced L↔R paddle feel.
- Bow / nose ≈ **+X** in Blender source; verify forward after Unity import.

---

## Rebuild

```bash
/usr/bin/blender -b -P /workspace/ponex-playmodels/fleet-commander/scripts/build_fleet_commander.py
```

Package root: `/workspace/ponex-playmodels/fleet-commander/`  
(scripts / blend / export / preview / README.md)
