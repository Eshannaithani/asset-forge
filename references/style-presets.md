# Style presets

A preset fixes three things: how concept images are prompted, how the mesh is finished (texture mode, budgets), and what extra verification the look needs. Projects name their preset in `Assets/Generated/_config/project.json`; the brief can override it.

## Rules for every concept prompt

Image-to-3D wants a product shot, not an illustration. Whatever the preset, the prompt ends with:

    centered, whole subject in frame, plain neutral grey background, soft even lighting, no ground shadow, no text, no watermark, no extra objects

Characters: "A-pose, arms out at 45 degrees, feet slightly apart, neutral expression, facing the camera". Props: "three-quarter view" for candidates, then "front view, orthographic" for the reference set. Turn views with EditImageWithPrompt: "same subject, same pose, same lighting and style, seen from the back" (then left, then right). Reject a view that changed the design; regenerate that view only.

## pixel-3d

Template for retro 3D pixel-art looks: the world renders at a low resolution and is quantised to a fixed palette, so fine detail disappears. Copy this block into your project presets file, give it your project's name, and fill the placeholders; the numbers decide what survives on screen.

- World render height: <px, for example 320>. At gameplay distance one screen pixel is about <cm>, so nothing thinner than about 2 px in metres survives. Put that minimum feature size in the concept prompt.
- Geometry: big simple forms, slightly exaggerated proportions, clean silhouette. Low end of the mobile triangle budget.
- Texture: flat albedo 256 to 512 px, large flat colour regions from the palette, no gradients, noise, normal or metallic maps. Always run TextureMesh with the concept image as reference and the prompt "flat hand-painted colours, no shading, no texture noise, limited palette".
- Mesh model: `model3d-tripo-p2-multiview`. Retopology to the budget with texture baking on.
- Concept scaffold: `<subject>, <art direction>, low-poly stylized 3D game asset, flat colours, limited palette of <your palette's colour families>, chunky readable shapes, clean silhouette, <pose or view>, ` + the common suffix. Model `stylized-3d`.
- Palette (sRGB hex): <list the game's palette here so the texture prompt and the visual check use the same colours>.
- Verify extra: the neutral review sheet does not show how the asset reads through the palette. Add the project's own capture: open its look-test scene (save the current scene first), instantiate the prefab where gameplay places it, run the project's capture menu through `Unity_ManageMenuItem`, read the console for the PNG path, look at it, then delete the instance and do not save the scene.
- Existing example to match: <path to a finished asset in the project whose proportions and material naming new assets should follow>.

## lowpoly-flat

Flat-shaded low-poly (think Crossy Road, Polytopia). Geometry does the work; textures are almost solid colours.

- Concept scaffold: `<subject>, <art direction>, low poly 3D game asset, flat shading, faceted surfaces, solid colours, ` + suffix. Model `stylized-3d`.
- Finish: `model3d-tripo-p2-multiview`, retopology to budget, TextureMesh with "flat solid colours per part, no gradients" at 256 to 512 px. `texture_mode: flat`.
- Verify extra: none beyond the neutral sheet.

## stylized-pbr

Hand-painted PBR with readable materials (Fortnite, Overwatch, Sea of Thieves).

- Concept scaffold: `<subject>, <art direction>, stylized 3D game asset, hand-painted PBR materials, exaggerated proportions, clean edges, ` + suffix. Model `stylized-3d`.
- Finish: `model3d-tripo-p2-multiview`, keep the generated PBR set, texture 1024 mobile or 2048 PC. Re-texture only on failure. `texture_mode: pbr`.
- Verify extra: check the roughness reads (nothing glass-shiny) in the review sheet's front view.

## realistic

Photoreal PBR for realistic or grounded games.

- Concept scaffold: `<subject>, <art direction>, photorealistic product photograph, studio lighting, high detail, ` + suffix. Model `gemini-3.1-flash` or `gpt-image-1-5`.
- Finish: `model3d-tripo-p2-multiview` for hard-surface, `model3d-hunyuan-3d-pro-3-1-multiview` for organic. Texture 2048. `texture_mode: pbr`.
- Verify extra: check for baked-in lighting in the albedo (dark shadow patches that move with nothing).

## Project config and project presets

`Assets/Generated/_config/project.json`, created on the first run:

    {
      "style": "lowpoly-flat",
      "platform": "mobile",
      "outputRoot": "Assets/Generated",
      "pipelineShader": "Universal Render Pipeline/Lit",
      "notes": "one line on the game's look and what new assets should match"
    }

`Assets/Generated/_config/presets.md` is optional and holds project-specific presets in the same format as this file. A project preset with the same name as a skill preset overrides it. Anything tied to one game, such as scene paths, capture menus, palettes and reference assets, belongs there and not in this shared file.
