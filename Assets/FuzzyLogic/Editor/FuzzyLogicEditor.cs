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
            var window = EditorWindow.GetWindow<FuzzyLogicEditor>();
            window.titleContent = new GUIContent("Fuzzy Logic");
            window.Show();
        }

        private FuzzyLogic fuzzyLogic = null;

        private void OnEnable()
        {
            GUIUtils.fuzzyLogicEditor = this;

            fuzzyLogic = new FuzzyLogic();
            fuzzyLogic.Initialize();
        }

        private void OnDisable()
        {
            GUIUtils.fuzzyLogicEditor = null;
        }

        private void OnGUI()
        {
            GUIUtils.BeginBox();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save", GUILayout.Width(80)))
                    {
                        string savePath = EditorUtility.SaveFilePanel("Save", string.Empty, string.Empty, "json");
                        if (string.IsNullOrEmpty(savePath) == false)
                        {
                            string json = JsonUtility.ToJson(fuzzyLogic, true);
                            File.WriteAllText(savePath, json);
                        }
                    }
                    if (GUILayout.Button("Load", GUILayout.Width(80)))
                    {
                        string loadPath = EditorUtility.OpenFilePanel(string.Empty, string.Empty, "json");
                        if (string.IsNullOrEmpty(loadPath) == false)
                        {
                            string json = File.ReadAllText(loadPath);
                            fuzzyLogic = JsonUtility.FromJson<FuzzyLogic>(json);
                            fuzzyLogic.Initialize();
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

            GUI_FuzzyLogic.Draw(fuzzyLogic);

            if (Event.current.control)
            {
                Repaint();
            }
        }
    }
}
#endif