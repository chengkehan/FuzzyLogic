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

        public const string DEFAULT_GUID = "e464ddbf-d6f2-468c-a7d5-7e81a720865c";

        [MenuItem("Window/Fuzzy Logic/Editor")]
        private static void OpenFizzyLogicEditor()
        {
            var window = EditorWindow.CreateInstance<FuzzyLogicEditor>();
            var title = new GUIContent();
            title.text = "Fuzzy Logic";
            title.image = EditorGUIUtility.FindTexture("_Popup");
            window.titleContent = title;
            window.Show();
        }

        #region Generate new GUID

        [MenuItem("Window/Fuzzy Logic/New GUID")]
        private static void NewGUID()
        {
            var objs = Selection.objects;
            foreach (var obj in objs)
            {
                if (obj is TextAsset && FuzzyLogic.ValidateHeader((obj as TextAsset).bytes))
                {
                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    var fuzzyLogic = FuzzyLogic.Deserialize((obj as TextAsset).bytes, null);
                    fuzzyLogic.guid = Guid.NewGuid().ToString();
                    var bytes = FuzzyLogic.Serialize(fuzzyLogic);
                    File.WriteAllBytes(assetPath, bytes);
                    AssetDatabase.ImportAsset(assetPath);

                    Debug.Log("Set new guid successfully, " + assetPath);
                }
            }
        }

        [MenuItem("Window/Fuzzy Logic/New GUID", true)]
        private static bool NewGUID_Validation()
        {
            var objs = Selection.objects;
            foreach (var obj in objs)
            {
                if (obj is TextAsset && FuzzyLogic.ValidateHeader((obj as TextAsset).bytes))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

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

        private static FuzzyLogicEditor GetFuzzyLogicEditor(int index)
        {
            if (index < 0 || index >= allFuzzyLogicEditors.Count)
            {
                return null;
            }
            else
            {
                return allFuzzyLogicEditors[index];
            }
        }

        private static void BringAllFuzzyLogicsToFront()
        {
            for (int i = 0; i < NumberFuzzyLogicEditors(); i++)
            {
                GetFuzzyLogicEditor(i).Focus();
            }
        }

        #endregion

        #region GUI Width and Height of Fuzzification and Defuzzification

        // We store gui settings here rather than GUI classes so that user can edit one FuzzyLogic in multi-windows.

        private float _fuzzificationGUIHeight = FuzzificationGUI.MIN_GUI_HEIGHT;
        public float fuzzificationGUIHeight
        {
            set
            {
                _fuzzificationGUIHeight = value;
            }
            get
            {
                return _fuzzificationGUIHeight;
            }
        }

        private float _defuzzificationGUIWidth = FuzzificationGUI.MIN_GUI_WIDTH * 2;
        public float defuzzificationGUIWidth
        {
            set
            {
                _defuzzificationGUIWidth = value;
            }
            get
            {
                return _defuzzificationGUIWidth;
            }
        }

        private Vector2 _scrollFuzzifications = Vector2.zero;
        public Vector2 scrollFuzzifications
        {
            set
            {
                _scrollFuzzifications = value;
            }
            get
            {
                return _scrollFuzzifications;
            }
        }

        private Vector2 _scrollInferences = Vector2.zero;
        public Vector2 scrollInferences
        {
            set
            {
                _scrollInferences = value;
            }
            get
            {
                return _scrollInferences;
            }
        }

        #endregion

        #region focused window by target guid

        // When set forcused target, we can resize window to fit size of gui, now we cache size of window at here.
        // And restore size of window after remove focused target.
        private Rect windowRectForRestoring;

        // Set as guid of fuzzification or defuzzification,
        // then corresponding gui will be drawed solo so that user can focus on this gui and not to be disturbed by others.
        private string _focusedTargetGUID = null;
        public string focusedTargetGUID
        {
            set
            {
                _focusedTargetGUID = value;
                if (_focusedTargetGUID != null)
                {
                    windowRectForRestoring = position;
                }
                else
                {
                    // Don't restore size of window when it's docked,
                    // otherwise it will break layout of docked windows.
                    if (docked == false)
                    {
                        Rect winRect = position;
                        winRect.width = windowRectForRestoring.width;
                        winRect.height = windowRectForRestoring.height;
                        position = winRect;
                    }
                }
            }
            get
            {
                return _focusedTargetGUID;
            }
        }

        public void SetFocusedTargetGUID(string guid, float newWindowWidth, float newWindowHeight)
        {
            focusedTargetGUID = guid;

            if (focusedTargetGUID != null && docked == false)
            {
                Rect windowRect = GUIUtils.Get(fuzzyLogic).editorWindow.position;
                windowRect.width = newWindowWidth;
                windowRect.height = newWindowHeight;
                GUIUtils.Get(fuzzyLogic).editorWindow.position = windowRect;
            }
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
                DeleteTempShortcutsProfile();
            }
        }

        private void OnGUI()
        {
            UndoRedo();

            if (focusedTargetGUID == null)
            {
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
                                    AssetDatabase.ImportAsset(filePath);
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

                            GUILayout.Space(10);
                            GUILayout.Label("|");
                            GUILayout.Space(10);

                            GUILayout.Label(new GUIContent(fuzzyLogic.guid, "GUID"));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(new GUIContent("@" + NumberFuzzyLogicEditors(), "Bring All to Front.\nThis number indicates the number of opened FuzzyLogic Windows."), GUILayout.ExpandWidth(true)))
                            {
                                BringAllFuzzyLogicsToFront();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUIUtils.EndBox();
            }

            GUIUtils.Get(fuzzyLogic).Draw();

            Repaint();
        }

        #region FuzzyLogic data stored on disk

        private void RegisterAllFuzzyLogicsOnDisk()
        {
            ForEachFuzzyLogicsOnDisk((fuzzyLogic, filePath) =>
            {
                // Register it
                if (FuzzyLogic.IsRegisteredFuzzyLogic(fuzzyLogic.guid) == false)
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

                    GUIUtils.Get(fuzzyLogic).ShowNotification("Reload Complete");
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
                string popupMenuItemPath = GUIUtils.Get(fuzzyLogic).popupMenuItemPath;
                string fuzzyLogicName = string.IsNullOrWhiteSpace(fuzzyLogic.name) ? fuzzyLogic.guid : fuzzyLogic.name;
                allFuzzyLogicsNames.Add(string.IsNullOrEmpty(popupMenuItemPath) ? fuzzyLogicName : popupMenuItemPath + "/" + fuzzyLogicName);
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