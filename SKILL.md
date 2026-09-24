---
name: asset-forge
description: >-
  Generate game-ready 3D assets (props, hard-surface objects, characters, creatures, environment pieces) and their animation clips for Unity with AI, verify them inside the open Unity editor, deliver a prefab plus report card, and iterate from feedback. Use this whenever the user asks to make, generate, model, rig, retexture or animate any 3D asset for a Unity project, mentions a mesh, prop, character, model or animation clip they need, wants a previously generated asset fixed or improved, or types /asset-forge. Also use it for "make me a <thing> for the game" in a 3D Unity project even when the words 3D or generate are absent. Not for 2D sprites, UI, standalone textures or skyboxes.
---

# 3D asset generator for Unity

Turns a one-line brief into a verified Unity prefab: spec, concept images, mesh, cleanup, rig, animation, in-editor checks, delivery, improvement passes. Everything runs through the Unity MCP against the editor that is already open, using the Unity AI models in `references/models.md`. No API keys, no external services.

Two ideas drive the design:

- **Credits and minutes are the scarce resource, not tokens.** Every generation blocks the session for tens of seconds to minutes and spends Unity AI credits. So the skill front-loads cheap thinking (the rundown), gates the two decisions a human makes in seconds (approve the spec, pick a concept), and never regenerates when a cheaper stage fixes the complaint.
- **Verify by measuring and rendering, not by trusting the generator.** A mesh that "succeeded" can still be three times too big, pivoted at its centre, pink, or unrigged. The checker script measures it, the multi-angle sheet shows it, and the report names the stage to rerun.

## Modes

Read the brief and pick one:

- **Full run** (default): all six stages.
- **Plan only**: the brief contains `--plan` or "plan only", or the user asks what it would cost or do. Stop after Stage 1, print the exact call list (tool, command, modelId, savePath) and the generation estimate. Make no generation calls. Run only preflight steps 1 and 4 (which project, project config); skip installs and the interrupted-generation check. The evals use this mode.
- **Improve**: the user gives feedback on an asset that exists under `Assets/Generated/<slug>/`. Jump to Stage 6.
- **Own reference**: the user supplies an image path. Skip concept generation, still remove the background.

## Stage 0: Preflight (every run, under a minute)

