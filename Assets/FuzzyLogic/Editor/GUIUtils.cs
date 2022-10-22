#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class GUIUtils
    {
        public class Highlight
        {
            private string data = string.Empty;

            private Color color = Color.white;

            private string _targetGuid = null;
            public string targetGUID
            {
                private set
                {
                    _targetGuid = value;
                }
                get
                {
                    return _targetGuid;
                }
            }

            public void Draw2(string guid)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    Rect rect = GUILayoutUtility.GetLastRect();
                    if (Event.current.control)
                    {
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            targetGUID = guid;
                            DrawInternal(rect);
                        }
                        else
                        {
                            if (targetGUID == guid)
                            {
                                DrawInternal(rect);
                            }
                        }
                    }
                    else
                    {
                        targetGUID = null;
                    }
                }
            }

            public void Draw(string newData)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    if (string.IsNullOrEmpty(data))
                    {
                        data = newData;
                        color.a = 0;
                    }
                    else
                    {
                        if (data != newData)
                        {
                            data = newData;
                            Rect rect = GUILayoutUtility.GetLastRect();
                            DrawInternal(rect);
                        }
                    }
                }
            }

            private void DrawInternal(Rect rect)
            {
                color.a = 0.3f;
                GUI.color = color;
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
        }

        private static Highlight _highlight = null;
        public static Highlight highlight
        {
            get
            {
                if (_highlight == null)
                {
                    _highlight = new Highlight();
                }
                return _highlight;
            }
        }

        public static FuzzyLogicEditor fuzzyLogicEditor = null;

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

        public static void ShowNotification(string msg)
        {
            fuzzyLogicEditor.ShowNotification(new GUIContent(msg));
        }
        
        public static void BeginBox(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"), options);
        }

        public static void EndBox()
        {
            EditorGUILayout.EndVertical();
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
#endif
