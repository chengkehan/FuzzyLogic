using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class FuzzificationGUI : IGUI
    {
        private Fuzzification fuzzification = null;

        public const float MIN_GUI_HEIGHT = 400;

        public const float MIN_GUI_WIDTH = 350;

        private float _scaleOfAxisX = 1.0f;
        private float scaleOfAxisX
        {
            get
            {
                return _scaleOfAxisX;
            }
            set
            {
                _scaleOfAxisX = value;
            }
        }

        private FlsList<Vector2> positionList = new FlsList<Vector2>();

        public FuzzificationGUI(Fuzzification fuzzification)
        {
            this.fuzzification = fuzzification;
        }

        private Dictionary<Fuzzification, Vector2> _scrollFuzzificationTrapezoids = new Dictionary<Fuzzification, Vector2>();
        public Vector2 GetScrollPosition(Fuzzification fuzzification)
        {
            if (_scrollFuzzificationTrapezoids.ContainsKey(fuzzification) == false)
            {
                _scrollFuzzificationTrapezoids[fuzzification] = Vector2.zero;

            }
            return _scrollFuzzificationTrapezoids[fuzzification];
        }
        public void SetScrollPosition(Fuzzification fuzzification, Vector2 scollPosition)
        {
            _scrollFuzzificationTrapezoids[fuzzification] = scollPosition;
        }

        public bool IsDefuzzification()
        {
            return fuzzification is Defuzzification;
        }

        public void Draw()
        {
            float guiHeight = MIN_GUI_HEIGHT;
            if (IsDefuzzification() == false)
            {
                guiHeight = GUIUtils.Get(fuzzification.fuzzyLogic).editorWindow.fuzzificationGUIHeight;
            }

            float guiWidth = MIN_GUI_WIDTH;
            if (IsDefuzzification())
            {
                guiWidth = GUIUtils.Get(fuzzification.fuzzyLogic).editorWindow.defuzzificationGUIWidth;
            }

            float areaHeight = guiHeight;
            float glCanvasHeight = 230;
            Color coordinateColor = Color.gray;
            float axisArrowSize = 10;
            float axisDivisonSize = 5;

            float glCanvasMargin = 18;
            Rect glDisplay = new Rect(0, 0, guiWidth, glCanvasHeight);
            Vector2 originalPos = new Vector2(glCanvasMargin, glCanvasHeight - glCanvasMargin);
            Vector2 xAxisArrowPos = new Vector2(glDisplay.width - glCanvasMargin, originalPos.y);
            Vector2 yAxisArrowPos = new Vector2(originalPos.x, glCanvasMargin);
            Vector2 xAxisMaxPos = xAxisArrowPos; xAxisMaxPos.x -= 20;
            Vector2 yAxisMaxPos = yAxisArrowPos; yAxisMaxPos.y += 20;

            // Scale Coordinate
            if (IsDefuzzification())
            {
                var defuzzification = fuzzification as Defuzzification;
                float originalXOffset = ((xAxisMaxPos.x - originalPos.x) * (defuzzification.MinExtensionScale() * 0.75f)) * (1 - scaleOfAxisX);
                originalPos.x += originalXOffset;
                yAxisArrowPos.x += originalXOffset;
                yAxisMaxPos.x += originalXOffset;

                float xMaxPositionOffset = ((xAxisMaxPos.x - originalPos.x) * (defuzzification.MaxExtensionScale() * 0.75f)) * (1 - scaleOfAxisX);
                xAxisArrowPos.x -= xMaxPositionOffset;
                xAxisMaxPos.x -= xMaxPositionOffset;
            }

            // frame area
            if (IsDefuzzification())
            {
                GUILayout.BeginVertical(GUILayout.Width(guiWidth));
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.Width(guiWidth), GUILayout.Height(areaHeight));
            }
            {
                // gl canvas area
                Rect glCanvasRect = GUILayoutUtility.GetRect(glDisplay.width, glDisplay.height);
                GUI.BeginGroup(glCanvasRect);
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        GUIUtils.glMaterial.SetPass(0);

                        // Background
                        GUIUtils.GLDrawShape(GL.QUADS, () =>
                        {
                            GL.Color(new Color32(5, 15, 25, 255));

                            GL.Vertex3(0, 0, 0);
                            GL.Vertex3(0, glDisplay.height, 0);
                            GL.Vertex3(glDisplay.width, glDisplay.height, 0);
                            GL.Vertex3(glDisplay.width, 0, 0);
                        });

                        // Y axis
                        GUIUtils.GLDrawShape(GL.LINES, () =>
                        {
                            GL.Color(coordinateColor);

                            GUIUtils.GLVector2(originalPos);
                            GUIUtils.GLVector2(yAxisArrowPos);

                            GUIUtils.GLVector2(yAxisArrowPos);
                            GUIUtils.GLFloat2(yAxisArrowPos.x - axisArrowSize * 0.5f, yAxisArrowPos.y + axisArrowSize);

                            GUIUtils.GLVector2(yAxisArrowPos);
                            GUIUtils.GLFloat2(yAxisArrowPos.x + axisArrowSize * 0.5f, yAxisArrowPos.y + axisArrowSize);
                        });

                        // X Asix
                        GUIUtils.GLDrawShape(GL.LINES, () =>
                        {
                            GL.Color(coordinateColor);

                            GUIUtils.GLVector2(originalPos);
                            GUIUtils.GLVector2(xAxisArrowPos);

                            GUIUtils.GLVector2(xAxisArrowPos);
                            GUIUtils.GLFloat2(xAxisArrowPos.x - axisArrowSize, xAxisArrowPos.y + axisArrowSize * 0.5f);

                            GUIUtils.GLVector2(xAxisArrowPos);
                            GUIUtils.GLFloat2(xAxisArrowPos.x - axisArrowSize, xAxisArrowPos.y - axisArrowSize * 0.5f);
                        });

                        // Y Max
                        GUIUtils.GLDrawShape(GL.LINES, () =>
                        {
                            GL.Color(coordinateColor);

                            GUIUtils.GLVector2(yAxisMaxPos);
                            GUIUtils.GLFloat2(yAxisMaxPos.x + axisDivisonSize, yAxisMaxPos.y);
                        });

                        // Division
                        {
                            float divisionSpace = (xAxisMaxPos.x - originalPos.x) / fuzzification.division;
                            Vector3 divisionPos = originalPos;
                            for (int i = 1; i < fuzzification.division + 1; i++)
                            {
                                GUIUtils.GLDrawShape(GL.LINES, () =>
                                {
                                    GL.Color(coordinateColor);

                                    divisionPos.x += divisionSpace;
                                    GUIUtils.GLVector2(divisionPos);
                                    GUIUtils.GLFloat2(divisionPos.x, divisionPos.y - axisDivisonSize);
                                });
                            }
                        }

                        // Trapezoids
                        for (int i = 0; i < fuzzification.NumberTrapezoids(); i++)
                        {
                            var trapezoid = fuzzification.GetTrapezoid(i);

                            GUIUtils.GLDrawShape(GL.LINES, () =>
                            {
                                GL.Color(trapezoid.color);
                                /*
                                  leftBorder
                                      |p1 ^    p2                 rightBorder
                                      | *-|----*                      |
                                      |/  |     \                     |
                                      *   |      \                    |
                                     /|   |       \                   |
                                    /#|   |        \                  |
                                   /##|   |         \                 |
                                  *-------o----------*---------------------->
                                  p0                 p3
                                 
                                  Truncate a trapezoid with two vertical lines which are left border and right border.
                                  Only the part in range of [leftBorder, rightBorder] is displayed.
                                  In figure above, the triangle filled with "#" will be truncated,
                                  because of this triangle is at the outside of [leftBorder, rightBorder],
                                  the other part of trapezoid is reserved.
                                */

                                Vector2 p0 = trapezoid.FootPointLeftPos(originalPos, xAxisMaxPos, yAxisMaxPos);
                                Vector2 p1 = trapezoid.PeakPointLeftPos(originalPos, xAxisMaxPos, yAxisMaxPos);
                                Vector2 p2 = trapezoid.PeakPointRightPos(originalPos, xAxisMaxPos, yAxisMaxPos);
                                Vector2 p3 = trapezoid.FootPointRightPos(originalPos, xAxisMaxPos, yAxisMaxPos);

                                positionList.Clear();
                                positionList.Add(p0);
                                positionList.Add(p1);
                                positionList.Add(p2);
                                positionList.Add(p3);

                                // Truncate with left border
                                float leftBorderX = 0;
                                for (int i = 0; i < positionList.size - 1; i++)
                                {
                                    Vector2 pA = positionList[i];
                                    Vector2 pB = positionList[i + 1];

                                    if (pA.x < leftBorderX)
                                    {
                                        positionList.RemoveAt(i);
                                        --i;
                                    }

                                    if (pA.x < leftBorderX && leftBorderX < pB.x)
                                    {
                                        Vector2 pM = pB - pA;
                                        pM *= (leftBorderX - pA.x) / (pB.x - pA.x);
                                        pM += pA;
                                        positionList.Insert(0, pM);
                                        break;
                                    }
                                }

                                // Truncate with right border
                                float rightBorderX = glDisplay.width;
                                for (int i = positionList.size - 1; i > 0; i--)
                                {
                                    Vector2 pA = positionList[i];
                                    Vector2 pB = positionList[i - 1];

                                    if (pA.x > rightBorderX)
                                    {
                                        positionList.RemoveAt(i);
                                    }

                                    if (pA.x > rightBorderX && rightBorderX > pB.x)
                                    {
                                        Vector2 pM = pB - pA;
                                        pM *= (rightBorderX - pA.x) / (pB.x - pA.x);
                                        pM += pA;
                                        positionList.Add(pM);
                                        break;
                                    }
                                }

                                // Draw 
                                if (positionList.size > 1)
                                {
                                    for (int i = 0; i < positionList.size - 1; i++)
                                    {
                                        GUIUtils.GLVector2(positionList[i]);
                                        GUIUtils.GLVector2(positionList[i + 1]);
                                    }
                                }
                            });
                        }

                        // Value Dash line and Cross Point
                        {
                            fuzzification.BaseLinePositions(originalPos, xAxisMaxPos, yAxisMaxPos, out Vector2 pStart, out Vector2 pEnd);
                            pEnd.y = 99999;

                            fuzzification.TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] _, out TrapezoidFuzzySet[] intersectionTrapezoids);
                            fuzzification.TestIntersectionPositionsOfBaseLineAndTrapezoids(originalPos, xAxisMaxPos, yAxisMaxPos, out Vector2[] intersectionPositions, out TrapezoidFuzzySet[] _);
                            if (intersectionPositions != null && intersectionPositions.Length > 0)
                            {
                                foreach (var p in intersectionPositions)
                                {
                                    pEnd.y = Mathf.Min(pEnd.y, p.y);
                                }

                                GUIUtils.GLDrawDashLine(coordinateColor, pStart, pEnd);

                                for (int i = 0; i < intersectionPositions.Length; i++)
                                {
                                    GUIUtils.GLDrawDot(intersectionTrapezoids[i].color, intersectionPositions[i]);
                                    GUIUtils.GLDrawDashLine(coordinateColor, intersectionPositions[i], new Vector2(originalPos.x, intersectionPositions[i].y));
                                }
                            }
                        }

                        // Barycenter and new shape
                        if (IsDefuzzification() && fuzzification.fuzzyLogic.evaluate)
                        {
                            // Barycenter
                            var defuzzification = fuzzification as Defuzzification;
                            Vector2 barycenterPoint = defuzzification.OutputValue(out Vector2[] _, out Vector2[] _);
                            Vector2 barycenterPosition = defuzzification.OutputPosition(barycenterPoint, originalPos, xAxisMaxPos, yAxisMaxPos, out Vector2[] shapePositions, out Vector2[] bottomPositions);
                            GUIUtils.GLDrawDot(Color.white, barycenterPosition);

                            // Shape
                            if (shapePositions != null && bottomPositions != null)
                            {
                                float leftBorderX = 0;
                                float rightBorderX = glDisplay.width;
                                Color shapeColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                                Color pointColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                                for (int i = 0; i < shapePositions.Length - 1; i++)
                                {
                                    Vector2 shapeP = shapePositions[i];
                                    Vector2 bottomP = bottomPositions[i];
                                    Vector2 nextShapeP = shapePositions[i + 1];
                                    Vector2 nextBottomP = bottomPositions[i + 1];

                                    // Truncate shape with left border and right border
                                    // Take a look at a similar comment above that is truncate trapezoid.
                                    if ((shapeP.x < leftBorderX && nextShapeP.x < leftBorderX) || (shapeP.x > rightBorderX && nextShapeP.x > rightBorderX))
                                    {
                                        // Skip
                                    }
                                    else
                                    {
                                        // Truncated by left border
                                        if (shapeP.x < leftBorderX && nextShapeP.x > leftBorderX)
                                        {
                                            Vector2 p = new Vector2(leftBorderX, shapeP.y + (leftBorderX - shapeP.x) / (nextShapeP.x - shapeP.x) * (nextShapeP.y - shapeP.y));
                                            Vector2 pb = new Vector2(p.x, originalPos.y);
                                            shapeP = p;
                                            bottomP = pb;
                                        }
                                        // Truncated by right border
                                        if (shapeP.x < rightBorderX && nextShapeP.x > rightBorderX)
                                        {
                                            Vector2 p = new Vector2(rightBorderX, shapeP.y + (rightBorderX - shapeP.x) / (nextShapeP.x - shapeP.x) * (nextShapeP.y - shapeP.y));
                                            Vector2 pb = new Vector2(p.x, originalPos.y);
                                            nextShapeP = p;
                                            nextBottomP = pb;
                                        }

                                        GUIUtils.GLDrawDot(pointColor, shapeP);

                                        GUIUtils.GLDrawShape(GL.TRIANGLES, () =>
                                        {
                                            GL.Color(shapeColor);

                                            GUIUtils.GLVector2(shapeP);
                                            GUIUtils.GLVector2(bottomP);
                                            GUIUtils.GLVector2(nextShapeP);

                                            GUIUtils.GLVector2(nextShapeP);
                                            GUIUtils.GLVector2(bottomP);
                                            GUIUtils.GLVector2(nextBottomP);
                                        });
                                    }
                                }
                            }
                        }

                    } // End Repaint Event

                    // Display value
                    {
                        fuzzification.TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] intersectionValues, out TrapezoidFuzzySet[] intersectionTrapezoids);
                        fuzzification.TestIntersectionPositionsOfBaseLineAndTrapezoids(originalPos, xAxisMaxPos, yAxisMaxPos, out Vector2[] intersectionPositions, out TrapezoidFuzzySet[] _);
                        if (intersectionPositions != null)
                        {
                            for (int i = 0; i < intersectionPositions.Length; i++)
                            {
                                GUI.color = intersectionTrapezoids[i].color;
                                GUI.Label(new Rect(originalPos.x, intersectionPositions[i].y, 50, 20), intersectionValues[i].y.ToString("f2"));
                                GUI.color = Color.white;
                            }
                        }
                    }

                    // Y Max Value   
                    {
                        GUI.color = coordinateColor;
                        GUI.Label(new Rect(yAxisMaxPos.x - 15, yAxisMaxPos.y - 10, 20, 20), "1");
                        GUI.color = Color.white;
                    }

                    // X Division Values
                    {
                        float divisionSpace = (xAxisMaxPos.x - originalPos.x) / fuzzification.division;
                        float currentValue = 0;
                        float valueSpace = fuzzification.maxValue / (float)fuzzification.division;
                        GUI.color = coordinateColor;
                        Rect rect = new Rect(originalPos.x - 10, originalPos.y, 100, 20);
                        for (int i = 0; i < fuzzification.division + 1; i++)
                        {
                            if (i == fuzzification.division)
                            {
                                currentValue = fuzzification.maxValue;
                            }
                            GUI.Label(rect, Mathf.Floor(currentValue).ToString());
                            currentValue += valueSpace;
                            rect.x += divisionSpace;
                        }
                        GUI.color = Color.white;
                    }

                    // Delete
                    // Make sure there is one fuzzification at least.
                    if (fuzzification.fuzzyLogic.NumberFuzzifications() > 1)
                    {
                        if (GUI.Button(new Rect(glDisplay.width - 20, 0, 20, 20), "x"))
                        {
                            GUIUtils.GUILoseFocus();
                            GUIUtils.UndoStackRecord(fuzzification.fuzzyLogic);
                            fuzzification.fuzzyLogic.RemoveFuzzification(fuzzification);
                        }
                        DrawFocusOnButton(new Rect(glDisplay.width - 40, 0, 20, 20));
                    }
                    else
                    {
                        DrawFocusOnButton(new Rect(glDisplay.width - 20, 0, 20, 20));
                    }

                    // Scale Coordinate
                    if (IsDefuzzification())
                    {
                        scaleOfAxisX = GUI.VerticalScrollbar(new Rect(glDisplay.width - 16, 20, 20, 100), scaleOfAxisX, 0f, 1f, 0f);
                    }
                }
                GUI.EndGroup();

                // gui area
                GUIUtils.BeginBox();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Name", GUILayout.Width(50));
                        GUIUtils.TextField(fuzzification.fuzzyLogic, fuzzification.name, t=>fuzzification.name=t);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Max Value");
                            GUIUtils.UIntField(fuzzification.fuzzyLogic, fuzzification.maxValue, v=>fuzzification.maxValue=v, GUILayout.Width(50));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Division");
                            GUIUtils.IntSlider(fuzzification.fuzzyLogic, fuzzification.division, 1, 20, v=>fuzzification.division=v);
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Value");
                        GUIUtils.Slider(fuzzification.fuzzyLogic, fuzzification.value, 0, fuzzification.maxValue, v=>fuzzification.value=v);

                        if (IsDefuzzification())
                        {
                            var defuzzification = fuzzification as Defuzzification;
                            GUILayout.Space(5);
                            GUILayout.Label("Subdivision");
                            GUIUtils.IntSlider(defuzzification.fuzzyLogic, defuzzification.subdivision, 10, 100, v=>defuzzification.subdivision=v);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUIUtils.EndBox();

                GUIUtils.BeginBox();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Trapezoids");
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+", GUILayout.Width(20)))
                        {
                            GUIUtils.GUILoseFocus();
                            GUIUtils.UndoStackRecord(fuzzification.fuzzyLogic);
                            fuzzification.AddTrapezoid();
                        }
                    }
                    GUILayout.EndHorizontal();

                    SetScrollPosition(fuzzification, GUILayout.BeginScrollView(GetScrollPosition(fuzzification)));
                    {
                        for (int i = 0; i < fuzzification.NumberTrapezoids(); i++)
                        {
                            var trapezoid = fuzzification.GetTrapezoid(i);
                            GUILayout.BeginVertical();
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Space(5);
                                    GUIUtils.TextField(trapezoid.fuzzyLogic, trapezoid.name, t=>trapezoid.name=t, GUILayout.Width(70));

                                    float peakPointLeftValue = trapezoid.peakPointLeftValue;
                                    float peakPointRightValue = trapezoid.peakPointRightValue;

                                    GUIUtils.FloatField(trapezoid.fuzzyLogic, peakPointLeftValue, v=>peakPointLeftValue=v, GUILayout.Width(30));
                                    GUIUtils.FloatField(trapezoid.fuzzyLogic, peakPointRightValue, v=>peakPointRightValue=v, GUILayout.Width(30));

                                    GUILayout.Space(5);

                                    GUIUtils.MinMaxSlider(trapezoid.fuzzyLogic, peakPointLeftValue, peakPointRightValue, fuzzification.MinValue(), fuzzification.MaxValue(), v=>peakPointLeftValue=v, v=>peakPointRightValue=v);

                                    trapezoid.peakPointLeftValue = peakPointLeftValue;
                                    trapezoid.peakPointRightValue = peakPointRightValue;
                                    trapezoid.UpdateFootPointValue();

                                    GUILayout.Space(5);
                                    trapezoid.color = EditorGUILayout.ColorField(trapezoid.color, GUILayout.Width(40));
                                    if (fuzzification.IsLeftShoulder(trapezoid) == false && fuzzification.IsRightShoulder(trapezoid) == false)
                                    {
                                        if (GUILayout.Button("-", GUILayout.Width(20)))
                                        {
                                            GUIUtils.GUILoseFocus();
                                            GUIUtils.UndoStackRecord(fuzzification.fuzzyLogic);
                                            fuzzification.RemoveTrapezoid(i);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        GUI.enabled = false;
                                        GUILayout.Button(string.Empty, GUILayout.Width(20));
                                        GUI.enabled = true;
                                    }
                                }
                                GUILayout.EndHorizontal();

                                if (IsDefuzzification())
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Space(5);

                                        // set alpha to 0 to make gui invisible to keep layout aligned with previous line
                                        GUI.color = new Color(1, 1, 1, 0);
                                        GUILayout.TextField(string.Empty, GUILayout.Width(70));
                                        GUI.color = Color.white;

                                        float footPointLeftValue = trapezoid.footPointLeftValue;
                                        float footPointRightValue = trapezoid.footPointRightValue;

                                        GUIUtils.FloatField(fuzzification.fuzzyLogic, footPointLeftValue, v=>footPointLeftValue=v, GUILayout.Width(30));
                                        GUIUtils.FloatField(fuzzification.fuzzyLogic, footPointRightValue, v=>footPointRightValue=v, GUILayout.Width(30));

                                        GUILayout.Space(5);

                                        GUIUtils.MinMaxSlider(fuzzification.fuzzyLogic, footPointLeftValue, footPointRightValue, fuzzification.MinValue(), fuzzification.MaxValue(), v=>footPointLeftValue=v, v=>footPointRightValue=v);

                                        trapezoid.footPointLeftValue = footPointLeftValue;
                                        trapezoid.footPointRightValue = footPointRightValue;

                                        GUILayout.Space(5);

                                        GUI.color = new Color(1, 1, 1, 0);
                                        EditorGUILayout.ColorField(Color.white, GUILayout.Width(40));
                                        GUI.color = Color.white;

                                        GUI.enabled = false;
                                        GUI.color = new Color(1, 1, 1, 0);
                                        GUILayout.Button(string.Empty, GUILayout.Width(20));
                                        GUI.color = Color.white;
                                        GUI.enabled = true;
                                    }
                                    GUILayout.EndHorizontal();
                                }
                            }
                            GUILayout.EndVertical();

                            GUIUtils.Get(trapezoid.fuzzyLogic).highlight.Draw2(trapezoid.guid);
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUIUtils.EndBox();
            }
            GUILayout.EndVertical();
        }

        private void DrawFocusOnButton(Rect rect)
        {
            if (GUI.Button(rect, new GUIContent(EditorGUIUtility.FindTexture("d_ScaleTool On"), "Focus on this and Hide others")))
            {
                var focusedTargetGUID = GUIUtils.Get(fuzzification.fuzzyLogic).editorWindow.focusedTargetGUID;
                if (focusedTargetGUID == null)
                {
                    focusedTargetGUID = fuzzification.guid;
                }
                else
                {
                    focusedTargetGUID = null;
                }

                float w = MIN_GUI_WIDTH;
                float h = MIN_GUI_HEIGHT;
                if (IsDefuzzification())
                {
                    w *= 2;
                    h *= 1.3f;
                }

                GUIUtils.Get(fuzzification.fuzzyLogic).editorWindow.SetFocusedTargetGUID(focusedTargetGUID, w, h);
            }
        }
    }
}