# Jack Cararrow Play Models — **SHIP + RAFT** PLAY avatars (Unity-ready)

Kiddy / Smash-lite assets for **Jack Cararrow** in Raymond's Ponex (kiddy Pong).  
Readable at Pong scale, solid Principled BSDF colors (no PBR maps), modest poly.

> **CRITICAL — dual-form lock (read this first)**
>
> | Role | Asset | This drop? |
> |------|-------|------------|
> | **PLAY / match avatar** | **`Jack_Ship`** + **`Jack_Raft`** | **YES — this package** |
> | **Person-form** | 2D fullbody `ponex-ui/fullbody/Fullbody_JackCarrow.png` only | **NO 3D person mesh** |
>
> Play models = **SHIP** and **RAFT** (not the person). Lifeline (ship kit) = **the ship**.  
> Person-form stays **2D fullbody art** — **do NOT** expect / build Jack person 3D.  
> **Phantom Jack Cararrow** (ghost raft→fleet path) is a **separate package**:  
> `/workspace/ponex-playmodels/phantom-jack/` — **not** included here.

Built with **Blender 4.3.2** on the shared box.

---

## Asset list

| Asset | File(s) | Role | Approx scale (Unity 1u = 1m) | Poly (verts / faces) |
|-------|---------|------|------------------------------|----------------------|
| **Jack_Ship** | `export/Jack_Ship.fbx` (+ `.obj`/`.mtl`) | **PLAY** caravel / lifeline paddle | ~1.92 m length (X); origin waterline center | ~839 / 573 |
| **Jack_Raft** | `export/Jack_Raft.fbx` (+ `.obj`/`.mtl`) | **PLAY** simple wooden raft (living kit) | ~1.20 m length (X); origin waterline center | ~400 / 260 |

Source scene: `blend/JackPlay.blend` (**Ship + Raft** — no person mesh).

Person-form reference (art only, not meshed): `/workspace/ponex-ui/fullbody/Fullbody_JackCarrow.png`.

### Hierarchy / sockets — Jack_Ship

| Empty | Where | Use |
|-------|-------|-----|
| **`Cannon_01`** | Mid port (+Y) | **Primary start cannon** mount |
| **`Cannon_02`** | Mid starboard (−Y) | Upgrade mount |
| **`Cannon_03`** | Aft port (+Y) | Upgrade mount |
| **`AnchorPoint`** | Near bow / deck bollard | Anchor-throw → chest pull / upgrade |

### Hierarchy / sockets — Jack_Raft

| Empty | Where | Use |
|-------|-------|-----|
| **`AnchorPoint`** | Near bow bollard | Optional anchor / interact mount |

Hull (ship) has simple dark **port rings + stub cylinders** at cannon mounts for silhouette only — **not** detailed barrels. Prefer parenting future meshes to the Empties.

---

## Previews (all under `preview/`)

| File | What |
|------|------|
| **`hero.png`** | Ship 3/4 hero (opaque sky) — primary ship hero |
| **`raft_hero.png`** | Raft 3/4 hero (opaque sky) |
| **`kit_board.png`** | Ship + Raft lineup board |
| `ship.png` / `ship_{front,34,side,back}.png` | Ship multi-angle |
| `ship_turntable.gif` / `.mp4` | Ship turntable |
| `raft.png` / `raft_{front,34,side,back}.png` | Raft multi-angle |
| `raft_turntable.gif` / `.mp4` | Raft turntable |

Live Manager feed may point at `kit_board.png` or ship `hero.png` after rebuilds.

---

## Material colors (Principled BSDF base color)

Warm living Jack pirate palette (both Ship + Raft):

