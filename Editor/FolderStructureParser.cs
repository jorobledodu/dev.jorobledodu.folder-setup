using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace jorobledodu.folderSetup
{
    [Serializable]
    public class FolderNode
    {
        public string name;
        public bool isFile;
        public List<FolderNode> children = new List<FolderNode>();
    }

    public static class FolderStructureParser
    {
        static readonly string[] FileExtensions = {
            ".unity",".asset",".txt",".md",".json",".xml",".shader",".mat",".prefab",".png",".jpg",".cs",".asmdef"
        };

        public static bool LooksLikeJson(string text)
        {
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                return c == '{' || c == '[';
            }
            return false;
        }

        static bool LooksLikeFile(string name)
        {
            var lower = name.Trim().ToLowerInvariant();
            return FileExtensions.Any(ext => lower.EndsWith(ext));
        }

        public static FolderNode Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Empty structure text.");

            if (LooksLikeJson(text))
            {
                var node = JsonUtility.FromJson<FolderNode>(text);
                MarkFilesRecursively(node);
                return node;
            }

            var lines = text.Replace("\r\n","\n").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            int paths = lines.Count(l => l.TrimStart().StartsWith("Assets/") || l.TrimStart().StartsWith("Assets\\"));
            if (paths >= Math.Max(1, lines.Length/2))
                return ParsePathsPerLine(lines);

            return ParseIndented(lines);
        }

        static void MarkFilesRecursively(FolderNode node)
        {
            if (node == null) return;
            node.isFile = LooksLikeFile(node.name);
            if (node.children == null) node.children = new List<FolderNode>();
            foreach (var c in node.children) MarkFilesRecursively(c);
        }

        static string CleanLine(string line, out int indent)
        {
            var idxArrow = line.IndexOf('←');
            if (idxArrow >= 0) line = line.Substring(0, idxArrow);
            var idxHash = line.IndexOf('#');
            if (idxHash >= 0) line = line.Substring(0, idxHash);

            line = line.Replace("├"," ").Replace("└"," ").Replace("│"," ").Replace("─"," ");

            int i = 0; int spaces = 0;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            {
                spaces += (line[i] == '\t') ? 4 : 1;
                i++;
            }

            var rest = line.Substring(i).TrimStart();

            while (rest.StartsWith("-") || rest.StartsWith("*") || rest.StartsWith(">") || rest.StartsWith("•") || rest.StartsWith("|"))
                rest = rest.Substring(1).TrimStart();

            indent = spaces;
            return rest.TrimEnd('/',' ');
        }

        static FolderNode ParseIndented(IEnumerable<string> rawLines)
        {
            var root = new FolderNode { name = "Assets", isFile = false };
            var stack = new Stack<(int indent, FolderNode node)>();
            stack.Push((indent: -1, node: root));
            bool seenExplicitAssets = false;

            foreach (var raw in rawLines)
            {
                int indent;
                var content = CleanLine(raw, out indent);
                if (string.IsNullOrEmpty(content)) continue;

                var parts = content.Split(new[]{'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);

                if (!seenExplicitAssets && (parts.Length==1 && parts[0].Equals("Assets", StringComparison.OrdinalIgnoreCase)))
                {
                    seenExplicitAssets = true;
                    continue;
                }

                while (stack.Count > 0 && indent <= stack.Peek().indent) stack.Pop();
                var parent = stack.Peek().node;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (string.IsNullOrEmpty(part)) continue;

                    bool isFile = LooksLikeFile(part);
                    var existing = parent.children.FirstOrDefault(c => c.name == part);
                    if (existing == null)
                    {
                        existing = new FolderNode { name = part, isFile = isFile };
                        parent.children.Add(existing);
                    }
                    parent = existing;
                }

                stack.Push((indent, parent));
            }

            return root;
        }

        static FolderNode ParsePathsPerLine(IEnumerable<string> lines)
        {
            var root = new FolderNode { name = "Assets", isFile = false };
            foreach (var raw in lines)
            {
                var l = raw.Trim();
                if (string.IsNullOrEmpty(l)) continue;
                var idx = l.IndexOf('#');
                if (idx >= 0) l = l.Substring(0, idx).Trim();
                if (string.IsNullOrEmpty(l)) continue;

                var parts = l.Replace("\\","/").Split(new[]{'/'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                int start = 0;
                if (parts[0].Equals("Assets", StringComparison.OrdinalIgnoreCase)) start = 1;

                var parent = root;
                for (int i = start; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (string.IsNullOrEmpty(part)) continue;
                    bool isFile = LooksLikeFile(part);
                    var existing = parent.children.FirstOrDefault(c => c.name == part);
                    if (existing == null)
                    {
                        existing = new FolderNode { name = part, isFile = isFile };
                        parent.children.Add(existing);
                    }
                    parent = existing;
                }
            }
            return root;
        }

        public static IEnumerable<(string path, bool isFile)> Flatten(FolderNode root)
        {
            var list = new List<(string, bool)>();
            void Recurse(FolderNode node, string current)
            {
                var here = string.IsNullOrEmpty(current) ? node.name : $"{current}/{node.name}";
                if (!here.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                    list.Add((here, node.isFile));
                foreach (var c in node.children) Recurse(c, here);
            }
            Recurse(root, "");
            return list;
        }
    }
}
