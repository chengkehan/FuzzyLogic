#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class GUI_FuzzyLogic
    {
        private static Vector2 scrollFuzzifications = Vector2.zero;

        private static Vector2 scrollInferences = Vector2.zero;

        public static void Draw(FuzzyLogic fuzzyLogic)
        {
            EditorGUILayout.BeginVertical();
            {
                scrollFuzzifications = EditorGUILayout.BeginScrollView(scrollFuzzifications, GUILayout.Height(GUI_Fuzzification.AREA_HEIGHT + 15));
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        int numFuzzifications = fuzzyLogic.NumberFuzzifications();
                        for (int i = 0; i < numFuzzifications; i++)
                        {
                            GUI_Fuzzification.Draw(fuzzyLogic.GetFuzzification(i));

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
                    GUI_Defuzzification.Draw(fuzzyLogic.defuzzification);

                    EditorGUILayout.BeginVertical();
                    {
                        scrollInferences = GUILayout.BeginScrollView(scrollInferences);
                        {
                            int numInferences = fuzzyLogic.NumberInferences();
                            for (int i = 0; i < numInferences; i++)
                            {
                                GUI_Inference.Draw(fuzzyLogic.GetInference(i));

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