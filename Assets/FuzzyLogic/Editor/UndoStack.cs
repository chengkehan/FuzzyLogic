using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class UndoStack
    {
        private List<HistoryItem> historyList = new List<HistoryItem>(50);

        private int pointerIndex = 0;

        private HistoryItem tempTopHistoryItem = null;

        public void Undo(FuzzyLogicEditor fuzzyLogicEditor)
        {
            if (fuzzyLogicEditor != null)
            {
                if (pointerIndex <= 0)
                {
                    EditorApplication.Beep();
                }
                else
                {
                    if (pointerIndex == historyList.Count)
                    {
                        tempTopHistoryItem = new HistoryItem();
                        tempTopHistoryItem.serializedData = FuzzyLogic.Serialize(fuzzyLogicEditor.fuzzyLogic);
                    }

                    pointerIndex--;
                    var historyItem = historyList[pointerIndex];
                    fuzzyLogicEditor.fuzzyLogic = FuzzyLogic.Deserialize(historyItem.serializedData, fuzzyLogicEditor.fuzzyLogic);
                }
            }
        }

        public void Redo(FuzzyLogicEditor fuzzyLogicEditor)
        {
            if (fuzzyLogicEditor != null)
            {
                if (pointerIndex >= historyList.Count - 1)
                {
                    if (tempTopHistoryItem == null)
                    {
                        EditorApplication.Beep();
                    }
                    else
                    {
                        fuzzyLogicEditor.fuzzyLogic = FuzzyLogic.Deserialize(tempTopHistoryItem.serializedData, fuzzyLogicEditor.fuzzyLogic);
                        tempTopHistoryItem = null;
                        pointerIndex = historyList.Count;
                    }
                }
                else
                {
                    pointerIndex++;
                    var historyItem = historyList[pointerIndex];
                    fuzzyLogicEditor.fuzzyLogic = FuzzyLogic.Deserialize(historyItem.serializedData, fuzzyLogicEditor.fuzzyLogic);
                }
            }
        }

        public void Record(FuzzyLogic fuzzyLogic)
        {
            Record_Internal(fuzzyLogic);
        }

        public void Empty()
        {
            historyList.Clear();
            pointerIndex = 0;
            tempTopHistoryItem = null;
        }

        private void Record_Internal(FuzzyLogic fuzzyLogic)
        {
            if (fuzzyLogic != null)
            {
                tempTopHistoryItem = null;

                for (int i = historyList.Count - 1; i >= pointerIndex; --i)
                {
                    historyList.RemoveAt(i);
                }

                if (historyList.Count + 1 > historyList.Capacity)
                {
                    historyList.RemoveAt(0);
                }

                var historyItem = new HistoryItem();
                historyItem.serializedData = FuzzyLogic.Serialize(fuzzyLogic);
                historyList.Add(historyItem);
                pointerIndex = historyList.Count;

                GUIUtils.Get(fuzzyLogic).isChanged = true;
            }
        }

        private class HistoryItem
        {
            public byte[] serializedData = null;
        }
    }
}
