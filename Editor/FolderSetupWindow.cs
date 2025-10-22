using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace jorobledodu.folderSetup
{
    public class FolderSetupWindow : EditorWindow
    {
        [MenuItem("Tools/Folder Setup Wizard")]
        public static void Open() => GetWindow<FolderSetupWindow>("Folder Setup");

        [SerializeField, TextArea(10, 30)]
        private string _structureText = DefaultStructure;

        private Vector2 _scroll;
        private bool _createGitkeep = true;

        // Deferred actions flags
        private bool _doLoadSample;
        private bool _doCreate;
        private bool _doDelete;

        const string DefaultStructure =
            @"Assets
- Art
- Materials
- Scenes
    - 00_gym.unity
- Prefabs
- Scripts";

        void OnGUI()
        {
            // Defensive
            if (_structureText == null)
                _structureText = string.Empty;

            GUILayout.Label("Folder Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Write folders easily.", MessageType.Info);

            _createGitkeep = EditorGUILayout.ToggleLeft(
                "Create .gitkeep in empty folders",
                _createGitkeep
            );

            // Row: Load Sample / Clear  (no returns inside)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Sample"))
                _doLoadSample = true;
            if (GUILayout.Button("Clear"))
                _structureText = string.Empty;
            EditorGUILayout.EndHorizontal();

            // Scroll area
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _structureText = EditorGUILayout.TextArea(
                _structureText,
                GUILayout.MinHeight(180) // no ExpandHeight inside scroll
            );
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            // Row: Create / Delete  (defer actions)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Folders & Files", GUILayout.Height(30)))
                _doCreate = true;

            if (GUILayout.Button("Delete Folders", GUILayout.Height(30)))
                _doDelete = true;
            EditorGUILayout.EndHorizontal();

            // ---- Execute deferred actions AFTER all layout groups are closed ----
            if (_doLoadSample)
            {
                _doLoadSample = false;
                SafeLoadFromFile();
                Repaint();
            }
            if (_doCreate)
            {
                _doCreate = false;
                SafeTryCreate();
            }
            if (_doDelete)
            {
                _doDelete = false;
                SafeTryDelete();
            }
        }

        // ---- Safe wrappers (catch exceptions so layout never breaks) ----

        void SafeTryCreate()
        {
            try
            {
                var root = FolderStructureParser.Parse(_structureText);
                var flat = FolderStructureParser
                    .Flatten(root)
                    .Where(e => e.path.StartsWith("Assets/"));
                if (!flat.Any())
                {
                    EditorUtility.DisplayDialog(
                        "No entries",
                        "No 'Assets/' paths found. Ensure your structure starts at Assets.",
                        "OK"
                    );
                    return;
                }
                FolderOps.CreateAll(flat, _createGitkeep);
                EditorUtility.DisplayDialog("Done", "Folders/files created successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        void SafeTryDelete()
        {
            try
            {
                // First confirmation (outside of any layout group)
                bool proceed = EditorUtility.DisplayDialog(
                    "Delete folders",
                    "This will delete the folders referenced in the structure under 'Assets/'. Are you sure?",
                    "I want to delete these folders",
                    "Cancel"
                );
                if (!proceed)
                    return;

                var root = FolderStructureParser.Parse(_structureText);
                var flat = FolderStructureParser
                    .Flatten(root)
                    .Where(e => e.path.StartsWith("Assets/"));
                var folders = flat.Where(e => !e.isFile).Select(e => e.path).Distinct().ToList();
                if (folders.Count == 0)
                {
                    EditorUtility.DisplayDialog(
                        "No folders",
                        "No folders found under 'Assets/'.",
                        "OK"
                    );
                    return;
                }

                bool contentDetected = folders.Any(FolderOps.FolderHasContent);
                bool deleteContent = false;
                if (contentDetected)
                {
                    int choice = EditorUtility.DisplayDialogComplex(
                        "Content has been detected in the folders",
                        "Some folders contain files. How do you want to proceed?",
                        "Delete content", // 0
                        "Do not delete content", // 1
                        "Cancel" // 2
                    );
                    if (choice == 2)
                        return;
                    deleteContent = (choice == 0);
                }

                FolderOps.DeleteFolders(folders, deleteContent);
                EditorUtility.DisplayDialog("Done", "Deletion finished.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        void SafeLoadFromFile()
        {
            try
            {
#if UNITY_2020_1_OR_NEWER
                string path = EditorUtility.OpenFilePanelWithFilters(
                    "Select structure file (.txt or .json)",
                    Application.dataPath,
                    new string[] { "Text or JSON", "txt,json", "All files", "*" }
                );
#else
                string path = EditorUtility.OpenFilePanel(
                    "Select structure file (.txt or .json)",
                    Application.dataPath,
                    ""
                );
#endif
                if (string.IsNullOrEmpty(path))
                    return;

                var text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    EditorUtility.DisplayDialog("Empty file", "The selected file is empty.", "OK");
                    return;
                }

                _structureText = text;
                ShowNotification(new GUIContent("Sample loaded"));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorUtility.DisplayDialog(
                    "Error",
                    "Could not read the file:\n" + ex.Message,
                    "OK"
                );
            }
        }
    }
}
