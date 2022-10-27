#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace FuzzyLogicSystem.Editor
{
    public class FuzzyLogicEditor : EditorWindow
    {
        [MenuItem("Window/Fuzzy Logic Editor")]
        private static void OpenFizzyLogicEditor()
        {
            var window = EditorWindow.GetWindow<FuzzyLogicEditor>();
            window.titleContent = new GUIContent("Fuzzy Logic");
            window.Show();
        }

        private UndoStack _undoStack = new UndoStack();
        public UndoStack undoStack
        {
            get
            {
                return _undoStack;
            }
        }

        private FuzzyLogic _fuzzyLogic = null;
        public FuzzyLogic fuzzyLogic
        {
            set
            {
                _fuzzyLogic = value;
                InitializeEditorWindow();
            }
            get
            {
                return _fuzzyLogic;
            }
        }

        private bool _focused = false;
        public bool focused
        {
            private set
            {
                _focused = value;
            }
            get
            {
                return _focused;
            }
        }

        private void OnEnable()
        {
            var guids = AssetDatabase.FindAssets("t:Script FuzzyLogicEditor");
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var dir = new FileInfo(path).Directory;
            path = dir.FullName + "/default.bytes";
            path = FileUtil.GetProjectRelativePath(path);
            var bytes = File.ReadAllBytes(path);

            fuzzyLogic = FuzzyLogic.Deserialize(bytes);
            fuzzyLogic.Initialize();
            InitializeEditorWindow();
        }

        private void OnDisable()
        {  
           DeleteTempShortcutsProfile(); 
        }

        private void OnGUI()
        {
            UndoRedo();

            GUIUtils.BeginBox();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save", GUILayout.Width(80)))
                    {
                        string savePath = EditorUtility.SaveFilePanel("Save", string.Empty, string.Empty, "bytes");
                        if (string.IsNullOrEmpty(savePath) == false)
                        {
                            GUIUtils.GUILoseFocus();
                            byte[] bytes = FuzzyLogic.Serialize(fuzzyLogic);
                            File.WriteAllBytes(savePath, bytes);
                        }
                    }
                    if (GUILayout.Button("Load", GUILayout.Width(80)))
                    {
                        string loadPath = EditorUtility.OpenFilePanel(string.Empty, string.Empty, "bytes");
                        if (string.IsNullOrEmpty(loadPath) == false)
                        {
                            GUIUtils.GUILoseFocus();
                            byte[] bytes = File.ReadAllBytes(loadPath);
                            if (FuzzyLogic.ValidateHeader(bytes))
                            {
                                fuzzyLogic = FuzzyLogic.Deserialize(bytes);
                                InitializeEditorWindow();
                            }
                            else
                            {
                                GUIUtils.Get(fuzzyLogic).ShowNotification("Invalid data");
                            }
                        }
                    }
                    if (GUILayout.Button("Add Fuzzification", GUILayout.Width(120)))
                    {
                        GUIUtils.GUILoseFocus();
                        GUIUtils.UndoStackRecord(fuzzyLogic);
                        fuzzyLogic.AddFuzzification();
                    }
                    if (GUILayout.Button("Add Inference", GUILayout.Width(120)))
                    {
                        GUIUtils.GUILoseFocus();
                        GUIUtils.UndoStackRecord(fuzzyLogic);
                        fuzzyLogic.AddInference(); 
                    }

                    GUIUtils.BeginBox(GUILayout.Width(100));
                    {
                        fuzzyLogic.updatingOutput = GUILayout.Toggle(fuzzyLogic.updatingOutput, "Updating Output");
                    }
                    GUIUtils.EndBox();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();

            GUIUtils.Get(fuzzyLogic).Draw();

            Repaint();
        }

        private void InitializeEditorWindow()
        {
            if (fuzzyLogic != null)
            {
                GUIUtils.Get(fuzzyLogic).editorWindow = this;
            }
        }

        private void OnFocus()
        {
            focused = true;
            RegisterTempShortcutsProfile();
        }

        private void OnLostFocus()
        {
            focused = false;
            DeleteTempShortcutsProfile();
        }

        #region Customize shortcuts for Undo/Redo

        private const string TEMP_SHORTCUTS_PROFILE_NAME = "FuzzyLogicTemp";

        // We cann't do undo/redo actions with a very high frequency.
        // This is not to limit users, but to avoid error in logic.
        // Because of OnGUI is invoked multi-times in one frame,
        // without this, when users press shortcuts of undo/redo once,
        // undo/redo actions will also be execute multi-times.
        // So we should add a gap between two undo/redo actions.
        private double undoRedoTime = 0;

        private void UndoRedo()
        {
            if (focused)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    if (Event.current.control || Event.current.command)
                    {
                        GUIUtils.GUILoseFocus();
                    }
                }

                if (Event.current.commandName == "SelectPrefabRoot" && Event.current.rawType == EventType.ValidateCommand)
                {
                    if (EditorApplication.timeSinceStartup - undoRedoTime > 0.2f)
                    {
                        GUIUtils.GUILoseFocus();
                        undoRedoTime = EditorApplication.timeSinceStartup;
                        undoStack.Undo(this);
                    }
                }
                if (Event.current.commandName == "SelectChildren" && Event.current.rawType == EventType.ValidateCommand)
                {
                    if (EditorApplication.timeSinceStartup - undoRedoTime > 0.2f)
                    {
                        GUIUtils.GUILoseFocus();
                        undoRedoTime = EditorApplication.timeSinceStartup;
                        undoStack.Redo(this);
                    }
                }
            }
        }

        private void RegisterTempShortcutsProfile()
        {
            DeleteTempShortcutsProfile();

            ShortcutManager.instance.CreateProfile(TEMP_SHORTCUTS_PROFILE_NAME);
            ShortcutManager.instance.activeProfileId = TEMP_SHORTCUTS_PROFILE_NAME;

            // Set other shortcuts for undo and redo
            {
                KeyCombination keyCombination = new KeyCombination(KeyCode.J, ShortcutModifiers.Action);
                ShortcutManager.instance.RebindShortcut("Main Menu/Edit/Undo", new ShortcutBinding(keyCombination));
            }
            {
                KeyCombination keyCombination = new KeyCombination(KeyCode.K, ShortcutModifiers.Action);
                ShortcutManager.instance.RebindShortcut("Main Menu/Edit/Redo", new ShortcutBinding(keyCombination));
            }

            // Register normal undo/redo shortcuts to actions that without side effect.
            // Otherwise, we will hear beep sound when press undo/redo shortcuts.
            {
                KeyCombination keyCombination = new KeyCombination(KeyCode.Z, ShortcutModifiers.Action);
                ShortcutManager.instance.RebindShortcut("Main Menu/Edit/Select Prefab Root", new ShortcutBinding(keyCombination));
            }
            {
                KeyCombination keyCombination = new KeyCombination(KeyCode.Z, ShortcutModifiers.Action | ShortcutModifiers.Shift);
                ShortcutManager.instance.RebindShortcut("Main Menu/Edit/Select Children", new ShortcutBinding(keyCombination));
            }
        }

        private void DeleteTempShortcutsProfile()
        {
            ShortcutManager.instance.activeProfileId = ShortcutManager.defaultProfileId;
            foreach (var item in ShortcutManager.instance.GetAvailableProfileIds())
            {
                if (item == TEMP_SHORTCUTS_PROFILE_NAME)
                {
                    ShortcutManager.instance.DeleteProfile(TEMP_SHORTCUTS_PROFILE_NAME);
                    break;
                }
            }
        }

        #endregion
    }
}
#endif