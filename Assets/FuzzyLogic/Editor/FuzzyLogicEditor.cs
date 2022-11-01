#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace FuzzyLogicSystem.Editor
{
    public class FuzzyLogicEditor : EditorWindow
    {
        public const string PERSISTENT_DATA_PATH = "Assets/FuzzyLogic/Data/";

        public const string DEFAULT = "default.bytes";

        public const string DEFAULT_GUID = "defaultGUID";

        [MenuItem("Window/Fuzzy Logic Editor")]
        private static void OpenFizzyLogicEditor()
        {
            var window = EditorWindow.GetWindow<FuzzyLogicEditor>();
            window.titleContent = new GUIContent("Fuzzy Logic");
            window.Show();
        }

        #region All FuzzyLogicEditor Windows

        private static List<FuzzyLogicEditor> allFuzzyLogicEditors = new List<FuzzyLogicEditor>();

        private static void AddFuzzyLogicEditor(FuzzyLogicEditor fuzzyLogicEditor)
        {
            allFuzzyLogicEditors.Add(fuzzyLogicEditor);
        }

        private static void RemoveFuzzyLogicEditor(FuzzyLogicEditor fuzzyLogicEditor)
        {
            allFuzzyLogicEditors.Remove(fuzzyLogicEditor);
        }

        private static int NumberFuzzyLogicEditors()
        {
            return allFuzzyLogicEditors.Count;
        }

        #endregion

        private UndoStack _undoStack = new UndoStack();
        public UndoStack undoStack
        {
            get
            {
                return _undoStack;
            }
        }

        private string _fuzzyLogicGUID = null;
        private string fuzzyLogicGUID
        {
            set
            {
                _fuzzyLogicGUID = value;
            }
            get
            {
                return _fuzzyLogicGUID;
            }
        }

        public FuzzyLogic fuzzyLogic
        {
            set
            {
                fuzzyLogicGUID = value.guid;
            }
            get
            {
                var fuzzyLogic = FuzzyLogic.GetRegisteredFuzzyLogic(fuzzyLogicGUID);
                GUIUtils.Get(fuzzyLogic).editorWindow = this;
                return fuzzyLogic;
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
            RegisterAllFuzzyLogicsOnDisk();

            fuzzyLogicGUID = DEFAULT_GUID;

            AddFuzzyLogicEditor(this);
        }

        private void OnDisable()
        {
            DeleteTempShortcutsProfile();
            RemoveFuzzyLogicEditor(this);

            if (NumberFuzzyLogicEditors() == 0)
            {
                FuzzyLogic.UnregisterAllFuzzyLogics();
            }
        }

        private void OnGUI()
        {
            UndoRedo();

            GUIUtils.BeginBox();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    OnGUI_RefreshAllFuzzyLogics();
                    OnGUI_AllFuzzyLogicsList();

                    bool fuzzyLogicIsChanged = GUIUtils.Get(fuzzyLogic).isChanged;
                    GUI.color = fuzzyLogicIsChanged ? Color.green : Color.white;
                    bool saveButton = GUILayout.Button("Save" + (fuzzyLogicIsChanged ? "*" : string.Empty), GUILayout.Width(80));
                    GUI.color = Color.white;
                    if (saveButton)
                    {
                        ForEachFuzzyLogicsOnDisk((i_fuzzyLogic, filePath) =>
                        {
                            if (i_fuzzyLogic.guid == fuzzyLogic.guid)
                            {
                                File.WriteAllBytes(filePath, FuzzyLogic.Serialize(fuzzyLogic));
                                GUIUtils.Get(fuzzyLogic).isChanged = false;
                                GUIUtils.Get(fuzzyLogic).editorWindow.undoStack.Empty();
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        });
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

                    {
                        GUILayout.Space(10);
                        GUILayout.Label("|");
                        GUILayout.Space(10);

                        fuzzyLogic.evaluate = GUILayout.Toggle(fuzzyLogic.evaluate, "Evaluate");

                        GUILayout.Space(10);
                        GUILayout.Label("|");
                        GUILayout.Space(10);

                        GUILayout.BeginHorizontal(GUILayout.Width(150));
                        {
                            GUILayout.Label("Name");
                            GUIUtils.TextField(fuzzyLogic, fuzzyLogic.name, o => fuzzyLogic.name = o);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();

            GUIUtils.Get(fuzzyLogic).Draw();

            Repaint();
        }

        #region FuzzyLogic data stored on disk

        private void RegisterAllFuzzyLogicsOnDisk()
        {
            ForEachFuzzyLogicsOnDisk((fuzzyLogic, filePath) =>
            {
                // Register it
                if (FuzzyLogic.QueryFuzzyLogic(fuzzyLogic.guid, out var _) == false)
                {
                    FuzzyLogic.RegisterFuzzyLogic(fuzzyLogic);

                    var popupMenuItemPath = filePath.Substring(PERSISTENT_DATA_PATH.Length);
                    var separatorIndex = popupMenuItemPath.IndexOf("/");
                    if (separatorIndex != -1)
                    {
                        GUIUtils.Get(fuzzyLogic).popupMenuItemPath = popupMenuItemPath.Substring(0, separatorIndex);
                    }
                    else
                    {
                        GUIUtils.Get(fuzzyLogic).popupMenuItemPath = string.Empty;
                    }
                }

                return true;
            });
        }

        private void ForEachFuzzyLogicsOnDisk(Func<FuzzyLogic, string/*filePath*/, bool/*go on iterating*/> action)
        {
            // Read all fuzzylogic files on disk
            var allFiles = Directory.GetFiles(PERSISTENT_DATA_PATH, "*.bytes", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var bytes = File.ReadAllBytes(file);
                if (FuzzyLogic.ValidateHeader(bytes))
                {
                    FuzzyLogic fuzzyLogic = FuzzyLogic.Deserialize(bytes, null);
                    if (action != null)
                    {
                        if (action(fuzzyLogic, file) == false)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log("Invalid header, Parse fuzzyLogic data fail. " + file);
                }
            }
        }

        #endregion

        #region EditorGUI of all fuzzyLogics list

        private FlsList<string> allFuzzyLogicsNames = new FlsList<string>();

        private FlsList<string> allFuzzyLogicsGUIDs = new FlsList<string>();

        private GUIStyle _refreshButtonStyle;
        private GUIStyle refreshButtonStyle
        {
            get
            {
                if (_refreshButtonStyle == null)
                {
                    _refreshButtonStyle = new GUIStyle();
                    _refreshButtonStyle.normal.background = EditorGUIUtility.FindTexture("d_RotateTool On");
                    _refreshButtonStyle.hover.background = EditorGUIUtility.FindTexture("d_RotateTool");
                }
                return _refreshButtonStyle;
            }
        }

        private void OnGUI_RefreshAllFuzzyLogics()
        {
            if(GUILayout.Button(new GUIContent(string.Empty, "Reload all FuzzyLogics from disk.\nChanges not saved will be lost.\nIt's useful when you modify(rename, delete or move etc.) saved data in folders."), refreshButtonStyle, GUILayout.Width(18), GUILayout.Height(18)))
            {
                
                bool doSave = true;
                if (GUIUtils.Get(fuzzyLogic).isChanged)
                {
                    if (EditorUtility.DisplayDialog("Message", "Some changes are not saved, would you still like to reload?\nChanges not saved will be lost.", "Yes", "Cancel") == false)
                    {
                        doSave = false;
                    }
                }
                if (doSave)
                {
                    for (int i = 0; i < FuzzyLogic.NumberRegisteredFuzzyLogics(); i++)
                    {
                        var fuzzyLogic = FuzzyLogic.GetRegisteredFuzzyLogic(i);
                        if (GUIUtils.Get(fuzzyLogic).editorWindow && GUIUtils.Get(fuzzyLogic).editorWindow.undoStack != null)
                        {
                            GUIUtils.Get(fuzzyLogic).editorWindow.undoStack.Empty();
                        }
                    }
                    FuzzyLogic.UnregisterAllFuzzyLogics();
                    RegisterAllFuzzyLogicsOnDisk();
                }
            }
            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    GUIUtils.GUILoseFocus();
                }
            }
        }

        private void OnGUI_AllFuzzyLogicsList()
        {
            allFuzzyLogicsNames.Clear();
            allFuzzyLogicsGUIDs.Clear();

            for (int i = 0; i < FuzzyLogic.NumberRegisteredFuzzyLogics(); i++)
            {
                var fuzzyLogic = FuzzyLogic.GetRegisteredFuzzyLogic(i);
                if (string.IsNullOrWhiteSpace(fuzzyLogic.name) == false)
                {
                    string popupMenuItemPath = GUIUtils.Get(fuzzyLogic).popupMenuItemPath;
                    allFuzzyLogicsNames.Add(string.IsNullOrEmpty(popupMenuItemPath) ? fuzzyLogic.name : popupMenuItemPath + "/" + fuzzyLogic.name);
                }
                else
                {
                    allFuzzyLogicsNames.Add("Unnamed/" + i);
                }
                allFuzzyLogicsGUIDs.Add(fuzzyLogic.guid);
            }

            allFuzzyLogicsNames.Sort(
                (a, b) =>
                {
                    return a.CompareTo(b);
                },
                allFuzzyLogicsGUIDs
            );

            int selectedIndex = Mathf.Max(allFuzzyLogicsGUIDs.IndexOf(fuzzyLogicGUID), 0);
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, allFuzzyLogicsNames.ToArray());
            if (selectedIndex != newSelectedIndex)
            {
                GUIUtils.GUILoseFocus();
                if (GUIUtils.Get(fuzzyLogic).isChanged)
                {
                    if(EditorUtility.DisplayDialog("Message", "Some changes are not saved, would you still like to close it?\nChanges not saved will be lost.", "Yes", "Cancel"))
                    {
                        ForEachFuzzyLogicsOnDisk((i_fuzzyLogic, filePath)=>
                        {
                            if (fuzzyLogic.guid == i_fuzzyLogic.guid)
                            { 
                                GUIUtils.Get(fuzzyLogic).isChanged = false;
                                GUIUtils.Get(fuzzyLogic).editorWindow.undoStack.Empty();
                                FuzzyLogic.Deserialize(FuzzyLogic.Serialize(i_fuzzyLogic), fuzzyLogic);
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        });
                    }
                    else
                    {
                        newSelectedIndex = selectedIndex;
                    }
                }
            }
            fuzzyLogicGUID = allFuzzyLogicsGUIDs[newSelectedIndex];
        }

        #endregion

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