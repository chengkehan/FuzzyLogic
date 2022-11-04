using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class InferenceGUI : IGUI
    {
        public const float FORCUS_ON_DEFAULT_WIDTH = 1000;

        public const float FORCUS_ON_DEFAULT_HEIGHT = 200;

        private class Highlights
        {
            private Dictionary<Inference, HighlightGUI> _highlighes = new Dictionary<Inference, HighlightGUI>();

            public HighlightGUI Get(Inference inference)
            {
                if (_highlighes.TryGetValue(inference, out HighlightGUI highlight) == false)
                {
                    highlight = new HighlightGUI();
                    _highlighes.Add(inference, highlight);
                }
                return highlight;
            }
        }

        private Inference inference = null;

        private FlsList<string> inputGuids = new FlsList<string>();

        private FlsList<string> inputLabels = new FlsList<string>();

        private Highlights outputHighlights = new Highlights();

        private Highlights leftSideOutputHighlights = new Highlights();

        private Highlights rightSideOutputHighlighes = new Highlights();

        public InferenceGUI(Inference inference)
        {
            this.inference = inference;
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(100));
                {
                    GUIUtils.TextField(inference.fuzzyLogic, inference.name, t=>inference.name=t);

                    if (GUILayout.Button("x", GUILayout.Width(20)))
                    {
                        GUIUtils.GUILoseFocus();
                        GUIUtils.UndoStackRecord(inference.fuzzyLogic);
                        inference.fuzzyLogic.RemoveInference(inference);
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

        private void DrawOutput(Inference inference)
        {
            GUIUtils.BeginBox(GUILayout.MaxWidth(150));
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
                    GUIUtils.Popup(inference.fuzzyLogic, selectedIndex, inputLabels.ToArray(), o => selectedIndex = o);
                    inference.outputGUID = inputGuids[selectedIndex];

                    string outputStr = inference.Output().ToString("f2");
                    DrawCenterAlignedLabel(outputStr);
                    outputHighlights.Get(inference).Draw(outputStr);
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();

            GUIUtils.Get(inference.fuzzyLogic).highlight.Draw2(inference.outputGUID);
        }

        private void DrawOP(Inference inference)
        {
            GUIUtils.BeginBox(GUILayout.Width(120));
            {
                DrawCenterAlignedLabel("OP");
                GUIUtils.EnumPopup(inference.fuzzyLogic, inference.op, o => inference.op = o);
            }
            GUIUtils.EndBox();
        }

        private void DrawOneSideInput(Inference inference, bool leftSideOrRightSide)
        {
            Highlights highlights = leftSideOrRightSide ? leftSideOutputHighlights : rightSideOutputHighlighes;

            DrawOneSideInput(inference, highlights,
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

        private void DrawOneSideInput(Inference inference, Highlights highlights, Action<string> set_oneSideInputGUID, Func<string> get_oneSideInputGUID)
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

            // registered fuzzyLogics
            for (int fuzzeLogicI = 0; fuzzeLogicI < FuzzyLogic.NumberRegisteredFuzzyLogics(); fuzzeLogicI++)
            {
                var aFuzzyLogic = FuzzyLogic.GetRegisteredFuzzyLogic(fuzzeLogicI);
                if (aFuzzyLogic != inference.fuzzyLogic)
                {
                    string popupMenuItemPath = GUIUtils.Get(aFuzzyLogic).popupMenuItemPath;
                    string fuzzyLogicName = string.IsNullOrWhiteSpace(aFuzzyLogic.name) ? aFuzzyLogic.guid : aFuzzyLogic.name;
                    inputLabels.Add("FuzzyLogics/" + (string.IsNullOrEmpty(popupMenuItemPath) ? fuzzyLogicName : popupMenuItemPath + "/" + fuzzyLogicName));
                    inputGuids.Add(aFuzzyLogic.guid);
                }
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

            GUIUtils.BeginBox(GUILayout.Width(300));
            {
                DrawCenterAlignedLabel("Input");
                EditorGUILayout.BeginHorizontal();
                {
                    string originalGUID = get_oneSideInputGUID();

                    int selectedIndex = 0;
                    if (inference.fuzzyLogic.IsFuzzificationTrapezoidGUID(originalGUID, out Fuzzification selectedFuzzification, out TrapezoidFuzzySet selectedTrapezoid))
                    {
                        selectedIndex = inputGuids.IndexOf(selectedFuzzification.guid);
                    }
                    else
                    {
                        selectedIndex = inputGuids.IndexOf(originalGUID);
                    }

                    // oneSideInputGUID of a newly created inference is null,
                    // selectedIndex will be -1, so clamp it.
                    selectedIndex = Mathf.Max(selectedIndex, 0);

                    int newSelectedIndex = 0;
                    GUIUtils.Popup(inference.fuzzyLogic, selectedIndex, inputLabels.ToArray(), o=>newSelectedIndex=o);
                    string newSelectedGUID = inputGuids[newSelectedIndex];

                    if (inference.fuzzyLogic.IsFuzzificationGUID(newSelectedGUID))
                    {
                        selectedFuzzification = inference.fuzzyLogic.GetFuzzification(newSelectedGUID);
                        selectedTrapezoid = selectedFuzzification.GetTrapezoid(originalGUID);
                        if (selectedTrapezoid == null)
                        {
                            selectedTrapezoid = selectedFuzzification.GetTrapezoid(0);
                        }

                        inputGuids.Clear();
                        inputLabels.Clear();

                        for (int trapezoidI = 0; trapezoidI < selectedFuzzification.NumberTrapezoids(); trapezoidI++)
                        {
                            var trapezoid = selectedFuzzification.GetTrapezoid(trapezoidI);
                            inputLabels.Add(string.IsNullOrWhiteSpace(trapezoid.name) ? ("Trapezoid" + trapezoidI) : trapezoid.name);
                            inputGuids.Add(trapezoid.guid);
                        }

                        selectedIndex = inputGuids.IndexOf(selectedTrapezoid.guid);
                        GUI.color = selectedTrapezoid.color;
                        {
                            GUIUtils.Popup(inference.fuzzyLogic, selectedIndex, inputLabels.ToArray(), o => selectedIndex = o);
                        }
                        GUI.color = Color.white;
                        set_oneSideInputGUID(inputGuids[selectedIndex]);

                        selectedFuzzification.TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] intersectionValues, out TrapezoidFuzzySet[] intersectionTrapezoids);
                        int index = Array.IndexOf(intersectionTrapezoids, selectedTrapezoid);
                        float outputValue = 0;
                        if (index != -1)
                        {
                            outputValue = intersectionValues[index].y;
                        }
                        var outputStr = outputValue.ToString("f2");
                        DrawCenterAlignedLabel(outputStr, GUILayout.MaxWidth(80));
                        highlights.Get(inference).Draw(outputStr);
                    }
                    else if (inference.fuzzyLogic.IsInferenceGUID(newSelectedGUID))
                    {
                        set_oneSideInputGUID(newSelectedGUID);

                        if (inference.IsCycleReference())
                        {
                            GUIUtils.Get(inference.fuzzyLogic).ShowNotification("Cycle reference is not allowed");
                            set_oneSideInputGUID(originalGUID);
                        }
                        else
                        {
                            var newSelectedInference = inference.fuzzyLogic.GetInference(newSelectedGUID);
                            var outputStr = newSelectedInference.Output().ToString("f2");
                            DrawCenterAlignedLabel(outputStr, GUILayout.MaxWidth(80));
                            highlights.Get(inference).Draw(outputStr);
                        }
                    }
                    else if (FuzzyLogic.IsRegisteredFuzzyLogic(newSelectedGUID))
                    {
                        set_oneSideInputGUID(newSelectedGUID);

                        if (FuzzyLogic.IsCycleReference(inference.fuzzyLogic))
                        {
                            GUIUtils.Get(inference.fuzzyLogic).ShowNotification("Cycle reference is not allowed");
                            set_oneSideInputGUID(originalGUID);
                        }
                        else
                        {
                            var outputStr = FuzzyLogic.GetRegisteredFuzzyLogic(newSelectedGUID).Output().ToString("f2");
                            DrawCenterAlignedLabel(outputStr, GUILayout.MaxWidth(80));
                            highlights.Get(inference).Draw(outputStr);
                        }
                    }
                    else
                    {
                        throw new Exception("Unexpected");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUIUtils.EndBox();

            GUIUtils.Get(inference.fuzzyLogic).highlight.Draw2(get_oneSideInputGUID());
        }

        private void DrawCenterAlignedLabel(string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(label);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool NoOtherOutputsToThisDefuzzificationTrapezoid(Inference inference, string trapezoidGUID)
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
