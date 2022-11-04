using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class GUIUtils
    {
        public static FuzzyLogicGUI Get(FuzzyLogic fuzzyLogic)
        {
            if (fuzzyLogic.gui == null)
            {
                fuzzyLogic.gui = new FuzzyLogicGUI(fuzzyLogic);
            }
            return fuzzyLogic.gui as FuzzyLogicGUI;
        }

        public static FuzzificationGUI Get(Fuzzification fuzzification)
        {
            if (fuzzification.gui == null)
            {
                fuzzification.gui = new FuzzificationGUI(fuzzification);
            }
            return fuzzification.gui as FuzzificationGUI;
        }

        public static DefuzzificationGUI Get(Defuzzification defuzzification)
        {
            if (defuzzification.gui == null)
            {
                defuzzification.gui = new DefuzzificationGUI(defuzzification);
            }
            return defuzzification.gui as DefuzzificationGUI;
        }

        public static InferenceGUI Get(Inference inference)
        {
            if (inference.gui == null)
            {
                inference.gui = new InferenceGUI(inference);
            }
            return inference.gui as InferenceGUI;
        }

        private static Material _glMaterial = null;
        public static Material glMaterial
        {
            get
            {
                if (_glMaterial == null)
                {
                    _glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                }
                return _glMaterial;
            }
        }

        public static void GUILoseFocus()
        {
            GUI.FocusControl(null);
        }

        public static void TextField(FuzzyLogic fuzzyLogic, string text, Action<string> setter, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            string newText = EditorGUILayout.DelayedTextField(text, options);
            if (EditorGUI.EndChangeCheck())
            {
                GUIUtils.GUILoseFocus();
                UndoStackRecord(fuzzyLogic);
                setter(newText);
            }
        }

        public static void UIntField(FuzzyLogic fuzzyLogic, int value, Action<int> setter, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            int newValue = Mathf.Abs(EditorGUILayout.DelayedIntField(value, options));
            if (EditorGUI.EndChangeCheck())
            {
                GUIUtils.GUILoseFocus();
                UndoStackRecord(fuzzyLogic);
                setter(newValue);
            }
        }

        public static void FloatField(FuzzyLogic fuzzyLogic, float value, Action<float> setter, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.DelayedFloatField(value, options);
            if (EditorGUI.EndChangeCheck())
            {
                GUIUtils.GUILoseFocus();
                UndoStackRecord(fuzzyLogic);
                setter(newValue);
            }
        }

        private static bool intSliderMouseDown = false;
        public static void IntSlider(FuzzyLogic fuzzyLogic, int value, int leftValue, int rightValue, Action<int> setter, params GUILayoutOption[] options)
        {
            if (intSliderMouseDown == false)
            {
                intSliderMouseDown = Event.current.type == EventType.MouseDown;
            }
            
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntSlider(value, leftValue, rightValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (intSliderMouseDown)
                {
                    UndoStackRecord(fuzzyLogic);
                }
                
                setter(newValue);

                if (intSliderMouseDown)
                {
                    intSliderMouseDown = false;
                }
            }
        }

        private static bool sliderMouseDown = false;
        public static void Slider(FuzzyLogic fuzzyLogic, float value, float leftValue, float rightValue, Action<float> setter, params GUILayoutOption[] options)
        {
            if (sliderMouseDown == false)
            {
                sliderMouseDown = Event.current.type == EventType.MouseDown;
            }

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(value, leftValue, rightValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (sliderMouseDown)
                {
                    UndoStackRecord(fuzzyLogic);
                }

                setter(newValue);

                if (sliderMouseDown)
                {
                    sliderMouseDown = false;
                }
            }
        }

        private static bool minMaxSliderMouseDown = false;
        public static void MinMaxSlider(FuzzyLogic fuzzyLogic, float minValue, float maxValue, float minLimit, float maxLimit, Action<float> setterMinValue, Action<float> setterMaxValue, params GUILayoutOption[] options)
        {
            if (minMaxSliderMouseDown == false)
            {
                minMaxSliderMouseDown = Event.current.type == EventType.MouseDown;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (minMaxSliderMouseDown)
                {
                    UndoStackRecord(fuzzyLogic);
                }

                setterMinValue(minValue);
                setterMaxValue(maxValue);

                if (minMaxSliderMouseDown)
                {
                    minMaxSliderMouseDown = false;
                }
            }
        }

        public static void Popup(FuzzyLogic fuzzyLogic, int selectedIndex, string[] labels, Action<int> setter, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, labels, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSelectedIndex != selectedIndex)
                {
                    UndoStackRecord(fuzzyLogic);
                }
            }
            setter(newSelectedIndex);
        }

        public static void EnumPopup<T>(FuzzyLogic fuzzyLogic, T selected, Action<T> setter) where T : Enum
        {
            EditorGUI.BeginChangeCheck();
            T newSelected = (T)EditorGUILayout.EnumPopup(selected);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSelected.Equals(selected) == false)
                {
                    UndoStackRecord(fuzzyLogic);
                }
            }
            setter(newSelected);
        }

        public static void UndoStackRecord(FuzzyLogic fuzzyLogic)
        {
            GUIUtils.Get(fuzzyLogic).editorWindow.undoStack.Record(fuzzyLogic);
        }

        public static void BeginBox(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"), options);
        }

        public static void EndBox()
        {
            EditorGUILayout.EndVertical();
        }

        public static void BeginHBox(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.GetStyle("Box"), options);
        }

        public static void EndHBox()
        {
            EditorGUILayout.EndHorizontal();
        }

        public static void GLDrawShape(int mode, Action function)
        {
            GL.Begin(mode);
            GL.PushMatrix();

            function.Invoke();

            GL.End();
            GL.PopMatrix();
        }

        public static void GLVector2(Vector2 v2)
        {
            GL.Vertex3(v2.x, v2.y, 0);
        }

        public static void GLFloat2(float x, float y)
        {
            GL.Vertex3(x, y, 0);
        }

        public static void GLDrawDashLine(Color color, Vector2 p1, Vector2 p2)
        {
            GLDrawShape(GL.LINES, () =>
            {
                GL.Color(color);

                Vector2 p = p1;
                Vector2 dir = (p2 - p1).normalized;
                float step = 5;
                float gap = 5;
                while (true)
                {
                    GLVector2(p);
                    if (Vector2.Distance(p, p2) < step + gap)
                    {
                        GLVector2(p2);
                        break;
                    }
                    else
                    {
                        Vector2 nextP = p + step * dir;
                        GLVector2(nextP);
                        p = nextP + gap * dir;
                    }
                }
            });
        }

        public static void GLDrawDot(Color color, Vector2 p)
        {
            GLDrawShape(GL.QUADS, () =>
            {
                GL.Color(color);

                float size = 3;
                GLVector2(new Vector2(p.x - size, p.y - size));
                GLVector2(new Vector2(p.x - size, p.y + size));
                GLVector2(new Vector2(p.x + size, p.y + size));
                GLVector2(new Vector2(p.x + size, p.y - size));
            });
        }
    }
}