| Slot | RGB (0–1) | Hex | Roughness | Used on |
|------|-----------|-----|-----------|---------|
| HullWood | 0.55, 0.28, 0.12 | `#8C471F` | 0.70 | Hull / raft planks |
| HullDark | 0.28, 0.15, 0.08 | `#472614` | 0.75 | Keel, crates, runners |
| DeckWood | 0.68, 0.48, 0.28 | `#AD7A47` | 0.65 | Deck / raft planks |
| CabinWood | 0.45, 0.26, 0.13 | `#734221` | 0.70 | Cabin / log pontoons |
| SailCream | 0.96, 0.92, 0.82 | `#F5EBD1` | 0.55 | Ship sail |
| StripeCream | 0.94, 0.88, 0.72 | `#F0E0B8` | 0.55 | Stripe / scrap sail |
| StripeOrange | 0.95, 0.48, 0.12 | `#F27A1F` | 0.50 | Soft orange accent |
| AccentBlack | 0.08, 0.07, 0.07 | `#141212` | 0.80 | Ports, bollard, flag |
| RopeTan | 0.62, 0.50, 0.32 | `#9E8052` | 0.75 | Yard / lashing |
| MastWood | 0.40, 0.24, 0.12 | `#663D1F` | 0.65 | Mast / posts |
| FlagBlack | 0.10, 0.09, 0.09 | `#1A1717` | 0.70 | Tiny flag |

No textures in this drop. Remap to Unity URP Lit / toon with the same hexes.

---

## Origins & orientation

- Origin at **waterline / geometric center** (Z ≈ 0 waterline; length centered on X).
- Length along local **+X** (bow +X, stern −X). Beam along **Y**. Up = **Z**.
- FBX: **Forward −Z, Up Y**, unit scale, transforms baked — Unity-friendly.

---

## Unity import notes (for Engineer)

1. Copy `export/Jack_Ship.fbx` and `export/Jack_Raft.fbx` into Unity (e.g. `Assets/Art/Ponex/PlayModels/Jack/`).
2. **Scale Factor = 1**. Verify longest axis ~1.9 m (ship) / ~1.2 m (raft).
3. Prefab tips:
   - These are **PLAY / match avatars** — not person bodies.
   - Keep Ship Empties `Cannon_01`–`03` + `AnchorPoint`; Raft `AnchorPoint`.
   - Do **not** expect Phantom Jack raft/fleet here — see `phantom-jack/`.
   - Do **not** expect a Jack person 3D mesh.
4. Colliders: not included. Suggest box/capsule along hull/deck.
5. Optional OBJ+MTL siblings in `export/`; prefer FBX for Unity.

### Kit reminder (Standard Jack — bible-locked)

| Field | Lock |
|-------|------|
| Play models | **SHIP** + **RAFT** (living kit) |
| Movement (ship kit) | **L/R only** |
| Lifeline | **The ship** |
| Bump | Fire **cannons** (cooldown) — start **1 cannon** |
| Interact | Throw **ANCHOR** → grab chest → upgrades |
| Out of scope | **Phantom Jack** raft→fleet (separate package) |

---

## Style / usage (art notes only)

- **Ship:** compact caravel/sloop — warm brown + cream sail + orange/cream stripes + black accents.
- **Raft:** simple kiddy wooden raft — planks, log pontoons, rope lash, scrap sail, tiny flag; readable Pong silhouette ~1.2 m.
- Keep polycounts modest; game-ready chunky shapes.

---

## Rebuild (on the shared box)

```bash
# Ship (existing)
/usr/bin/blender -b -P /workspace/ponex-playmodels/jack/scripts/build_jack_ship.py

# Raft (adds Jack_Raft.fbx + previews; refreshes JackPlay.blend with Ship+Raft)
/usr/bin/blender -b -P /workspace/ponex-playmodels/jack/scripts/build_jack_raft.py
```

Do **not** implement Unity/C# gameplay in this drop. Parent handles Windows Finals staging — do **not** copy into Windows Finals from this box.

---

## Absolute paths

- Work root: `/workspace/ponex-playmodels/jack/`
- Blend: `/workspace/ponex-playmodels/jack/blend/JackPlay.blend`
- Export ship: `/workspace/ponex-playmodels/jack/export/Jack_Ship.fbx`
- Export raft: `/workspace/ponex-playmodels/jack/export/Jack_Raft.fbx`
- Hero (ship): `/workspace/ponex-playmodels/jack/preview/hero.png`
- Hero (raft): `/workspace/ponex-playmodels/jack/preview/raft_hero.png`
- Kit board: `/workspace/ponex-playmodels/jack/preview/kit_board.png`
- Scripts: `scripts/build_jack_ship.py`, `scripts/build_jack_raft.py`
- Person-form art (2D only): `/workspace/ponex-ui/fullbody/Fullbody_JackCarrow.png`
- Phantom pack (separate): `/workspace/ponex-playmodels/phantom-jack/`
