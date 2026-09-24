// AssetGenCheck.cs
// Installed by the asset-forge skill into Assets/Editor/AssetGen/. Editor-only.
// Measures a generated prefab against its spec.json, builds a neutral review scene, and samples animation poses.
//
// From Unity MCP RunCommand (class CommandScript : IRunCommand):
//   result.Log(AssetGen.AssetGenCheck.Run(prefabPath, specPath, outJsonPath));
//   var go = AssetGen.AssetGenCheck.BuildReviewScene(prefabPath, heightM); result.Log("instance {0}", go.GetInstanceID());
//   result.Log(AssetGen.AssetGenCheck.SamplePose("stumps_v1", "Idle", 0.5f));
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetGen
{
    [Serializable] public class SpecAnimation { public string name = ""; public string source = ""; public string prompt = ""; public string path = ""; }

    [Serializable]
    public class Spec
    {
        public string slug = ""; public string assetClass = ""; public string style = ""; public string platform = "";
        public float height_m; public float height_tolerance = 0.1f; public string pivot = "base-centre";
        public int max_triangles; public int max_materials; public int max_texture; public string texture_mode = "pbr";
        public string rig = "none"; public SpecAnimation[] animations = new SpecAnimation[0];
    }

    [Serializable] public class MaterialInfo { public string name; public string shader; public bool errorShader; public bool missingMainTexture; public int mainTextureSize; }
    [Serializable] public class ClipInfo { public string name; public float length; public bool loop; public bool humanoid; }

    [Serializable]
    public class Report
    {
        public string prefab; public bool ok;
        public List<string> failures = new List<string>();
        public List<string> warnings = new List<string>();
        public float height_m, width_m, depth_m, boundsMinY, pivotOffsetX, pivotOffsetY, pivotOffsetZ;
        public string pivotVerdict = "";
        public int triangles, vertices, submeshes, meshCount, materialCount, boneCount;
        public List<MaterialInfo> materials = new List<MaterialInfo>();
        public bool hasAnimator, hasAvatar, avatarValid, avatarHuman;
        public List<ClipInfo> clips = new List<ClipInfo>();
        public string importAnimationType = ""; public float importScale = 1f; public string sourceModel = "";
    }

    public static class AssetGenCheck
    {
        public const string ReviewFolder = "Assets/Generated/_Review";
        public const string ReviewScenePath = ReviewFolder + "/AssetGenReview.unity";

        // ---------- measure ----------

        public static string Run(string prefabPath, string specPath, string outJsonPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return "ERROR: prefab not found at " + prefabPath;
            Spec spec = null;
            if (!string.IsNullOrEmpty(specPath) && File.Exists(specPath))
                spec = JsonUtility.FromJson<Spec>(File.ReadAllText(specPath));

            var r = new Report { prefab = prefabPath };
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject go = null;
            try
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;
                Measure(go, r);
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                EditorSceneManager.ClosePreviewScene(preview);
            }
            if (spec != null) Compare(r, spec); else r.pivotVerdict = "unchecked (no spec)";
            r.ok = r.failures.Count == 0;

            if (!string.IsNullOrEmpty(outJsonPath))
            {
                var dir = Path.GetDirectoryName(outJsonPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(outJsonPath, JsonUtility.ToJson(r, true));
                if (outJsonPath.StartsWith("Assets/")) AssetDatabase.ImportAsset(outJsonPath);
            }
            return Summary(r);
        }

        static void Measure(GameObject go, Report r)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            var bounds = new Bounds();
            var meshes = new List<Mesh>();

            foreach (var ren in renderers)
            {
                var smr = ren as SkinnedMeshRenderer;
                var mr = ren as MeshRenderer;
                if (smr == null && mr == null) continue;

                if (!hasBounds) { bounds = ren.bounds; hasBounds = true; } else bounds.Encapsulate(ren.bounds);

                Mesh mesh = null;
                if (smr != null)
                {
                    mesh = smr.sharedMesh;
                    r.boneCount = Mathf.Max(r.boneCount, smr.bones != null ? smr.bones.Length : 0);
                }
                else
                {
                    var mf = ren.GetComponent<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                }
                if (mesh != null && !meshes.Contains(mesh)) meshes.Add(mesh);

                foreach (var mat in ren.sharedMaterials)
                {
                    var mi = new MaterialInfo();
                    if (mat == null) { mi.name = "(null)"; mi.shader = "(none)"; mi.errorShader = true; mi.missingMainTexture = true; }
                    else
                    {
                        mi.name = mat.name;
                        mi.shader = mat.shader != null ? mat.shader.name : "(none)";
                        mi.errorShader = mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || !mat.shader.isSupported;
                        Texture tex = null;
                        if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
                        if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                        mi.missingMainTexture = tex == null;
                        mi.mainTextureSize = tex != null ? Mathf.Max(tex.width, tex.height) : 0;
                    }
                    if (!r.materials.Exists(x => x.name == mi.name)) r.materials.Add(mi);
                }
            }

            r.materialCount = r.materials.Count;
            r.meshCount = meshes.Count;
            foreach (var m in meshes)
            {
                r.vertices += m.vertexCount;
                r.submeshes += m.subMeshCount;
                for (int s = 0; s < m.subMeshCount; s++)
                    if (m.GetTopology(s) == MeshTopology.Triangles) r.triangles += (int)(m.GetIndexCount(s) / 3);
                if (string.IsNullOrEmpty(r.sourceModel))
                {
                    var p = AssetDatabase.GetAssetPath(m);
                    if (!string.IsNullOrEmpty(p))
                    {
                        r.sourceModel = p;
                        var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                        if (imp != null) { r.importAnimationType = imp.animationType.ToString(); r.importScale = imp.globalScale; }
                    }
                }
            }

            if (hasBounds)
            {
                r.height_m = bounds.size.y; r.width_m = bounds.size.x; r.depth_m = bounds.size.z;
                r.boundsMinY = bounds.min.y;
                r.pivotOffsetX = bounds.center.x; r.pivotOffsetY = bounds.center.y; r.pivotOffsetZ = bounds.center.z;
            }
            else r.warnings.Add("no MeshRenderer or SkinnedMeshRenderer found");

            var anim = go.GetComponentInChildren<Animator>(true);
            r.hasAnimator = anim != null;
            if (anim != null)
            {
                r.hasAvatar = anim.avatar != null;
                r.avatarValid = anim.avatar != null && anim.avatar.isValid;
                r.avatarHuman = anim.avatar != null && anim.avatar.isHuman;
                if (anim.runtimeAnimatorController != null)
                {
                    foreach (var c in anim.runtimeAnimatorController.animationClips)
                    {
                        if (c == null || r.clips.Exists(x => x.name == c.name)) continue;
                        r.clips.Add(new ClipInfo { name = c.name, length = c.length, loop = c.isLooping, humanoid = c.humanMotion });
                    }
                }
            }
        }

        static void Compare(Report r, Spec s)
        {
            float h = Mathf.Max(r.height_m, 0.001f);
            if (s.height_m > 0f)
            {
                float tol = s.height_tolerance > 0f ? s.height_tolerance : 0.1f;
                float err = Mathf.Abs(r.height_m - s.height_m) / s.height_m;
                if (err > tol)
                    r.failures.Add(string.Format("height {0:F3} m vs spec {1:F3} m ({2:P0} off) -> import scale x{3:F3}",
                        r.height_m, s.height_m, err, s.height_m / h));
            }

            float tolXZ = 0.05f * h;
            bool centred = Mathf.Abs(r.pivotOffsetX) <= tolXZ && Mathf.Abs(r.pivotOffsetZ) <= tolXZ;
            string pv = string.IsNullOrEmpty(s.pivot) ? "base-centre" : s.pivot;
            if (pv == "base-centre" || pv == "feet")
            {
                bool onGround = Mathf.Abs(r.boundsMinY) <= tolXZ;
                r.pivotVerdict = (centred && onGround) ? "ok" : (onGround ? "off-centre" : "not-at-base");
            }
            else if (pv == "centre")
            {
                bool centredY = Mathf.Abs(r.pivotOffsetY) <= tolXZ;
                r.pivotVerdict = (centred && centredY) ? "ok" : "off-centre";
            }
            else r.pivotVerdict = "unchecked (" + pv + ")";
            if (r.pivotVerdict != "ok" && !r.pivotVerdict.StartsWith("unchecked"))
                r.failures.Add(string.Format("pivot {0}: bounds min y {1:F3}, centre offset ({2:F3}, {3:F3}, {4:F3}) -> parent offset fix",
                    r.pivotVerdict, r.boundsMinY, r.pivotOffsetX, r.pivotOffsetY, r.pivotOffsetZ));

            if (s.max_triangles > 0 && r.triangles > s.max_triangles)
                r.failures.Add(string.Format("triangles {0} > budget {1} -> retopology", r.triangles, s.max_triangles));
            if (s.max_materials > 0 && r.materialCount > s.max_materials)
                r.warnings.Add(string.Format("materials {0} > budget {1}; consider atlasing", r.materialCount, s.max_materials));

            foreach (var m in r.materials)
            {
                if (m.errorShader) r.failures.Add("material '" + m.name + "' shader '" + m.shader + "' missing or unsupported -> shader swap");
                if (m.missingMainTexture) r.warnings.Add("material '" + m.name + "' has no base texture");
                if (s.max_texture > 0 && m.mainTextureSize > s.max_texture)
                    r.warnings.Add(string.Format("material '{0}' texture {1}px > {2}px -> importer max size", m.name, m.mainTextureSize, s.max_texture));
            }

            if (s.rig == "biped")
            {
                if (!r.hasAnimator || !r.avatarValid || !r.avatarHuman)
                    r.failures.Add(string.Format("rig: expected Humanoid avatar (animator={0}, valid={1}, human={2}) -> rig stage, or importer Rig tab = Humanoid",
                        r.hasAnimator, r.avatarValid, r.avatarHuman));
                if (!string.IsNullOrEmpty(r.importAnimationType) && r.importAnimationType != "Human")
                    r.warnings.Add("model importer animationType is " + r.importAnimationType + ", expected Human");
            }

            if (s.animations != null)
            {
                foreach (var a in s.animations)
                {
                    if (a == null || string.IsNullOrEmpty(a.name)) continue;
                    if (!r.clips.Exists(c => c.name.IndexOf(a.name, StringComparison.OrdinalIgnoreCase) >= 0))
                        r.failures.Add("animation '" + a.name + "' missing from controller -> animation stage");
                }
            }
        }

        static string Summary(Report r)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendFormat("{0} | {1:F2}x{2:F2}x{3:F2} m | pivot {4} | {5} tris, {6} verts, {7} mats, {8} meshes | rig animator={9} human={10} bones={11} | clips {12}\n",
                r.ok ? "PASS" : "FAIL", r.width_m, r.height_m, r.depth_m, r.pivotVerdict, r.triangles, r.vertices, r.materialCount, r.meshCount,
                r.hasAnimator, r.avatarHuman, r.boneCount, r.clips.Count);
            foreach (var f in r.failures) sb.Append("FAIL: ").Append(f).Append('\n');
            foreach (var w in r.warnings) sb.Append("WARN: ").Append(w).Append('\n');
            return sb.ToString();
        }

        // ---------- review scene ----------

        public static GameObject BuildReviewScene(string prefabPath, float heightM)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc.isDirty && sc.path != ReviewScenePath)
                    throw new InvalidOperationException("Scene '" + sc.name + "' has unsaved changes. Save it first; the review scene replaces the open scenes.");
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException("prefab not found: " + prefabPath);
            Directory.CreateDirectory(ReviewFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float half = Mathf.Max(heightM, 0.5f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ApplyMaterial(ground, "ReviewGround", new Color(0.45f, 0.45f, 0.47f));

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Ref_1m_Cube";
            cube.transform.position = new Vector3(-(half + 1.0f), 0.5f, 0f);
            ApplyMaterial(cube, "ReviewCube", new Color(0.9f, 0.55f, 0.2f));

            var human = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            human.name = "Ref_1p75m_Human";
            human.transform.localScale = new Vector3(0.4f, 0.875f, 0.4f);
            human.transform.position = new Vector3(half + 1.0f, 0.875f, 0f);
            ApplyMaterial(human, "ReviewHuman", new Color(0.3f, 0.6f, 0.9f));

            var key = new GameObject("Key Light").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.2f;
            key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.4f;
            fill.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.38f);

            var camGo = new GameObject("Review Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            float dist = Mathf.Max(3f, heightM * 2.5f);
            camGo.transform.position = new Vector3(dist * 0.6f, heightM * 0.7f + 0.6f, -dist);
            camGo.transform.LookAt(new Vector3(0f, heightM * 0.5f, 0f));

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            var anim = go.GetComponentInChildren<Animator>(true);
            if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            EditorSceneManager.SaveScene(scene, ReviewScenePath);
            Selection.activeGameObject = go;
            var view = SceneView.lastActiveSceneView;
            if (view != null) view.FrameSelected();
            return go;
        }

        static void ApplyMaterial(GameObject go, string name, Color c)
        {
            var path = ReviewFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(mat);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ---------- animation pose ----------

        public static string SamplePose(string objectName, string clipName, float normalizedTime)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return "ERROR: object not found: " + objectName;
            var anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null || anim.runtimeAnimatorController == null) return "ERROR: no Animator with a controller on " + objectName;
            AnimationClip clip = null;
            foreach (var c in anim.runtimeAnimatorController.animationClips)
                if (c != null && c.name.IndexOf(clipName, StringComparison.OrdinalIgnoreCase) >= 0) { clip = c; break; }
            if (clip == null) return "ERROR: clip not found: " + clipName;
            float t = Mathf.Clamp01(normalizedTime) * clip.length;
            clip.SampleAnimation(anim.gameObject, t);
            SceneView.RepaintAll();
            return string.Format("sampled '{0}' at {1:F2}s of {2:F2}s", clip.name, t, clip.length);
        }

        // ---------- manual entry point ----------

        [MenuItem("Tools/AssetGen/Check Selected Prefab")]
        static void CheckSelected()
        {
            var obj = Selection.activeObject;
            var path = obj != null ? AssetDatabase.GetAssetPath(obj) : null;
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) { Debug.LogWarning("AssetGen: select a prefab first"); return; }
            var versionDir = Path.GetDirectoryName(path);
            var specPath = Path.Combine(Path.GetDirectoryName(versionDir), "spec.json").Replace('\\', '/');
            Debug.Log(Run(path, File.Exists(specPath) ? specPath : "", Path.Combine(versionDir, "check.json").Replace('\\', '/')));
        }
    }
}
