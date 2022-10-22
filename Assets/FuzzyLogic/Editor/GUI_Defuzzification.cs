#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FuzzyLogicSystem.Editor
{
    public class GUI_Defuzzification
    {
        public static void Draw(Defuzzification defuzzification)
        {
            GUI_Fuzzification.Draw(defuzzification);
        }
    }
}
#endif
