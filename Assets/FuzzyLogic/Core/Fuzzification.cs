using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace FuzzyLogicSystem
{
    [Serializable]
    public class Fuzzification
    {
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

        // belong to this fuzzy logic
        private FuzzyLogic _fuzzyLogic = null;
        public FuzzyLogic fuzzyLogic
        {
            set
            {
                _fuzzyLogic = value;
            }
            get
            {
                return _fuzzyLogic;
            }
        }

        [SerializeField]
        private int _division = 10;
        public int division
        {
            set
            {
                _division = value;
            }
            get
            {
                return _division;
            }
        }

        [SerializeField]
        private int _maxValue = 100;
        public int maxValue
        {
            get
            {
                return _maxValue;
            }
            set
            {
                _maxValue = value;
                ClampValue();
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
        protected float _value = 0;
        public virtual float value
        {
            set
            {
                _value = value;
                ClampValue();
            }
            get
            {
                return _value;
            }
        }

        [SerializeField]
        private List<TrapezoidFuzzySet> trapezoids = new List<TrapezoidFuzzySet>();

        // x and y component of each item is a value between minValue and maxValue
        private FlsList<Vector2> _intersections_values = null;
        private FlsList<Vector2> intersections_values
        {
            get
            {
                if (_intersections_values == null)
                {
                    _intersections_values = new FlsList<Vector2>();
                }
                return _intersections_values;
            }
        }

        // trapezoid of each intersection point
        private FlsList<TrapezoidFuzzySet> _intersections_trapezoids = null;
        private FlsList<TrapezoidFuzzySet> intersections_trapezoids
        {
            get
            {
                if (_intersections_trapezoids == null)
                {
                    _intersections_trapezoids = new FlsList<TrapezoidFuzzySet>();
                }
                return _intersections_trapezoids;
            }
        }

        // convert values to positions in coordinate
        private FlsList<Vector2> _intersections_positions = null;
        private FlsList<Vector2> intersections_positions
        {
            get
            {
                if (_intersections_positions == null)
                {
                    _intersections_positions = new FlsList<Vector2>();
                }
                return _intersections_positions;
            }
        }

        public Fuzzification(string guid, FuzzyLogic fuzzyLogic)
        {
            this.guid = guid;
            this.fuzzyLogic = fuzzyLogic;

            var leftShoulder = new TrapezoidFuzzySet(Guid.NewGuid().ToString());
            leftShoulder.fuzzification = this;
            leftShoulder.fuzzyLogic = fuzzyLogic;
            leftShoulder.color = Color.red;
            leftShoulder.limitedValue = false;
            trapezoids.Add(leftShoulder);

            var rightShoulder = new TrapezoidFuzzySet(Guid.NewGuid().ToString());
            rightShoulder.fuzzification = this;
            rightShoulder.fuzzyLogic = fuzzyLogic;
            rightShoulder.color = Color.green;
            rightShoulder.limitedValue = false;
            trapezoids.Add(rightShoulder);

            leftShoulder.peakPointLeftValue = MinValue();
            leftShoulder.footPointLeftValue = MinValue();
            leftShoulder.peakPointRightValue = MinValue() + maxValue * 0.2f;

            rightShoulder.peakPointRightValue = MaxValue();
            rightShoulder.footPointRightValue = MaxValue();
            rightShoulder.peakPointLeftValue = MaxValue() - maxValue * 0.2f;

            // Update automaticllay
            leftShoulder.limitedValue = true;
            rightShoulder.limitedValue = true;
            leftShoulder.UpdateFootPointValue();
            rightShoulder.UpdateFootPointValue();

        }

        public void AddTrapezoid()
        {
            var nextOne = trapezoids[trapezoids.Count - 1];
            var prevOne = trapezoids[trapezoids.Count - 2];

            var trapezoid = new TrapezoidFuzzySet(Guid.NewGuid().ToString());
            trapezoid.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);
            trapezoid.fuzzyLogic = fuzzyLogic;
            trapezoid.fuzzification = this;
            trapezoid.limitedValue = false;
            trapezoids.Insert(trapezoids.Count - 1, trapezoid);

            trapezoid.peakPointLeftValue = prevOne.peakPointRightValue + (nextOne.peakPointLeftValue - prevOne.peakPointRightValue) * 0.5f;
            trapezoid.peakPointRightValue = trapezoid.peakPointLeftValue;
            trapezoid.footPointLeftValue = prevOne.peakPointRightValue;
            trapezoid.footPointRightValue = nextOne.peakPointLeftValue;
            trapezoid.limitedValue = true;
            trapezoid.UpdateFootPointValue();
        }

        public void RemoveTrapezoid(int index)
        {
            CheckIndexOfTrapezoid(index);
            trapezoids.RemoveAt(index);
        }

        public int NumberTrapezoids()
        {
            return trapezoids.Count;
        }

        public TrapezoidFuzzySet GetTrapezoidByName(string name)
        {
            for (int i = 0; i < NumberTrapezoids(); i++)
            {
                if (GetTrapezoid(i).name == name)
                {
                    return GetTrapezoid(i);
                }
            }
            return null;
        }

        public TrapezoidFuzzySet GetTrapezoid(int index)
        {
            CheckIndexOfTrapezoid(index);
            return trapezoids[index];
        }

        public TrapezoidFuzzySet GetTrapezoid(string guid)
        {
            for (int i = 0; i < NumberTrapezoids(); i++)
            {
                if (GetTrapezoid(i).guid == guid)
                {
                    return GetTrapezoid(i);
                }
            }
            return null;
        }

        public bool IsLeftShoulder(TrapezoidFuzzySet target)
        {
            int index = trapezoids.IndexOf(target);
            return index == 0;
        }

        public bool IsRightShoulder(TrapezoidFuzzySet target)
        {
            int index = trapezoids.IndexOf(target);
            return index == trapezoids.Count - 1;
        }

        public TrapezoidFuzzySet PreviousTrapezoid(TrapezoidFuzzySet target)
        {
            int index = trapezoids.IndexOf(target);
            --index;
            CheckIndexOfTrapezoid(index);
            return trapezoids[index];
        }

        public TrapezoidFuzzySet NextTrapezoid(TrapezoidFuzzySet target)
        {
            int index = trapezoids.IndexOf(target);
            ++index;
            CheckIndexOfTrapezoid(index);
            return trapezoids[index];
        }

        // Minimize value on x-axis is 0 normally and maximize value on x-axis is defined by a field named "maxValue".
        // MinExtensionScale is 0 means minimize value on x-axis is 0=>(-maxValue * 0)
        // MinExtensionScale is 0.5 means minimize value on x-axis is (-maxValue * 0.5)
        public virtual float MinExtensionScale()
        {
            return 0;
        }

        public float MinValue()
        {
            return -maxValue * MinExtensionScale();
        }

        // Maximize value on x-axis is defined by a field name "maxValue" and minimize value on x-axis is 0 normally.
        // MaxExtensionScale is 0 means maximize value on x-axis is maxValue=>((1 + 0) * maxValue).
        // MaxExtensionScale is 0.5 means maxmize value on x-axis is ((1 + 0.5) * maxValue)
        public virtual float MaxExtensionScale()
        {
            return 0;
        }

        public float MaxValue()
        {
            return maxValue * (1 + MaxExtensionScale());
        }

        // See comment of TestIntersectionPositionsOfBaseLineAndTrapezoids
        public void TestIntersectionValuesOfBaseLineAndTrapozoids(out Vector2[] o_intersectionValues, out TrapezoidFuzzySet[] o_intersectionTrapezoids)
        {
            intersections_values.Clear();
            intersections_trapezoids.Clear();

            for (int trapezoidI = 0; trapezoidI < NumberTrapezoids(); trapezoidI++)
            {
                var trapezoid = GetTrapezoid(trapezoidI);
                trapezoid.AdjustPeakPointByHeight(trapezoid.peakPointLeftValue, trapezoid.footPointLeftValue, out float o_peakPointLeftValue, out float _);
                trapezoid.AdjustPeakPointByHeight(trapezoid.peakPointRightValue, trapezoid.footPointRightValue, out float o_peakPointRightValue, out float _);
                if (value >= trapezoid.footPointLeftValue && value < o_peakPointLeftValue)
                { 
                    TestIntersectionValues(o_peakPointLeftValue, trapezoid.footPointLeftValue, value, trapezoid.height, out float intersectionValueX, out float intersectionValueY);
                    intersections_values.Add(new Vector2(intersectionValueX, intersectionValueY));
                    intersections_trapezoids.Add(trapezoid);
                }
                else if (value > o_peakPointRightValue && value <= trapezoid.footPointRightValue)
                {
                    TestIntersectionValues(o_peakPointRightValue, trapezoid.footPointRightValue, value, trapezoid.height, out float intersectionValueX, out float intersectionValueY);
                    intersections_values.Add(new Vector2(intersectionValueX, intersectionValueY));
                    intersections_trapezoids.Add(trapezoid);
                }
                else if (value >= o_peakPointLeftValue && value <= o_peakPointRightValue)
                {
                    intersections_values.Add(new Vector2(value, trapezoid.height));
                    intersections_trapezoids.Add(trapezoid);
                }
                else
                {
                    // No intersection
                }
            }

            o_intersectionValues = intersections_values.ToArray();
            o_intersectionTrapezoids = intersections_trapezoids.ToArray();
        }

        //       *--* (peakPointValue, height)
        //      /    \ 
        //     /      * (intersectionValueX, intersectionValueY)
        //    /       |\
        //   *--------*-* (footPointValue, 0)
        //        (value, 0)
        private void TestIntersectionValues(float peakPointValue, float footPointValue, float value, float height, out float intersectionValueX, out float intersectionValueY)
        {
            Vector2 v = new Vector2(peakPointValue - footPointValue, height - 0);
            v *= (value - footPointValue) / (peakPointValue - footPointValue);
            intersectionValueX = footPointValue + v.x;
            intersectionValueY = v.y;
        }

        //  ^        | 
        //  |-----   |-----
        //  |     \  *a   |
        //  |      \/|    |
        //  |      /\|    |
        //  |     /  *b   |
        //  |    /   |\   |
        //  o--------*------->
        //        value
        // BaseLine start at the position of value and end to infinite upward direction.
        // This function is calculating all intersections of baseline and trapezoids.
        // In this figure, is a and b.
        // Before invoke this function, you should invoke TestIntersectionValuesOfBaseLineAndTrapozoids firstly.
        // This function is just convert values to positions.
        public void TestIntersectionPositionsOfBaseLineAndTrapezoids(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos, out Vector2[] intersectionPositions, out TrapezoidFuzzySet[] intersectionTrapezoids)
        {
            intersections_positions.Clear();
            for (int i = 0; i < intersections_values.size; i++)
            {
                intersections_positions.Add(trapezoids[0].ConvertValuesToPos(intersections_values[i].x, intersections_values[i].y, originalPos, xAxisMaxPos, yAxisMaxPos));
            }
            intersectionPositions = intersections_positions.ToArray();
            intersectionTrapezoids = intersections_trapezoids.ToArray();
        }

        //  ^        | 
        //  |        *pEnd
        //  |        | 
        //  |        |    
        //  |        |    
        //  |        |   
        //  |        |   
        //  o--------*------->
        //        value(pStart)
        public void BaseLinePositions(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos, out Vector2 pStart, out Vector2 pEnd)
        {
            pStart = trapezoids[0].ConvertValuesToPos(value, 0, originalPos, xAxisMaxPos, yAxisMaxPos);
            pEnd = trapezoids[0].ConvertValuesToPos(value, 1, originalPos, xAxisMaxPos, yAxisMaxPos);
        }

        private void CheckIndexOfTrapezoid(int index)
        {
            if (index < 0 || index >= trapezoids.Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private void ClampValue()
        {
            _value = Mathf.Clamp(value, 0, maxValue);
        }
    }
}
