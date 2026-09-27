// EMERGENCE — house contact sheet (ART-domain input for Patrik's eye, D-008/D-064).
// Drop Reports/RUN_HOUSESHEET.trigger. Optional body: comma-separated prefab names (exact); empty = P_BLD_house_01..14.
// Opens the floor scene (never saved), stands each house on a temporary ground plane beside a 1.75 m villager
// capsule at WorldDresser.HouseScale, captures a 3/4 view per house to 45-UNITY/evidence/houses/<date>/<name>.png,
// measures footprint/height at scale 1 and at HouseScale, writes housesheet-report.txt + Reports/HOUSESHEET_DONE.txt.
// Never edits a pack asset. Presentation-only.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoHouseSheet
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_HOUSESHEET.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "HOUSESHEET_DONE.txt");
        const float VillagerHeight = 1.75f;

        static AutoHouseSheet() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var rep = new StringBuilder();
            rep.AppendLine("HOUSE SHEET — " + DateTime.Now.ToString("s") + " HouseScale=" + WorldDresser.HouseScale);
            string verdict;
            try { verdict = Run(body, rep); }
            catch (Exception e) { verdict = "ERROR " + e.Message; rep.AppendLine(verdict + "\n" + e.StackTrace); }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoHouseSheet] " + verdict);
        }

        static string Run(string body, StringBuilder rep)
        {
            var names = string.IsNullOrEmpty(body)
                ? Enumerable.Range(1, 14).Select(i => $"P_BLD_house_{i:00}").ToList()
                : body.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            var evDir = Path.Combine(@"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\houses", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(evDir);

            var startScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(WorldDresser.FloorScenePath, OpenSceneMode.Single);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // temporary ground + villager reference (scene is never saved)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TMP_ground"; ground.transform.localScale = new Vector3(20, 1, 20);
            var gmat = new Material(Shader.Find("Universal Render Pipeline/Lit")); gmat.color = new Color(0.42f, 0.52f, 0.30f);
            ground.GetComponent<Renderer>().sharedMaterial = gmat;
            var villager = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            villager.name = "TMP_villager"; villager.transform.localScale = new Vector3(0.45f, VillagerHeight / 2f, 0.45f);
            var vmat = new Material(Shader.Find("Universal Render Pipeline/Lit")); vmat.color = new Color(0.85f, 0.80f, 0.70f);
            villager.GetComponent<Renderer>().sharedMaterial = vmat;

            var camGo = new GameObject("TMP_cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 35f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 2000f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            bool prevAsync = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            int done = 0;
            try
            {
                foreach (var n in names)
                {
                    var prefab = FindPrefabExact(n);
                    if (prefab == null) { rep.AppendLine("  MISSING " + n); continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
                    var b1 = Bounds(go);
                    go.transform.localScale = Vector3.one * WorldDresser.HouseScale;
                    var b = Bounds(go);
                    // stand on ground: lift so min y = 0
                    go.transform.position = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                    b = Bounds(go);
                    villager.transform.position = new Vector3(b.max.x + 0.8f, VillagerHeight / 2f, b.min.z + 0.5f);
                    // 3/4 view from front-right, slightly above
                    float r = Mathf.Max(b.extents.magnitude, 3f) * 2.6f;
                    var target = b.center + Vector3.right * 0.4f;
                    cam.transform.position = target + new Vector3(0.8f, 0.55f, -1.0f).normalized * r;
                    cam.transform.LookAt(target);
                    var file = Path.Combine(evDir, n + ".png");
                    var m = Capture(cam, file);
                    rep.AppendLine($"  HOUSE {n}: scale1 footprint {b1.size.x:0.0}x{b1.size.z:0.0} m height {b1.size.y:0.0} m · at {WorldDresser.HouseScale}: {b.size.x:0.0}x{b.size.z:0.0} m height {b.size.y:0.0} m (villager {VillagerHeight}) · magenta={m.magenta} meanLum={m.lum} -> {Path.GetFileName(file)}");
                    UnityEngine.Object.DestroyImmediate(go);
                    done++;
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(villager); UnityEngine.Object.DestroyImmediate(ground);
                UnityEngine.Object.DestroyImmediate(gmat); UnityEngine.Object.DestroyImmediate(vmat);
                try { if (!string.IsNullOrEmpty(startScene) && File.Exists(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
                      else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); } catch { }
            }
            File.WriteAllText(Path.Combine(evDir, "housesheet-report.txt"), rep.ToString());
            return $"OK houses={done}/{names.Count} dir={evDir}";
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        static GameObject FindPrefabExact(string name)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            return null;
        }

        static (int magenta, long lum) Capture(Camera cam, string file)
        {
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            var px = tex.GetPixels32(); int magenta = 0; long lum = 0;
            foreach (var c in px) { if (c.r > 220 && c.b > 220 && c.g < 80) magenta++; lum += c.r + c.g + c.b; }
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            return (magenta, lum / (3L * px.Length));
        }
    }
}
#endif
