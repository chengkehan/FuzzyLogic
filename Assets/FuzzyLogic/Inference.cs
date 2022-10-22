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

        public bool OutputIsCycleReference(float output)
        {
            return output < 0;
        }

        // Calculate output of this inference
        // returned value is in range of [0, 1] normally.
        // if cycle reference is detected, returned value is a negative.
        // Pass returned value into IsOutputCycleReference to check cycle reference. 
        public float Output()
        {
            return Output_Internal(0);
        }

        private float Output_Internal(int depth)
        {
            ++depth;
            if (op == OP.And)
            {
                float leftSideOutput = LeftSideOutput(depth);
                float rightSideOutput = RightSideOutput(depth);
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
                float leftSideOutput = LeftSideOutput(depth);
                float rightSideOutput = RightSideOutput(depth);
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
                float leftSideOutput = LeftSideOutput(depth);
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
                float leftSideOutput = LeftSideOutput(depth);
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

        private float LeftSideOutput(int depth)
        {
            return OneSideOutput(leftSideInputGUID, depth);
        }

        private float RightSideOutput(int depth)
        {
            return OneSideOutput(rightSideInputGUID, depth);
        }

        private float OneSideOutput(string guid, int depth)
        {
            if (fuzzyLogic.IsInferenceGUID(guid))
            {
                // If depth is too deep, assuming it's stackoverflow now, cycle reference is existed.
                if (depth > 10)
                {
                    return -1;
                }
                else
                {
                    return fuzzyLogic.GetInference(guid).Output_Internal(depth);
                }
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
            else
            {
                return 0;
            }
        }
    }
}
