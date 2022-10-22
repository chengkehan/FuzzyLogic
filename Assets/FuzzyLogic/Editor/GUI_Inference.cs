#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class GUI_Inference : MonoBehaviour
    {
        private static FlsList<string> inputGuids = new FlsList<string>();

        private static FlsList<string> inputLabels = new FlsList<string>();

        public static void Draw(Inference inference)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    inference.name = EditorGUILayout.TextField(inference.name, GUILayout.Width(80));

                    if (GUILayout.Button("x", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog(string.Empty, "Are you sure to delete it?", "Yes", "Cancel"))
                        {
                            inference.fuzzyLogic.RemoveInference(inference);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                

                if (inference.op == Inference.OP.And || inference.op == Inference.OP.Or || inference.op == Inference.OP._I)
                {
                    DrawOneSideInput(inference, true);
                    DrawOP(inference);
                    if (inference.op != Inference.OP._I)
                    {
                        DrawOneSideInput(inference, false);
                    }
                    DrawOutput(inference);
                }
                else if (inference.op == Inference.OP.Not)
                {
                    DrawOP(inference);
                    DrawOneSideInput(inference, true);
                    DrawOutput(inference);
                }
                else
                {
                    // Do nothing
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawOutput(Inference inference)
        {
            GUIUtils.BeginBox(GUILayout.MaxWidth(100));
            {
                DrawCenterAlignedLabel("Output");

                EditorGUILayout.BeginHorizontal();
                {
                    inputLabels.Clear();
                    inputGuids.Clear();

                    inputLabels.Add("_");
                    inputGuids.Add(inference.guid);

                    // 1. add named data
                    for (int i = 0; i < inference.fuzzyLogic.defuzzification.NumberTrapezoids(); i++)
                    {
                        var trapezoid = inference.fuzzyLogic.defuzzification.GetTrapezoid(i);
                        if (string.IsNullOrWhiteSpace(trapezoid.name) == false)
                        {
                            if (NoOtherOutputsToThisDefuzzificationTrapezoid(inference, trapezoid.guid))
                            {
                                inputLabels.Add(trapezoid.name);
                                inputGuids.Add(trapezoid.guid);
                            }
                        }
                    }
                    // 1. add unnamed data
                    for (int i = 0; i < inference.fuzzyLogic.defuzzification.NumberTrapezoids(); i++)
                    {
                        var trapezoid = inference.fuzzyLogic.defuzzification.GetTrapezoid(i);
                        if (string.IsNullOrWhiteSpace(trapezoid.name))
                        {
                            if (NoOtherOutputsToThisDefuzzificationTrapezoid(inference, trapezoid.guid))
                            {
                                inputLabels.Add("Trapezoid" + i);
                                inputGuids.Add(trapezoid.guid);
                            }
                        }
                    }

                    if (inference.fuzzyLogic.IsDefuzzificationTrapezoidGUID(inference.outputGUID, out TrapezoidFuzzySet _) == false)
                    {
                        inference.outputGUID = inference.guid;
                    }

                    int selectedIndex = inputGuids.IndexOf(inference.outputGUID);
                    selectedIndex = EditorGUILayout.Popup(selectedIndex, inputLabels.ToArray());
                    inference.outputGUID = inputGuids[selectedIndex];

                    DrawCenterAlignedLabel(inference.Output().ToString("f2"));
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();
        }

        private static void DrawOP(Inference inference)
        {
            GUIUtils.BeginBox();
            {
                DrawCenterAlignedLabel("OP");
                inference.op = (Inference.OP)EditorGUILayout.EnumPopup(inference.op, GUILayout.Width(60));
            }
            GUIUtils.EndBox();
        }

        private static void DrawOneSideInput(Inference inference, bool leftSideOrRightSide)
        {
            DrawOneSideInput(inference,
                (data) =>
                {
                    if (leftSideOrRightSide)
                    {
                        inference.leftSideInputGUID = data;
                    }
                    else
                    {
                        inference.rightSideInputGUID = data;
                    }
                },
                () =>
                {
                    return leftSideOrRightSide ? inference.leftSideInputGUID : inference.rightSideInputGUID;
                }
            );
        }

        private static void DrawOneSideInput(Inference inference, Action<string> set_oneSideInputGUID, Func<string> get_oneSideInputGUID)
        {
            inputGuids.Clear();
            inputLabels.Clear();

            // set fuzzifications popup data
            for (int fuzzificationI = 0; fuzzificationI < inference.fuzzyLogic.NumberFuzzifications(); fuzzificationI++)
            {
                var fuzzification = inference.fuzzyLogic.GetFuzzification(fuzzificationI);
                inputLabels.Add(string.IsNullOrWhiteSpace(fuzzification.name) ? ("Fuzzification" + fuzzificationI) : fuzzification.name);
                inputGuids.Add(fuzzification.guid);
            }

            // set inferences popup data
            // 1. set named data
            for (int inferenceI = 0; inferenceI < inference.fuzzyLogic.NumberInferences(); inferenceI++)
            {
                var anotherInference = inference.fuzzyLogic.GetInference(inferenceI);
                if (inference != anotherInference/*not current inference itself*/)
                {
                    if (string.IsNullOrWhiteSpace(anotherInference.name) == false)
                    {
                        inputLabels.Add(anotherInference.name);
                        inputGuids.Add(anotherInference.guid);
                    }
                }
            }
            // 2. set unnamed data
            for (int inferenceI = 0; inferenceI < inference.fuzzyLogic.NumberInferences(); inferenceI++)
            {
                var _inference = inference.fuzzyLogic.GetInference(inferenceI);
                if (inference != _inference)
                {
                    if (string.IsNullOrWhiteSpace(_inference.name))
                    {
                        inputLabels.Add("Inference" + inferenceI);
                        inputGuids.Add(_inference.guid);
                    }
                }
            }

            GUIUtils.BeginBox();
            {
                DrawCenterAlignedLabel("Input");
                EditorGUILayout.BeginHorizontal();
                {
                    if (inference.fuzzyLogic.IsInferenceGUID(get_oneSideInputGUID()))
                    {
                        int selectedIndex = inputGuids.IndexOf(get_oneSideInputGUID());
                        int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, inputLabels.ToArray());
                        set_oneSideInputGUID(inputGuids[newSelectedIndex]);
                        // a fuzzification is selected
                        if (inference.fuzzyLogic.IsFuzzificationGUID(get_oneSideInputGUID()))
                        {
                            set_oneSideInputGUID(inference.fuzzyLogic.GetFuzzification(get_oneSideInputGUID()).GetTrapezoid(0).guid);
                        }
                        // an inference is selected
                        else
                        {
                            float output = inference.fuzzyLogic.GetInference(get_oneSideInputGUID()).Output();
                            if (inference.OutputIsCycleReference(output))
                            {
                                GUIUtils.ShowNotification("Cycle reference is not allowed");
                                set_oneSideInputGUID(inputGuids[selectedIndex]);
                            }
                            else
                            {
                                DrawCenterAlignedLabel(output.ToString("f2"), GUILayout.MaxWidth(80));
                            }
                        }
                    }
                    else if (inference.fuzzyLogic.IsFuzzificationTrapezoidGUID(get_oneSideInputGUID(), out Fuzzification o_fuzzification, out TrapezoidFuzzySet o_trapezoid))
                    {
                        /*
                         ArgumentException is throw from GUILayout.
                         Because unity will invoke OnGUI several times for different events in one frame.
                         After layout of gui is calculated and cached, unity will repaint gui with cached data.
                         And other codes are invoked as the same time.
                         But when unity invoke OnGUI for repainting gui, condition was changed, our codes doesn't get to this point again.
                         So unity rise an exception that cached layout data is not matching drawing gui.
                         I cann't find an elegant way to solve this problem, so I choose to ignore this exception, because it will not cause any side effect.
                         */
                        try
                        {
                            int selectedIndex = inputGuids.IndexOf(o_fuzzification.guid);
                            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, inputLabels.ToArray());
                            o_fuzzification = inference.fuzzyLogic.GetFuzzification(inputGuids[newSelectedIndex]);
                            // an inference is selected.
                            if (o_fuzzification == null)
                            {
                                set_oneSideInputGUID(inputGuids[newSelectedIndex]);
                                float output = inference.fuzzyLogic.GetInference(inputGuids[newSelectedIndex]).Output();
                                if (inference.OutputIsCycleReference(output))
                                {
                                    GUIUtils.ShowNotification("Cycle reference is not allowed");
                                    set_oneSideInputGUID(inputGuids[selectedIndex]);
                                }
                            }
                            // a fuzzification is selected
                            else
                            {
                                inputGuids.Clear();
                                inputLabels.Clear();

                                for (int trapezoidI = 0; trapezoidI < o_fuzzification.NumberTrapezoids(); trapezoidI++)
                                {
                                    var trapezoid = o_fuzzification.GetTrapezoid(trapezoidI);
                                    inputLabels.Add(string.IsNullOrWhiteSpace(trapezoid.name) ? ("Trapezoid" + trapezoidI) : trapezoid.name);
                                    inputGuids.Add(trapezoid.guid);
                                }

                                selectedIndex = Mathf.Max(inputGuids.IndexOf(get_oneSideInputGUID()), 0);
                                GUI.color = o_trapezoid.color;
                                {
                                    selectedIndex = EditorGUILayout.Popup(selectedIndex, inputLabels.ToArray());
                                }
                                GUI.color = Color.white;
                                set_oneSideInputGUID(inputGuids[selectedIndex]);

                                o_fuzzification.TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] intersectionValues, out TrapezoidFuzzySet[] intersectionTrapezoids);
                                int index = Array.IndexOf(intersectionTrapezoids, o_trapezoid);
                                float outputValue = 0;
                                if (index != -1)
                                {
                                    outputValue = intersectionValues[index].y;
                                }
                                DrawCenterAlignedLabel(outputValue.ToString("f2"), GUILayout.MaxWidth(80));
                            }
                        }
                        catch(ArgumentException)
                        {
                            // Do nothing
                        }
                    }
                    else
                    {
                        set_oneSideInputGUID(inference.fuzzyLogic.GetFuzzification(0).GetTrapezoid(0).guid);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();
        }

        private static void DrawCenterAlignedLabel(string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(label);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static bool NoOtherOutputsToThisDefuzzificationTrapezoid(Inference inference, string trapezoidGUID)
        {
            for (int inferenceI = 0; inferenceI < inference.fuzzyLogic.NumberInferences(); inferenceI++)
            {
                var otherInference = inference.fuzzyLogic.GetInference(inferenceI);
                if (otherInference != inference && otherInference.outputGUID == trapezoidGUID)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
