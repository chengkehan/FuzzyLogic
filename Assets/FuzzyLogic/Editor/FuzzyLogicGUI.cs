#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class FuzzyLogicGUI : IGUI
    {
        private HighlightGUI _highlight = null;
        public HighlightGUI highlight
        {
            get
            {
                if (_highlight == null)
                {
                    _highlight = new HighlightGUI();
                }
                return _highlight;
            }
        }

        private FuzzyLogicEditor _editorWindow = null;
        public FuzzyLogicEditor editorWindow
        {
            set
            {
                _editorWindow = value;
            }
            get
            {
                return _editorWindow;
            }
        }

        private Vector2 scrollFuzzifications = Vector2.zero;

        private Vector2 scrollInferences = Vector2.zero;

        private FuzzyLogic fuzzyLogic = null;

        public FuzzyLogicGUI(FuzzyLogic fuzzyLogic)
        {
            this.fuzzyLogic = fuzzyLogic;
        }

        public void ShowNotification(string msg)
        {
            if (editorWindow != null)
            {
                editorWindow.ShowNotification(new GUIContent(msg));
            }
        }

        public void Draw()
        {
            EditorGUILayout.BeginVertical();
            {
                scrollFuzzifications = EditorGUILayout.BeginScrollView(scrollFuzzifications, GUILayout.Height(FuzzificationGUI.AREA_HEIGHT + 15));
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        int numFuzzifications = fuzzyLogic.NumberFuzzifications();
                        for (int i = 0; i < numFuzzifications; i++)
                        {
                            GUIUtils.Get(fuzzyLogic.GetFuzzification(i)).Draw();

                            // A fuzzification was deleted. Iterator changed, so break it.
                            if (numFuzzifications != fuzzyLogic.NumberFuzzifications())
                            {
                                break;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                {
                    GUIUtils.Get(fuzzyLogic.defuzzification).Draw();

                    EditorGUILayout.BeginVertical();
                    {
                        scrollInferences = GUILayout.BeginScrollView(scrollInferences);
                        {
                            int numInferences = fuzzyLogic.NumberInferences();
                            for (int i = 0; i < numInferences; i++)
                            {
                                GUIUtils.Get(fuzzyLogic.GetInference(i)).Draw();

                                // An inference was deleted. Iterator changed, so break it.
                                if (numInferences != fuzzyLogic.NumberInferences())
                                {
                                    break;
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

            }
            EditorGUILayout.EndVertical();

            fuzzyLogic.Update();
        }
    }
}
#endif