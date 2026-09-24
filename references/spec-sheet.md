# Spec sheet: rundown template, defaults, spec.json

## Rundown template

Print this block filled in, then wait for approval. Keep it to these lines; the user should be able to scan and edit it in seconds.

    **Asset:** <name> (`<slug>`), <class>
    **Style:** <preset> · <art direction from the brief>
    **Size:** <height> m tall, pivot <rule>, faces +Z
    **Budget:** ≤ <N> triangles, ≤ <M> materials, textures ≤ <res> px, <flat albedo | PBR>
    **Rig:** none | biped (Humanoid avatar)
    **Animations:** <Clip>: <text prompt "..." | video: <path or "please provide a 3-10 s clip"> | rig preset>, ...
    **Concept:** 2 candidates, you pick, then 4 views · or: your reference <path>
    **Output:** Assets/Generated/<slug>/v1/<slug>_v1.prefab + report.md
    **Generations:** ~<n> (<2 concept + 3 views + 1 mesh + ... + k clips>). Each blocks this session for up to several minutes with no progress updates.
    Reply "go", or edit any line.

## Defaults per asset class

Pick the class from the brief, then take the defaults. Use the low end of a range for background or many-instance assets (words like filler, crowd, background, scatter) and the high end for hero, close-up, main, player.

| Class | Examples | Height | Pivot | Tris mobile | Tris PC | Texture mobile | Rig |
|---|---|---|---|---|---|---|---|
| prop-small | bat, ball, cup, stumps, phone | real size | base-centre; grip for handheld | 300–1500 | 1k–5k | 512 | none |
| prop-large | crate, bench, cart, fridge | real size | base-centre | 800–3000 | 3k–10k | 1024 | none |
| hard-surface | vehicle, machine, turret, weapon | real size | base-centre; grip for weapons | 2000–6000 | 8k–25k | 1024–2048 | none |
| character-biped | player, NPC, mascot | adult 1.6–1.9, kid 1.1–1.4, chibi 0.9–1.2 | feet, faces +Z | 4000–12000 | 15k–40k | 1024 | biped |
| creature | dog, bird, dragon | real size | feet | 3000–10000 | 10k–30k | 1024 | none (not riggable in-editor yet, say so) |
| environment-piece | tree, rock, wall, lamp post | real size | base-centre | 300–3000 | 2k–10k | 512–1024 | none |

Texture mode comes from the preset: pixel and flat presets use flat albedo with no normal or metallic maps; PBR presets keep the full set. Materials: 1 for small props, 2 for large props and characters, up to 4 for hard-surface. More than that hurts draw calls on mobile.

Real sizes worth knowing: cricket bat 0.96 m, stumps 0.71 m, ball 0.073 m, door 2.0 m, car 1.5 m tall and 4.5 m long, chair 0.9 m, street lamp 4 m.

## Reading the brief

- Platform: iOS, Android, phone, mobile → mobile budgets. PC, console, desktop, Steam → PC budgets. Nothing said → project config, else mobile.
- Style: named preset wins; otherwise the project config preset; otherwise infer (pixel, low-poly, realistic, cartoon) and name the inferred preset in the rundown so it can be corrected.
- Animations: list each requested action as its own clip. Idle is always added for characters unless the user says no. Sport, combat and tool actions get `source: video` and a request for a clip. Generic actions get `source: text`.
- Parts: "opens", "detachable", "separate lid" → segmented model (Tripo 3.1) and note it.
- Candidates: users can say "one candidate" or "four candidates"; default 2.

## Generation estimate

    generations = concept candidates (2) + background removals (candidates + extra views) + extra views (3 for characters, 4 for props whose candidates were three-quarter views) + mesh (1) + retopo (0 or 1) + texture (0 or 1) + rig (0 or 1) + clips (k)

Background removals are cheap but still spend credits, so they count. A pixel-style prop is about 12. A rigged character with three clips is about 16. Say the number and the breakdown. When the user supplies a reference image the concept and view terms drop to one background removal.

## spec.json

Saved at `Assets/Generated/<slug>/spec.json`. The editor checker parses this with JsonUtility, so keep exactly this shape: flat fields, arrays of objects, no dictionaries.

    {
      "slug": "stumps",
      "name": "Cricket stumps set",
      "assetClass": "prop-small",
      "style": "pixel-3d",
      "artDirection": "worn wood, plastic bails, gully street",
      "platform": "mobile",
      "height_m": 0.71,
      "height_tolerance": 0.1,
      "pivot": "base-centre",
      "max_triangles": 1200,
      "max_materials": 1,
      "max_texture": 512,
      "texture_mode": "flat",
      "rig": "none",
      "animations": [
        { "name": "Idle", "source": "text", "prompt": "relaxed batting stance, small weight shifts", "path": "" },
        { "name": "Pull", "source": "video", "prompt": "", "path": "Assets/Generated/batsman/refs/pull.mp4" }
      ],
      "concept": { "candidates": 2, "views": "front,back,left,right", "userReference": "" },
      "meshModel": "model3d-tripo-p2-multiview",
      "notes": ""
    }

`pivot` is one of `base-centre`, `feet`, `centre`, `grip`. `texture_mode` is `flat` or `pbr`. `rig` is `none` or `biped`. Use empty strings, not null.