1. One editor only. Run `Application.dataPath` through `Unity_RunCommand` and confirm it is the project the user means. Two open editors route MCP calls to the wrong project.
2. Models available: `Unity_AssetGeneration_GetModels`. An empty list means the project is not linked to Unity Cloud. Tell the user to link it (Edit > Project Settings > Services) and stop.
3. Interrupted generations: `Unity_AssetGeneration_ManageInterrupted` with `List`. Resume or discard before starting new ones so paid work is not thrown away.
4. Project config: read `<project>/Assets/Generated/_config/project.json` if present (style preset, platform, overrides) and `_config/presets.md` if present (project-specific presets that override the skill's by name; examples of both in `references/style-presets.md`). On the first run create project.json with defaults and say so.
5. Checker installed: `Assets/Editor/AssetGen/AssetGenCheck.cs` must exist and match `assets/AssetGenCheck.cs` from this skill. Install with `Unity_CreateScript`, then read the console (`Unity_ReadConsole`) for compile errors before calling anything in it.

## Stage 1: Brief to rundown (gate 1)

Fill the spec sheet using `references/spec-sheet.md`. Read that file for the template and the per-class defaults instead of guessing sizes and budgets; the defaults exist so assets stay consistent across a project.

Show the rundown as one message and ask for approval. It must include the generation estimate and one sentence saying generation blocks this session for several minutes with no progress updates. That sentence is the consent the generation tool requires, so one approval covers both.

Write the approved spec to `Assets/Generated/<slug>/spec.json` (schema in spec-sheet.md). The checker reads it, so the spec is the contract for the whole run.

In plan-only mode, print the call list after the rundown and stop.

## Stage 2: Concept (gate 2)

Skip when the user supplied a reference image. Otherwise:

1. Build the prompt from the preset scaffold in `references/style-presets.md`. Generate the front view with `GenerateImage`, two candidates only; more costs more and rarely helps. Characters: A-pose, facing camera, full body, neutral grey background, no ground shadow. Props: one three-quarter view per candidate.
2. `RemoveImageBackground` on each candidate. The mesh models need a cut-out subject; a background becomes geometry.
3. Stitch candidates with `scripts/contact_sheet.py` and show one image. Ask the user to pick or say what to change. One regeneration round is normal. A third means the brief or the preset needs editing, so say that instead of rolling again.
4. For the pick, make the reference set with `EditImageWithPrompt` ("same subject, same pose, lighting and style, seen from the back"): back, left and right for characters, whose candidates are already front views; for props, first turn the three-quarter pick into an orthographic front, then back, left and right. Remove backgrounds and save the four views under `Assets/Generated/<slug>/refs/`.

## Stage 3: Generate and finish

Every generation call gets a unique `savePath` under `Assets/Generated/<slug>/v<N>/`. Default filenames collide and overwrite when calls run in parallel. Name intermediates by stage so the chain is readable later: `<slug>_v<N>_raw.fbx`, `_retopo.fbx`, `_tex.fbx`, and the final rigged or finished mesh `<slug>_v<N>.fbx`; clips `<slug>_<Clip>_v<N>.anim`.

1. **Mesh.** `GenerateMesh` with the multi-view model when four views exist, single-view otherwise. Model rules and how to pass reference images are in `references/models.md`.
2. **Retopology.** Only when the face count is above the spec budget. Every pass loses detail, so skip it when the mesh already fits.
3. **Texture.** Only when the preset says so (pixel and flat presets re-texture to a low-res flat albedo) or the generated texture is visibly wrong. `TextureMesh` with the concept image as reference keeps the look consistent.
4. **Rig.** Characters only: `RigMesh` with the biped rigging model. Quadrupeds and creatures cannot be rigged in the editor today; the rundown says so up front rather than discovering it here.
5. **Animation.** One `GenerateHumanoidAnimation` call per clip, issued together so they overlap. Text prompts for generic motion (idle, walk, run, wave, cheer). Video-to-motion for anything sport-, combat- or tool-specific; the rundown asks for a short reference clip. Then `MakeStationary` for in-place gameplay clips and `TrimToBestLoop` for idle and locomotion. Create the animator controller from the idle clip and add the other clips as states.

## Stage 4: Verify

1. Build the review scene and place the asset: `AssetGen.AssetGenCheck.BuildReviewScene(prefabPath, heightM)` through `Unity_RunCommand`. It adds a ground plane, a 1 m cube and a 1.75 m human silhouette beside the asset so scale reads at a glance. It refuses to run while a scene has unsaved changes, so save first.
2. Measure: `AssetGen.AssetGenCheck.Run(prefabPath, specPath, checkJsonPath)`. Thresholds and meanings are in `references/verify-checklist.md`.
3. Look: `Unity_SceneView_CaptureMultiAngleSceneView` with the instance ID returned by step 1. One capture per version, it is expensive. Characters get a second capture after `SamplePose(objectName, clipName, 0.5)`. Presets may add a capture, such as rendering through a game's palette camera.
4. Judge the sheet against the visual rubric in the checklist: silhouette, holes, floating parts, seams, symmetry, feet on ground, style match.
5. Auto-fix only cheap, reversible things: import scale to hit the spec height, a parent object to move the pivot, shader swap to the project's pipeline, texture max size. Re-run the check after a fix. Everything else goes on the fix list.

Never regenerate silently. A failed check produces a report and a recommendation; the user decides whether to spend more credits.

## Stage 5: Deliver

One folder per asset, one subfolder per version:

    Assets/Generated/<slug>/spec.json
    Assets/Generated/<slug>/refs/                concept views, backgrounds removed
    Assets/Generated/<slug>/v1/<slug>_v1.prefab  mesh, .fbm textures and materials beside it
    Assets/Generated/<slug>/v1/Animations/       clips and controller
    Assets/Generated/<slug>/v1/check.json  sheet.png  report.md

Build `report.md` with `scripts/report.py spec.json check.json --sheet sheet.png --out report.md`. Reply in chat with the sheet, the pass/fail table, the fix list and the prefab path. Keep the chat short; the report holds the detail.

## Stage 6: Improve

Route feedback to the cheapest stage that fixes it, then run only from there into a new `v<N+1>/` folder. Earlier versions stay. When the fix is non-generative (import scale, material edit, pivot parent), copy the previous version's mesh, textures and materials into the new folder first and edit the copies, so the old prefab keeps rendering exactly as it did.

| Feedback sounds like | Rerun from | Not this |
|---|---|---|
| wrong colours, too shiny, blurry texture | Texture (3.3), or a material smoothness fix | new mesh |
| too heavy, too many triangles | Retopology (3.2) | new mesh |
| wrong size, floating, sunk, pivot off | import scale or parent fix (4.5) | anything generative |
| shape or silhouette wrong, missing part | Concept (Stage 2), then mesh | texture |
| pose, motion, foot sliding, wrong action | Animation (3.5); video-to-motion after text fails twice | rig |
| bends wrong, joints misplaced | Rig (3.4) | animation |
| "not in the game's style" | check the preset first; usually Texture, sometimes Concept | mesh |

Stop after two improvement passes on the same complaint and say why: at that point the fix is the brief or the preset, not another roll.

## Decision rules

- Tripo P2 is the default mesh model. Hunyuan 3D 3.1 Pro for organic, rounded or creature shapes. Quad output only when the user will hand-edit in Blender.
- Multi-view whenever the concept stage ran. It is the biggest quality lever for game assets.
- Mobile budgets by default; PC budgets only when the project config says so.
- One mesh per run. Candidates belong at the concept stage where they cost a fraction.
- Ask for a reference video for any sport, combat or tool-use animation. Text-to-motion is fine for idle, locomotion and gestures.

## Budget and safety

- State the number of generations in the rundown. Per-model credit costs show in the editor's generator window; when the user reports them, record them in the report so estimates calibrate.
- Generation calls block the session. Warn once in the rundown, then do not narrate waits.
- One editor, unique savePath, background-removed references, saved scenes before scene builds. Full list in `references/unity-traps.md`; read it once per session.
- Never delete a version folder. Never edit game scenes; the review scene is the only scene this skill writes.

## Reference map

- `references/spec-sheet.md`: rundown template, spec.json schema, defaults per class and platform. Stage 1.
- `references/models.md`: model IDs, inputs, when to use which, known unknowns. Stage 3.
- `references/style-presets.md`: prompt scaffolds, per-preset finish and verify steps, project config. Stages 2 to 4.
- `references/verify-checklist.md`: thresholds, visual rubric, allowed auto-fixes with snippets. Stage 4.
- `references/unity-traps.md`: MCP and Unity AI gotchas. Once per session.
- `assets/AssetGenCheck.cs`: editor script the skill installs. `scripts/contact_sheet.py`, `scripts/report.py`: run with python3.
