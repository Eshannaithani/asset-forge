# asset-forge

A Claude Code skill that turns a one-line brief into a verified, game-ready Unity asset: props, hard-surface objects, characters and their animation clips. It drives the AI generators already inside Unity 6 through the Unity MCP server, measures the result inside the editor, delivers a prefab with a report card, and improves the asset from plain-language feedback.

```bash
/asset-forge a wooden market cart, low-poly, for mobile
/asset-forge --plan a stylized knight with idle and attack animations
```

## Related Systems Architecture & Case Study

`asset-forge` was built to accelerate asset authoring and rapid prototyping for intelligent gameplay ecosystems:

- **Interactive AI Lab & Case Study**: [Unity 6 AI NPC Behavior System Architecture](https://www.eshannaithani.com/labs/ai-npc-system/architecture?utm_source=github&utm_medium=repo&utm_campaign=asset_forge) — Inspect an autonomous tactical guard AI with vision, hearing, and NavMesh dynamic pathfinding.
- **Engineering Services**: [AI Gameplay Systems & Unity Development](https://www.eshannaithani.com/services/ai-gameplay-systems?utm_source=github&utm_medium=repo&utm_campaign=asset_forge) — Custom AI state machines, procedural systems, and gameplay engineering.

## What it does

1. **Brief to rundown.** Writes a spec sheet (size, pivot, triangle budget, texture mode, rig, clips, generation estimate) from per-class defaults and asks you to approve it.
2. **Concept.** Generates two concept images in your style preset, removes backgrounds, shows a contact sheet, and turns your pick into front, back, left and right views.
3. **Generate and finish.** Multi-view image-to-3D, retopology only when over budget, re-texture only when the style needs it, biped rigging, text- or video-to-motion clips, animator controller.
4. **Verify.** Builds a neutral review scene with a 1 m cube and a 1.75 m silhouette, runs an editor checker against the spec (height, pivot, triangles, materials, shaders, avatar, clips), captures a multi-angle sheet, applies only cheap reversible fixes.
5. **Deliver.** One folder per asset, one subfolder per version, with `spec.json`, `check.json`, `sheet.png` and `report.md`.
6. **Improve.** Routes feedback to the cheapest stage that fixes it ("too shiny" is a material edit, "wrong shape" goes back to concept) and never deletes an earlier version.

Everything runs against the Unity editor you already have open. No API keys, no external services, no model training.

## Requirements

- Unity 6.0 or newer with Unity AI enabled and the project linked to Unity Cloud (Edit > Project Settings > Services). Generation spends Unity AI credits.
- The Unity MCP server connected to Claude Code (the skill uses its `Unity_AssetGeneration_*`, `Unity_RunCommand` and scene-capture tools).
- Python 3 with Pillow for the two helper scripts.
- One Unity editor open at a time.

## Install

For every project on this machine:

```bash
git clone https://github.com/Eshannaithani/asset-forge.git ~/.claude/skills/asset-forge
```

For one project only:

```bash
git clone https://github.com/Eshannaithani/asset-forge.git .claude/skills/asset-forge
```

Update later with `git pull` in that folder. Claude Code picks the skill up on the next session.

## Project setup

On the first run in a project the skill creates `Assets/Generated/_config/project.json` with the style preset, platform and output folder, and installs its checker script at `Assets/Editor/AssetGen/AssetGenCheck.cs`.

Game-specific presets (palettes, look-test scenes, capture menus, reference assets) go in `Assets/Generated/_config/presets.md` in the same format as `references/style-presets.md`. A project preset with the same name overrides the shared one. Keep those details in the project, not in this repo.

## Modes

- **Full run**: the six stages above, with two stops: approve the rundown, pick a concept.
- **Plan only** (`--plan` or "plan only"): rundown plus the exact call list, no credits spent.
- **Improve**: feedback on an existing asset under `Assets/Generated/<slug>/`.
- **Own reference**: give an image path and the concept stage is skipped.

## Layout

```
SKILL.md                      workflow, gates, routing table, decision rules
references/spec-sheet.md      rundown template, defaults per asset class, spec.json schema
references/models.md          model ids exposed by Unity AI and when to use which
references/style-presets.md   prompt scaffolds and finish rules per style
references/verify-checklist.md thresholds, visual rubric, allowed auto-fixes
references/unity-traps.md     Unity MCP and Unity AI gotchas
scripts/contact_sheet.py      stitches images into one labelled grid
scripts/report.py             builds report.md from spec.json and check.json
assets/AssetGenCheck.cs       editor script: measure, review scene, pose sampling
evals/evals.json              plan-only test prompts with expectations
```

## Testing

The prompts in `evals/evals.json` run in plan-only mode and cost no credits. Run them with the Claude Code skill-creator loop or by pasting them into a session with a Unity project open, then compare the rundown against the expectations. The first real run in a project answers the known unknowns listed at the end of `references/models.md`; update that file with what the generators actually return.

## Status

Version 0.1. Plan-only evals pass. The full generate-and-verify path has been exercised stage by stage but not yet end to end on a fresh asset, so expect to confirm the retopology face-count field, the mesh output format and the rig import type on your first run.

## License

MIT.
