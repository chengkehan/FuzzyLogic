using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace FuzzyLogicSystem
{
    [Serializable]
    public class Inference
    {
        public enum OP
        {
            And = 1, // Take an intersection set of left side input and right side input
            Or = 2, // Take an union set of left side input and righ side input
            Not = 3, // Take a supplementary set of left side input
            _I = 4 // Use left side input and ignore right side input
        }

        // Editor only gui
        private IGUI _gui = null;
        public IGUI gui
        {
            set
            {
                _gui = value;
            }
            get
            {
                return _gui;
            }
        }

        // Belong to which fuzzyLogic
        private FuzzyLogic _fuzzyLogic = null;
        public FuzzyLogic fuzzyLogic
        {
            get
            {
                return _fuzzyLogic;
            }
            set
            {
                _fuzzyLogic = value;
            }
        }

        [SerializeField]
        private string _guid = null;
        public string guid
        {
            private set
            {
                _guid = value;
            }
            get
            {
                return _guid;
            }
        }

        [SerializeField]
        private string _name = null;
        public string name
        {
            set
            {
                _name = value;
            }
            get
            {
                return _name;
            }
        }

        [SerializeField]
        private string _leftSideInputGUID = null;
        public string leftSideInputGUID
        {
            set
            {
                _leftSideInputGUID = value;
            }
            get
            {
                return _leftSideInputGUID;
            }
        }

        [SerializeField]
        private string _rightSideInputGUID = null;
        public string rightSideInputGUID
        {
            set
            {
                _rightSideInputGUID = value;
            }
            get
            {
                return _rightSideInputGUID;
            }
        }

        [SerializeField]
        private string _outputGUID = null;
        public string outputGUID
        {
            set
            {
                _outputGUID = value;
            }
            get
            {
                return _outputGUID;
            }
        }

        [SerializeField]
        private OP _op = OP.And;
        public OP op
        {
            set
            {
                _op = value;
            }
            get
            {
                return _op;
            }
        }

        public Inference(string guid)
        {
            this.guid = guid;
        }

        /*
          When leftSideInputGUID or rightSideInputGUID of this inference(eg named A) is another inference(eg named B),
          meanwhile, leftSideInputGUID or rightSideInputGUID of another inference(named B) is set as this inference(named A) directly or linked indirectly,
          we can call this case is cycle reference.

          Cycle reference will cause program find the source of data flow on and on.
          It will never exit until stack overflow. You can also think is as a endless loop util it use up all resources of computer.
          Just like code "while(true){ allocate memory and calculate here }".

          This function is used to find out cycle reference. It will return true if cycle reference is existed, otherwise return false.

          Figure of Cycle Reference:

              A -
              ^ |
              |_|           

              A -> B
              ^    |
              |____|

              A -> B -> C -> D
              ^              |
              |______________|
        */
        public bool IsCycleReference()
        {
            return IsCycleReference_Internal(guid);
        }

        private bool IsCycleReference_Internal(string guid)
        {
            if (leftSideInputGUID == guid)
            {
                return true;
            }
            else
            {
                if (fuzzyLogic.IsInferenceGUID(leftSideInputGUID))
                {
                    if(fuzzyLogic.GetInference(leftSideInputGUID).IsCycleReference_Internal(guid))
                    {
                        return true;
                    }
                }
            }

            if (op == OP.And || op == OP.Or)
            {
                if (rightSideInputGUID == guid)
                {
                    return true;
                }
                else
                {
                    if (fuzzyLogic.IsInferenceGUID(rightSideInputGUID))
                    {
                        if(fuzzyLogic.GetInference(rightSideInputGUID).IsCycleReference_Internal(guid))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Calculate output of this inference
        public float Output()
        {
            return Output_Internal();
        }

        private float Output_Internal()
        {
            if (op == OP.And)
            {
                float leftSideOutput = LeftSideOutput();
                float rightSideOutput = RightSideOutput();
                if (leftSideOutput < 0 || rightSideOutput < 0)
                {
                    return -1;
                }
                else
                {
                    return Mathf.Min(leftSideOutput, rightSideOutput);
                }
            }
            else if (op == OP.Or)
            {
                float leftSideOutput = LeftSideOutput();
                float rightSideOutput = RightSideOutput();
                if (leftSideOutput < 0 || rightSideOutput < 0)
                {
                    return -1;
                }
                else
                {
                    return Mathf.Max(leftSideOutput, rightSideOutput);
                }
            }
            else if (op == OP.Not)
            {
                float leftSideOutput = LeftSideOutput();
                if (leftSideOutput < 0)
                {
                    return -1;
                }
                else
                {
                    return 1 - leftSideOutput;
                }
            }
            else if (op == OP._I)
            {
                float leftSideOutput = LeftSideOutput();
                if (leftSideOutput < 0)
                {
                    return -1;
                }
                else
                {
                    return leftSideOutput;
                }
            }
            else
            {
                return 0;
            }
        }

        private float LeftSideOutput()
        {
            return OneSideOutput(leftSideInputGUID);
        }

        private float RightSideOutput()
        {
            return OneSideOutput(rightSideInputGUID);
        }

        private float OneSideOutput(string guid)
        {
            if (fuzzyLogic.IsInferenceGUID(guid))
            {
                return fuzzyLogic.GetInference(guid).Output_Internal();
            }
            else if (fuzzyLogic.IsFuzzificationTrapezoidGUID(guid, out Fuzzification fuzzification, out TrapezoidFuzzySet trapezoid))
            {
                fuzzification.TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] intersectionValues, out TrapezoidFuzzySet[] intersectionTrapezoids);
                int index = Array.IndexOf(intersectionTrapezoids, trapezoid);
                float value = 0;
                if (index != -1)
                {
                    value = intersectionValues[index].y;
                }
                return value;
            }
            else if(FuzzyLogic.IsRegisteredFuzzyLogic(guid))
            {
                return FuzzyLogic.GetRegisteredFuzzyLogic(guid).Output();
            }
            else
            {
                return 0;
            }
        }
    }
}
