// EMERGENCE — Asset Intake render-test (headless). Drop Reports/RUN_INTAKE.trigger.
// Builds a lineup: a Village Pack house (reference) beside key City Pack buildings, under our
// day light rig, on a plane. Captures + magenta-scans → the Stage-1+3 intake gate in action.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class CityIntakeRunner
    {
        const string EvidenceDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\audition-lightrig";
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_INTAKE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "INTAKE_DONE.txt");

        static CityIntakeRunner() { EditorApplication.update += Tick; }

        static GameObject Find(string name)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            return null;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            try
            {
                File.Delete(Trigger);
                Directory.CreateDirectory(Path.GetDirectoryName(Done));
                File.WriteAllText(Done, "RUNNING\n");

                UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                var root = new GameObject("Intake");

                // ground plane
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.SetParent(root.transform); ground.transform.localScale = Vector3.one * 12f;
                var gm = ground.GetComponent<MeshRenderer>();
                gm.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.35f, 0.5f, 0.25f) };

                // lineup: reference Village house + key City Pack buildings
                string[] names = { "P_BLD_house_01", "BLD_03_16x8_Ext_01", "BLD_03_L_Ext_01",
                                   "P_MOD_CityWall_03_tower", "P_MOD_Bridge_03",
                                   "COMP_PROP_statue_city_01", "COMP_PROP_marketstall_city_01" };
                float x = -36f; int placed = 0; var report = "";
                var placedGos = new System.Collections.Generic.List<GameObject>();
                foreach (var nm in names)
                {
                    var pf = Find(nm);
                    report += nm + (pf != null ? " OK" : " MISSING") + "\n";
                    if (pf == null) { x += 12f; continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                    go.transform.position = new Vector3(x, 0f, 0f);
                    x += 12f; placed++; placedGos.Add(go);
                }

                var camGo = new GameObject("DocCamera"); camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 55f;
                // auto-frame the placed BUILDINGS (ignore the huge ground plane) so offsets/scale don't matter
                Bounds b = new Bounds(); bool has = false;
                foreach (var go in placedGos)
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
                if (has)
                {
                    float ext = Mathf.Max(b.extents.magnitude, 5f);
                    cam.transform.position = b.center + new Vector3(ext * 0.15f, ext * 0.55f, -ext * 2.0f);
                    cam.transform.LookAt(b.center);
                    cam.farClipPlane = ext * 12f + 2000f;
                    report += $"bounds center={b.center} size={b.size}\n";
                }
                else { cam.transform.position = new Vector3(0, 14, -34); cam.transform.rotation = Quaternion.Euler(16, 0, 0); }

                EmergenceLightRig.Apply("spring", "day");

                // capture
                const int w = 2560, h = 1440;
                var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                var px = tex.GetPixels32(); int magenta = 0;
                foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
                Directory.CreateDirectory(EvidenceDir);
                File.WriteAllBytes(Path.Combine(EvidenceDir, "citypack-intake.png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);

                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} placed={placed}/{names.Length} magenta={magenta}\n" + report);
                Debug.Log($"[CityIntake] placed={placed} magenta={magenta}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
