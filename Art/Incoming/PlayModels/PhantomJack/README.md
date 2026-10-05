# Phantom Jack Cararrow — Raft→Fleet PLAY pack (Unity-ready)

Kiddy / Smash-lite **ghost/undead** alt playable for **Phantom Jack Cararrow**.  
Story identity remains **Jack Cararrow**. Separate from living `jack/` (Ship + Raft) and from the separate playable **Fleet Commander**.

> **CRITICAL — dual-form + naming locks**
>
> | Role | Asset | This drop? |
> |------|-------|------------|
> | **PLAY / match avatar** | Stage meshes `Phantom_Raft` … `Phantom_Fleet` | **YES** |
> | **Person-form / HERO** | 2D fullbody only (later) | **NO 3D person FBX** |
>
> Playable name locked: **Phantom Jack Cararrow**.  
> Stage 6 form label **“Fleet commander”** ≠ playable **Fleet Commander** (separate kit) — **no merge / no rename**.

Built with **Blender 4.3.2** on the shared box.

Ghost vibe: desaturated blue-gray / blackish wood + faint cyan / pale glow accents (subtle emission). Distinct from warm living Jack.

---

## Stage table (bible)

| Stage | Asset | Form | Abilities (kit) | Empties (this drop) |
|------:|-------|------|-----------------|---------------------|
| 1 | **`Phantom_Raft`** | Raft | Move only | `AnchorPoint` |
| 2 | **`Phantom_LittleBoat`** | Little boat | Faster; no attack | `AnchorPoint` |
| 3 | **`Phantom_Sailboat`** | Sailboat / ship | 1 cannon | `Cannon_01`, `AnchorPoint` |
| 4 | **`Phantom_Warship`** | Warship | Triple cannons | `Cannon_01`–`03`, `AnchorPoint` |
| 5 | **`Phantom_Carrier`** | Carrier | Cannons + airplanes | `HangarPoint`, `Plane_01`–`03`, `Cannon_01`–`03`, `AnchorPoint` |
| 6 | **`Phantom_Fleet`** | Fleet commander form | Carrier planes + **sentry** | `SentryPoint`, `HangarPoint`, `Plane_01`–`04`, `Cannon_01`–`03`, `AnchorPoint` |

Movement kit lock (gameplay, not meshed here): **all-around board** (like Nari), not L/R only.

---

## Asset list / poly

*(Filled after build — see `scripts/_last_stats.txt`.)*

| Asset | File | Approx scale (X length) | Poly (verts / faces) |
|-------|------|-------------------------|----------------------|
| Phantom_Raft | `export/Phantom_Raft.fbx` | ~1.06 m | 228 / 150 |
| Phantom_LittleBoat | `export/Phantom_LittleBoat.fbx` | ~1.47 m | 101 / 83 |
| Phantom_Sailboat | `export/Phantom_Sailboat.fbx` | ~1.65 m | 233 / 181 |
| Phantom_Warship | `export/Phantom_Warship.fbx` | ~1.99 m | 373 / 263 |
| Phantom_Carrier | `export/Phantom_Carrier.fbx` | ~2.27 m | 233 / 173 |
| Phantom_Fleet | `export/Phantom_Fleet.fbx` | ~2.50 m | 373 / 263 |

Source scene: `blend/PhantomJackPlay.blend` (all stages laid out — **no person mesh**).  
Full empty lists + validation: `scripts/_last_stats.txt`.

---

## Previews (`preview/`)

| File | What |
|------|------|
| **`hero.png`** | Stage 6 Fleet commander 3/4 hero (opaque) |
| **`raft_hero.png`** | Stage 1 Raft hero |
| **`kit_board.png` / `kit_board_front.png`** | All-stage lineup board |
| `{raft,littleboat,sailboat,warship,carrier,fleet}_{front,34,side,back}.png` | Multi-angle |
| `{raft,sailboat,fleet}_turntable.gif` / `.mp4` | Key-asset turntables |

---

## Material colors (Principled BSDF)

| Slot | RGB (0–1) | Hex | Notes |
|------|-----------|-----|-------|
| GhostWood | 0.28, 0.32, 0.38 | `#47515F` | Desat blue-gray wood |
| GhostDark | 0.10, 0.11, 0.14 | `#1A1C24` | Blackish |
| GhostDeck | 0.36, 0.40, 0.46 | `#5C6675` | Deck |
| GhostHull | 0.22, 0.26, 0.32 | `#384252` | Hull |
| GhostSail | 0.55, 0.62, 0.70 | `#8C9EB2` | Pale sail |
| GhostMetal | 0.18, 0.20, 0.24 | `#2E333D` | Ports / metal |
| CyanGlow | 0.35, 0.85, 0.95 | `#59D9F2` | Accent + subtle emission |
| PaleGlow | 0.70, 0.88, 0.95 | `#B2E0F2` | Pale glow tips |
| Blackish | 0.06, 0.07, 0.09 | `#0F1217` | Deep accent |
| RopeGhost | 0.40, 0.42, 0.38 | `#666B61` | Muted rope |

No PBR maps. Remap to Unity URP Lit / toon; keep emission low on glow slots.

---

## Origins & orientation

- Origin at **waterline / geometric center** (Z ≈ 0).
- Length **+X** (bow +X). Beam **Y**. Up **Z**.
- FBX: Forward **−Z**, Up **Y**, `FBX_SCALE_ALL`, bake space transform.

---

## Unity import notes

1. Copy all `export/Phantom_*.fbx` into e.g. `Assets/Art/Ponex/PlayModels/PhantomJack/`.
2. Scale Factor = 1. Prefer parenting future plane/cannon/sentry meshes to Empties.
3. Do **not** expect a 3D person / hero mesh in this package.
4. Do **not** merge with living `jack/` or with playable **Fleet Commander**.
5. Colliders not included — suggest box/capsule per stage silhouette.

---

## Rebuild

```bash
/usr/bin/blender -b -P /workspace/ponex-playmodels/phantom-jack/scripts/build_phantom_jack.py
```

Do **not** copy into Windows Finals from this box (parent lands). No Unity/C# in this drop.

---

## Absolute paths

- Root: `/workspace/ponex-playmodels/phantom-jack/`
- Blend: `/workspace/ponex-playmodels/phantom-jack/blend/PhantomJackPlay.blend`
- Export: `/workspace/ponex-playmodels/phantom-jack/export/Phantom_*.fbx`
- Script: `/workspace/ponex-playmodels/phantom-jack/scripts/build_phantom_jack.py`
- Living Jack (separate): `/workspace/ponex-playmodels/jack/`
