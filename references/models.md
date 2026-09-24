# Models available through the Unity MCP

Verified 2026-09-24 on Unity 6000.3.9f1 with `Unity_AssetGeneration_GetModels`. Re-run it if an id below errors; the list changes as Unity adds providers.

All generation goes through `Unity_AssetGeneration_GenerateAsset` with `command`, `modelId`, `savePath`, and one of `prompt`, `referenceImageInstanceId(s)` or `targetAssetPath`. Independent calls can be issued in the same response and overlap; each still blocks until done.

## Concept images

Commands: `GenerateImage`, `EditImageWithPrompt`, `RemoveImageBackground`. savePath ends in `.png`.

| modelId | Use for |
|---|---|
| `stylized-3d` | Default for props and characters in stylized presets. Renders subjects as clean 3D-looking objects, which is what image-to-3D wants |
| `gemini-3.1-flash` | Fast, follows layout instructions (view angle, pose, background). Use for the back, left and right views through EditImageWithPrompt |
| `gpt-image-1-5` | When an edit must preserve the design exactly (view turns, small fixes) |
| `flux-2-pro` | Cleanup edits such as removing a prop the model added |
| `photoroom-bg-removal` | Background removal (RemoveImageBackground) |

## Mesh

Command: `GenerateMesh`. savePath ends in `.fbx`. Treat the reference image as mandatory: text-only runs have not produced usable results here.

| modelId | Input | Use for |
|---|---|---|
| `model3d-tripo-p2` | 1 image, optional prompt | Default single-view. Clean topology, PBR, up to 50k faces |
| `model3d-tripo-p2-multiview` | up to 4 labelled images | Default whenever the concept stage produced views |
| `model3d-tripo-p2-quad`, `model3d-tripo-p2-multiview-quad` | same | Quad FBX for hand-editing in Blender, capped at 25k faces. Not for direct game use |
| `model3d-tripo3-1`, `model3d-tripo3-1-multiview` | image, optional prompt | Segmented parts: use when parts must be separate objects (door and frame, lid and box). Smart low-poly topology |
| `model3d-hunyuan-3d-pro-3-1`, `model3d-hunyuan-3d-pro-3-1-multiview` | image only | Organic and rounded shapes: creatures, food, plush, rocks. Configurable face count |
| `model3d-tripo-p1`, `model3d-tripo-p1-multiview` | image | Fallback when P2 fails; has image autofix |

## Cleanup and finish

| Stage | command | modelId | Notes |
|---|---|---|---|
| Retopology | `RetopologyMesh` | `model3d-tripo-retopo` | Tri or quad, can bake the texture onto the low-poly result. Preferred |
| Retopology | `RetopologyMesh` | `model3d-tencent-smarttopology` | Hunyuan Polygen 1.5, reduction levels. Try when Tripo retopo distorts |
| Texture | `TextureMesh` | `model3d-tripo3-texturing` | Prompt or reference image; outputs albedo, metallic, roughness, normal. Pass the concept image as reference to keep the look |
| Rig | `RigMesh` | `model3d-tripo-rigging-v1` | Biped only. Can also retarget its preset walk, run and idle clips |

`targetAssetPath` points at the mesh asset being processed. Give each output its own savePath.

## Animation

Command: `GenerateHumanoidAnimation`. savePath ends in `.anim`.

| modelId | Input | Use for |
|---|---|---|
| `uthana-text-to-motion-3` | prompt | Default for text: idle, walk, run, jump, wave, cheer, pick up |
| `unity-text-to-motion` | prompt | Unity first-party fallback when Uthana output is poor |
| `uthana-video-to-motion` | `targetAssetPath` = a VideoClip asset in the project | Sport, combat, tool use, anything with precise timing. Ask for a 3-10 s clip, one person, full body visible, static camera |

Post-process with the tool whose name starts `Unity_AssetGeneration_EditAnimati`: `MakeStationary` removes root motion for in-place gameplay clips; `TrimToBestLoop` finds the loop in idle and locomotion clips. Build the controller with the tool whose name starts `Unity_AssetGeneration_CreateAnima` (first clip becomes the default state), then add the remaining clips as states through a RunCommand using `UnityEditor.Animations.AnimatorController`.

## Passing reference images

Mesh and texture commands take Texture2D instance IDs, not paths. Get them with a RunCommand:

    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/<slug>/refs/front.png");
    result.Log("{0}", tex.GetInstanceID());

For multi-view pass `referenceImageInstanceIds` and `referenceImageLabels` in the same order with labels `front`, `back`, `left`, `right`.

## Known unknowns

Confirm during the first real run, then edit this file so the next run does not rediscover them.

- How `RetopologyMesh` takes the target face count. The tool schema has no dedicated field; try the `prompt` field ("target 1500 triangles") and read the result's face count with the checker.
- Exact output of `GenerateMesh`. Expect an FBX with a `.fbm` texture folder: an earlier run produced `<name>.fbx` plus `<name>.fbm/Color_*.png` and `NormalGL_*.png`. Confirm accepted `meshFormat` values.
- Whether the rigged FBX from `RigMesh` imports with Rig set to Humanoid automatically or needs the importer switched.
- Credit cost per model. The editor's generator window shows it; note observed costs in the report so rundown estimates calibrate.
