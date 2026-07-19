// EMERGENCE — one-shot: copy the cached Blacksmith .unitypackage into the project
// (Asset Store cache lives in %APPDATA%, unreachable from the mount; PM import
// chokes on a malformed pathname entry, so we materialize it ourselves.)
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class CopyBlacksmithPkg
    {
        [MenuItem("Emergence/Tools/Copy Blacksmith Package To _incoming")]
        public static void Run()
        {
            var root = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x");
            var dstDir = Path.Combine(Directory.GetCurrentDirectory(), "_incoming");
            Directory.CreateDirectory(dstDir);
            int n = 0;
            if (Directory.Exists(root))
            {
                foreach (var f in Directory.GetFiles(root, "*.unitypackage", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(f).ToLowerInvariant();
                    if (!name.Contains("blacksmith")) continue;
                    var dst = Path.Combine(dstDir, Path.GetFileName(f));
                    if (File.Exists(dst)) { File.SetAttributes(dst, FileAttributes.Normal); File.Delete(dst); }
                    File.Copy(f, dst);
                    File.SetAttributes(dst, FileAttributes.Normal);
                    Debug.Log($"[CopyBlacksmithPkg] {f} -> {dst}");
                    n++;
                }
            }
            Debug.Log($"[CopyBlacksmithPkg] done, {n} file(s) copied");
        }
    }
}
#endif
