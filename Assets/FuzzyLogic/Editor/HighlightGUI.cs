using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class HighlightGUI
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
}
