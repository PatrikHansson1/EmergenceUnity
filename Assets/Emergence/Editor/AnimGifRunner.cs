// EMERGENCE P1 — animation GIF (TD-029c): show LIVE motion without play mode. Each character's
// GLB clip is sampled across 24 time-steps in EDIT mode; a frame is captured per step; the cloud
// assembles them into a looping GIF. Root position is reset each frame so they animate IN PLACE
// (walk clips carry root motion that would otherwise drift them out of frame).
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class AnimGifRunner
    {
        const string CharDir = "Assets/Emergence/Models/characters/";
        const string NatureDir = "Assets/Emergence/Models/nature/";
        const string FrameDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\audition-lightrig\anim-frames";
        const int N = 24;

        class Actor { public GameObject go; public AnimationClip clip; public Vector3 pos; public Quaternion rot; }

        [MenuItem("Emergence/P1 Dressing/RUN ANIM GIF (edit-mode sampled)")]
        public static void Run()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var camGo = new GameObject("AnimCam"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.farClipPlane = 500f;
            cam.transform.position = new Vector3(0f, 1.7f, -10f);
            cam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            var gm = new Material(Shader.Find("Universal Render Pipeline/Lit")); gm.SetColor("_BaseColor", new Color(0.30f, 0.42f, 0.20f));
            ground.GetComponent<Renderer>().sharedMaterial = gm;

            var actors = new List<Actor>();
            void Add(string dir, string nm, float x)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(dir + nm + ".glb");
                if (pf == null) { Debug.LogWarning("[AnimGif] missing " + nm); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                go.transform.position = new Vector3(x, 0f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 200f, 0f); // ~3/4 toward camera — stride reads
                AnimationClip clip = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(dir + nm + ".glb"))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview")) { clip = c; break; }
                actors.Add(new Actor { go = go, clip = clip, pos = go.transform.position, rot = go.transform.rotation });
            }
            Add(CharDir, "villager-f-walk", -4.4f);
            Add(CharDir, "villager-work", -2.2f);
            Add(CharDir, "villager", 0f);
            Add(NatureDir, "deer", 2.4f);
            Add(NatureDir, "wolf", 4.6f);

            EmergenceLightRig.Apply("spring", "day");
            EmergencePostStack.Apply("day");

            Directory.CreateDirectory(FrameDir);
            for (int i = 0; i < N; i++)
            {
                float ph = (float)i / N;
                foreach (var a in actors)
                {
                    if (a.clip != null && a.clip.length > 0f) a.clip.SampleAnimation(a.go, ph * a.clip.length);
                    a.go.transform.position = a.pos;   // kill root-motion drift — animate in place
                    a.go.transform.rotation = a.rot;
                }
                Capture(cam, $"frame-{i:00}");
            }
            Debug.Log($"[AnimGif] {N} frames + {actors.Count} actors written to {FrameDir}");
        }

        static void Capture(Camera cam, string name)
        {
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(FrameDir, $"{name}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
    }
}
#endif
