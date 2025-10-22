using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace jorobledodu.folderSetup
{
    public static class FolderOps
    {
        static readonly HashSet<string> NonContentExtensions = new HashSet<string>{".meta",".gitkeep"};

        public static void CreateAll(IEnumerable<(string path, bool isFile)> entries, bool createGitkeepForEmptyFolders)
        {
            foreach (var (path, isFile) in entries)
            {
                if (isFile)
                {
                    EnsureParentFolder(Path.GetDirectoryName(path));
                    CreateFile(path);
                }
                else
                {
                    EnsureFolder(path);
                }
            }

            if (createGitkeepForEmptyFolders)
            {
                foreach (var (path, isFile) in entries)
                {
                    if (!isFile && AssetDatabase.IsValidFolder(path))
                        TryCreateGitkeep(path);
                }
            }

            AssetDatabase.Refresh();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Replace("\\","/").Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                var sub = string.Join("/", parts.Take(i + 1));
                if (!AssetDatabase.IsValidFolder(sub))
                {
                    var parent = string.Join("/", parts.Take(i));
                    if (string.IsNullOrEmpty(parent)) parent = "Assets";
                    AssetDatabase.CreateFolder(parent, parts[i]);
                }
            }
        }

        public static void EnsureParentFolder(string dir)
        {
            if (!string.IsNullOrEmpty(dir)) EnsureFolder(dir.Replace("\\","/"));
        }

        static void CreateFile(string path)
        {
            path = path.Replace("\\","/");
            var ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".unity":
                    CreateEmptyScene(path);
                    break;
                case ".asset":
                    CreateTextAsset(path, "// TODO: Replace with your asset type.");
                    break;
                default:
                    if (!File.Exists(path)) File.WriteAllText(path, "");
                    break;
            }
        }

        static void CreateTextAsset(string path, string contents)
        {
            if (File.Exists(path)) return;
            File.WriteAllText(path, contents);
        }

        static void CreateEmptyScene(string path)
        {
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        public static void TryCreateGitkeep(string folderPath)
        {
            var full = Path.Combine(folderPath, ".gitkeep").Replace("\\","/");
            var sys = Path.GetFullPath(full);
            Directory.CreateDirectory(Path.GetDirectoryName(sys));
            if (!File.Exists(sys)) File.WriteAllText(sys, "");
        }

        public static bool FolderHasContent(string unityFolderPath)
        {
            var sysPath = Path.GetFullPath(unityFolderPath);
            if (!Directory.Exists(sysPath)) return false;

            var files = Directory.EnumerateFiles(sysPath, "*", SearchOption.AllDirectories).Select(p => p.Replace("\\","/"));
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (!NonContentExtensions.Contains(ext)) return true;
            }
            return false;
        }

        public static void DeleteFolders(IEnumerable<string> unityFolderPaths, bool deleteContentAnyway)
        {
            foreach (var folder in unityFolderPaths)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                if (!deleteContentAnyway && FolderHasContent(folder)) continue;

                FileUtil.DeleteFileOrDirectory(folder);
                var meta = folder + ".meta";
                if (File.Exists(meta)) FileUtil.DeleteFileOrDirectory(meta);
            }
            AssetDatabase.Refresh();
        }
    }
}
