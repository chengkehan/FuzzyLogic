#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class FuzzyLogicEditor : EditorWindow
    {
        [MenuItem("Window/Fuzzy Logic Editor")]
        private static void OpenFizzyLogicEditor()
        {
            var window = EditorWindow.CreateWindow<FuzzyLogicEditor>();
            window.titleContent = new GUIContent("Fuzzy Logic");
            window.Show();
        }

        private FuzzyLogic fuzzyLogic = null;

        private void OnEnable()
        {
            fuzzyLogic = new FuzzyLogic();
            fuzzyLogic.Initialize();
            InitializeEditorWindow();
        }

        private void OnGUI()
        {
            GUIUtils.BeginBox();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save", GUILayout.Width(80)))
                    {
                        string savePath = EditorUtility.SaveFilePanel("Save", string.Empty, string.Empty, "bytes");
                        if (string.IsNullOrEmpty(savePath) == false)
                        {
                            byte[] bytes = FuzzyLogic.Serialize(fuzzyLogic);
                            File.WriteAllBytes(savePath, bytes);
                        }
                    }
                    if (GUILayout.Button("Load", GUILayout.Width(80)))
                    {
                        string loadPath = EditorUtility.OpenFilePanel(string.Empty, string.Empty, "bytes");
                        if (string.IsNullOrEmpty(loadPath) == false)
                        {
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
                        fuzzyLogic.AddFuzzification();
                    }
                    if (GUILayout.Button("Add Inference", GUILayout.Width(120)))
                    {
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

            if (Event.current.control)
            {
                Repaint();
            }
        }

        private void InitializeEditorWindow()
        {
            if (fuzzyLogic != null)
            {
                GUIUtils.Get(fuzzyLogic).editorWindow = this;
            }
        }
    }
}
#endif