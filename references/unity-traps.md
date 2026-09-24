# Unity MCP and Unity AI traps

Learned on real runs. Each one cost time or credits once; read this once per session.

- **One editor open.** With two, MCP commands route to the wrong project. Confirm `Application.dataPath` in the first RunCommand of a session and stop if it is not the expected project.
- **Empty model list** means the project is not linked to Unity Cloud (Edit > Project Settings > Services). Nothing generates until it is.
- **Reference images must have the background removed.** The mesh models turn backgrounds into geometry. Run RemoveImageBackground even on images that look clean.
- **Unique savePath on every generation.** Parallel generations with default names overwrite each other. Put the version folder and a descriptive name in every path, with the right extension: `.png` images, `.fbx` meshes, `.anim` clips, `.controller`.
- **Generation blocks the session** for tens of seconds to minutes and the tool requires explicit consent once per conversation. The rundown approval is that consent; do not ask twice.
- **Check for interrupted generations first** (`Unity_AssetGeneration_ManageInterrupted`, List). Resuming is free; regenerating is not.
- **Multi-angle capture is expensive.** One per version, plus one mid-clip frame for characters. Do not capture after every small tweak.
- **Compile errors after CreateScript.** Read the console before calling anything in a freshly installed script; a broken editor script blocks every later RunCommand.
- **RunCommand rules.** Class must be `internal class CommandScript : IRunCommand`; log with `result.Log`. Register creations and modifications on `result` so they are undoable.
- **Review scene builder refuses dirty scenes.** Save the user's scene before Stage 4 or the call throws on purpose.
- **Play mode through MCP freezes when the editor is unfocused.** Only needed if a preset captures in play mode: set `Application.runInBackground` false then true after entering play mode.
- **JsonUtility limits.** spec.json and check.json must be flat objects and arrays of objects. No dictionaries, no nulls; use empty strings.
- **Do not build players from MCP.** BuildPlayer fails with "user interactions not supported"; not this skill's job anyway.
- **Rig import.** A rigged FBX may import as Generic. Check `importAnimationType` in check.json and switch the importer's Rig tab to Humanoid before judging the rig as failed.
