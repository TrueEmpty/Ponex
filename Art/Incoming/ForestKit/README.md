# Ponex Forest Kit (v1) — Unity-ready art drop

Kiddy / cartoony forest-grassy stage kit for **Raymond's Ponex** (kiddy Pong).  
Bold silhouettes, soft shapes, Smash-bros-lite hazard vibe — **not** hyper-realistic.

Built with **Blender 4.3.2** on the shared box. Solid-color Principled BSDF materials (no PBR maps).

---

## Asset list

| Asset | File(s) | Role | Approx scale (Unity 1u = 1m) | Poly (verts / faces) |
|-------|---------|------|------------------------------|----------------------|
| **Tree** | `export/Tree.fbx` (+ `.obj`) | Field-populatable hazard prop | ~4.2 m tall | 234 / 414 |
| **Tree_B** | `export/Tree_B.fbx` | Shorter / cooler-green variant | ~3.4 m tall | 234 / 414 |
| **Apple** | `export/Apple.fbx` (+ `.obj`) | Heal pickup / projectile-readable | ~0.45 m diameter | 206 / 222 |
| **GrassPad** | `export/GrassPad.fbx` (+ `.obj`) | 2×2 m grassy ground tile | 2 × 2 m, slight mound | 162 / 160 |
| **Stump** | `export/Stump.fbx` (+ `.obj`) | Short matching prop | ~0.7 m tall, ~0.76 m wide | 24 / 14 |
| **Bush** | `export/Bush.fbx` | Optional filler foliage | ~0.7 m tall cluster | 126 / 240 |

Source scene: `blend/ForestKit.blend` (all objects laid out together).  
Previews: `preview/*.png`.

### Hierarchy / sockets
- **Tree** and **Tree_B** each include a child Empty named **`AppleSpawn`** near the canopy (socket for later hazard / apple-drop logic).  
  Engineer: find transform `AppleSpawn` under the tree prefab — do not rely on world position.

---

## Material colors (Principled BSDF base color)

| Slot | RGB (0–1) | Hex | Roughness |
|------|-----------|-----|-----------|
| Trunk / stump side | 0.45, 0.28, 0.14 | `#734720` | 0.75 |
| Canopy (Tree) | 0.22, 0.62, 0.28 | `#389E47` | 0.55 |
| Canopy (Tree_B) | 0.18, 0.55, 0.35 | `#2E8C59` | 0.55 |
| Apple body | 0.90, 0.18, 0.15 | `#E62E26` | 0.40 |
| Apple leaf | 0.30, 0.70, 0.25 | `#4DB33F` | 0.50 |
| Stem | 0.35, 0.22, 0.10 | `#59381A` | 0.80 |
| Grass pad | 0.35, 0.72, 0.28 | `#59B847` | 0.85 |
| Stump cut top | 0.55, 0.40, 0.22 | `#8C6638` | 0.70 |
| Bush | 0.25, 0.58, 0.30 | `#40944D` | 0.55 |

Materials are embedded as FBX material slots (solid colors). Feel free to replace with Unity URP Lit / toon shaders using the same base colors.

---

## Origins & orientation

- **Tree / Tree_B / Stump / GrassPad / Bush**: origin at **ground center** (bottom of mesh, XY centered). Place at terrain contact point.
- **Apple**: origin at **geometric center**.
- FBX export: **Forward −Z, Up Y**, unit scale applied, transforms baked — Unity-friendly Blender defaults.

---

## Unity import notes (for Engineer)

1. Copy `export/*.fbx` into the Unity project (e.g. `Assets/Art/Ponex/ForestKit/`).
2. Model import:
   - **Scale Factor = 1** (Blender meters → Unity meters).
   - Forward/Up already match Unity (−Z / Y); leave Convert Units on if your pipeline expects it, but verify a Tree stands ~4.2 m in Scene view.
3. Materials: FBX will create stub materials; remap to URP/Lit or a cartoony shader using the hex colors above. No textures in v1.
4. Prefab tips:
   - **Tree / Tree_B**: toggleable **hazards** later (art only in this drop — no gameplay). Keep `AppleSpawn` Empty on the prefab.
   - **Apple**: heal on hit/grab (gameplay later). Slightly oversized for Pong readability.
   - **GrassPad**: tile / stage floor piece; can be duplicated in a grid.
   - **Stump / Bush**: dressing props matching the tree style.
5. Colliders: not included. Engineer should add simple capsule (tree trunk), sphere (apple), box (pad/stump) as needed.
6. Optional: OBJ siblings are available for tools that prefer OBJ; prefer FBX for Unity.

---

## Style / usage (art notes only)

- Cohesive **kiddy forest stage** — saturated friendly greens, warm brown trunks, bright red apples.
- Trees are meant to be **field-populatable** and later wired as **toggleable hazards**.
- Apples are **readable at Pong scale** and intended to **heal on hit/grab** (logic out of scope for this drop).
- Keep polycounts modest; these are game-ready chunky shapes, not film assets.

---

## Rebuild (on the shared box)

```bash
/usr/bin/blender -b -P /workspace/ponex-forest-kit/scripts/build_forest_kit_v2.py
```

Do **not** implement Unity/C# gameplay in this kit. Character dual-form art is backlog.

---

## Absolute paths

- Work root: `/workspace/ponex-forest-kit/`
- Blend: `/workspace/ponex-forest-kit/blend/ForestKit.blend`
- Exports: `/workspace/ponex-forest-kit/export/`
- Previews: `/workspace/ponex-forest-kit/preview/`
- Scripts: `/workspace/ponex-forest-kit/scripts/`
