# Verify checklist

Two halves: numbers from `AssetGenCheck.Run` and eyes on the multi-angle sheet. Both go into `check.json` and `report.md`. A failed check is information for the user, not a trigger to regenerate.

## Automated checks

| Check | Pass condition | On fail, rerun from | Auto-fix allowed |
|---|---|---|---|
| Height | within `height_tolerance` (default 10%) of `height_m` | import scale | yes |
| Pivot `base-centre` / `feet` | bounds min y within 5% of height of 0; x and z centre offset within 5% of height | parent offset | yes |
| Pivot `centre` | bounds centre within 5% of height of the origin | parent offset | yes |
| Pivot `grip` | unchecked by script; judge from the sheet | parent offset | yes |
| Triangles | ≤ `max_triangles` | Retopology | no |
| Materials | ≤ `max_materials` (warning only) | none; atlas later if it matters | no |
| Shader | no null material, no error or unsupported shader | shader swap | yes |
| Main texture | present on every material (warning) | Texture | no |
| Texture size | ≤ `max_texture` (warning) | importer max size | yes |
| Rig | when `rig: biped`: Animator with a valid Humanoid avatar; importer animationType Human | Rig, or importer Rig tab | importer only |
| Clips | every spec animation name found in the controller | Animation | no |

## Visual rubric for the 2x2 sheet

Score each line pass or fail and list the fails in the report. The isometric tile shows silhouette and style; front and right show proportion and seams; top shows footprint and pivot.

1. Silhouette reads at thumbnail size and matches the picked concept.
2. No holes, floating shards, or inside-out faces (dark patches that ignore lighting).
3. No baked-in background or ground disc.
4. Symmetry where the subject should be symmetric.
5. Texture seams and smearing not visible from the front and isometric views.
6. Base or feet on the ground, nothing sunk or hovering.
7. Style matches the preset: colours, shading, detail level.
8. Characters: proportions match the spec; hands and feet not fused; face not smeared.
9. Mid-clip frame: pose plausible, feet not sliding through the ground, limbs not intersecting the body.

## Auto-fix snippets (RunCommand, class CommandScript)

Import scale so the asset hits the spec height:

    var imp = (ModelImporter)AssetImporter.GetAtPath(meshPath);
    imp.globalScale = imp.globalScale * (specHeight / measuredHeight);
    imp.SaveAndReimport();

Pivot fix without touching the mesh: wrap in an empty parent and save that as the delivered prefab.

    var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(srcPrefab));
    var root = new GameObject(slug + "_v" + n);
    inst.transform.SetParent(root.transform, false);
    inst.transform.localPosition = new Vector3(-centerX, -minY, -centerZ);   // from check.json
    PrefabUtility.SaveAsPrefabAsset(root, dstPrefab);
    Object.DestroyImmediate(root);

Shader swap to the project pipeline:

    var shader = Shader.Find(pipelineShader);   // from project.json
    foreach (var matPath in materialPaths) { var m = AssetDatabase.LoadAssetAtPath<Material>(matPath); m.shader = shader; EditorUtility.SetDirty(m); }
    AssetDatabase.SaveAssets();

Texture max size:

    var ti = (TextureImporter)AssetImporter.GetAtPath(texPath); ti.maxTextureSize = maxTexture; ti.SaveAndReimport();

Re-run `AssetGenCheck.Run` after any auto-fix and keep the new check.json.

## Not allowed without asking

Regenerating any stage, deleting a version folder, editing a game scene, changing project settings. Say what you would do and let the user decide.
