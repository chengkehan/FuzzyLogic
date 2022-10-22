using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace FuzzyLogicSystem
{
    // A Trapezoid
    //      *----*
    //     /      \
    //    /        \
    //   *----------*
    //
    // Degenerate limit to Rectangle
    //      *----*
    //      |    |
    //      |    | 
    //      *----*
    //
    // It means we will never get a up-down flipped trapezoid.
    [Serializable]
    public class TrapezoidFuzzySet
    {
        // belong to this fuzzyLogic
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

        // belong to this fuzzification
        private Fuzzification _fuzzification = null;
        public Fuzzification fuzzification
        {
            set
            {
                _fuzzification = value;
            }
            get
            {
                return _fuzzification;
            }
        }

        // belong to this defuzzification
        // If a fuzzyset is in a defuzzification, it must be in a fuzzification. Both defuzzification and fuzzification are the same one.
        // If a fuzzyset is in a fuzzification, it maybe not in a defuzzification.
        // So we can check this property to validate whether a fuzzyset is in defuzzification.
        public Defuzzification defuzzification
        {
            get
            {
                return fuzzification as Defuzzification;
            }
        }

        // unique id
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

        // color to draw itself
        [SerializeField]
        private Color _color = Color.white;
        public Color color
        {
            set
            {
                _color = value;
            }
            get
            {
                return _color;
            }
        }

        // a readable name for user
        [SerializeField]
        private string _name = string.Empty;
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        //      *-----
        //     /      \
        //    /        \
        [SerializeField]
        private float _peakPointLeftValue = 0;
        public float peakPointLeftValue
        {
            get
            {
                return _peakPointLeftValue;
            }
            set
            {
                if (limitedValue)
                {
                    if (defuzzification == null)
                    {
                        if (fuzzification.IsLeftShoulder(this))
                        {
                            value = fuzzification.MinValue();
                        }
                        else
                        {
                            var prevOne = fuzzification.PreviousTrapezoid(this);
                            value = Mathf.Clamp(value, prevOne.peakPointRightValue, peakPointRightValue);
                        }
                    }
                    else
                    {
                        value = Mathf.Clamp(value, fuzzification.MinValue(), peakPointRightValue);
                    }
                }
                _peakPointLeftValue = value;
            }
        }

        // Only valid in Defuzzification
        // In Fuzzification, this value is equals to peakPointRight of previous TrapzoidFuzzySet
        //      -----
        //     /     \
        //    /       \
        //   *
        [SerializeField]
        private float _footPointLeftValue = 0;
        public float footPointLeftValue
        {
            get
            {
                return _footPointLeftValue;
            }
            set
            {
                if (limitedValue)
                {
                    if (defuzzification == null)
                    {
                        if (fuzzification.IsLeftShoulder(this))
                        {
                            value = fuzzification.MinValue();
                        }
                        else
                        {
                            var prevOne = fuzzification.PreviousTrapezoid(this);
                            value = prevOne.peakPointRightValue;
                        }
                    }
                    else
                    {
                        value = Mathf.Clamp(value, fuzzification.MinValue(), peakPointLeftValue);
                    }
                }
                _footPointLeftValue = value;
            }
        }

        //      ----*
        //     /     \
        //    /       \
        [SerializeField]
        private float _peakPointRightValue = 0;
        public float peakPointRightValue
        {
            get
            {
                return _peakPointRightValue;
            }
            set
            {
                if (limitedValue)
                {
                    if (defuzzification == null)
                    {
                        if (fuzzification.IsRightShoulder(this))
                        {
                            value = fuzzification.MaxValue();
                        }
                        else
                        {
                            var nextOne = fuzzification.NextTrapezoid(this);
                            value = Mathf.Clamp(value, peakPointLeftValue, nextOne.peakPointLeftValue);
                        }
                    }
                    else
                    {
                        value = Mathf.Clamp(value, peakPointLeftValue, fuzzification.MaxValue());
                    }
                }
                _peakPointRightValue = value;
            }
        }

        // Only valid in Defuzzification
        // In Fuzzification, this value is equals to peakPointleft of next TrapzoidFuzzySet
        //      -----
        //     /     \
        //    /       \
        //             *
        [SerializeField]
        private float _footPointRightValue = 0;
        public float footPointRightValue
        {
            get
            {
                return _footPointRightValue;
            }
            set
            {
                if (limitedValue)
                {
                    if (defuzzification == null)
                    {
                        if (fuzzification.IsRightShoulder(this))
                        {
                            value = fuzzification.MaxValue();
                        }
                        else
                        {
                            var nextOne = fuzzification.NextTrapezoid(this);
                            value = nextOne.peakPointLeftValue;
                        }
                    }
                    else
                    {
                        value = Mathf.Clamp(value, peakPointRightValue, fuzzification.MaxValue());
                    }
                }
                _footPointRightValue = value;
            }
        }

        private bool _limitedValue = true;
        public bool limitedValue
        {
            set
            {
                _limitedValue = value;
            }
            get
            {
                return _limitedValue;
            }
        }

        // update realtime
        private float _height = 1;
        public float height
        {
            set
            {
                _height = value;
            }
            get
            {
                return _height;
            }
        }

        public TrapezoidFuzzySet(string guid)
        {
            this.guid = guid;
        }

        public void UpdateFootPointValue()
        {
            if (defuzzification == null)
            {
                footPointLeftValue = 0;
                footPointRightValue = 0;
            }
            else
            {
                footPointLeftValue = footPointLeftValue;
                footPointRightValue = footPointRightValue;
            }
        }

        public Vector2 PeakPointLeftPos(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos)
        {
            AdjustPeakPointByHeight(peakPointLeftValue, footPointLeftValue, out float xValue, out float yValue);
            return ConvertValuesToPos(xValue, yValue, originalPos, xAxisMaxPos, yAxisMaxPos);
        }

        public Vector2 FootPointLeftPos(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos)
        {
            float xValue = footPointLeftValue;
            float yValue = 0;
            return ConvertValuesToPos(xValue, yValue, originalPos, xAxisMaxPos, yAxisMaxPos);
        }

        public Vector2 PeakPointRightPos(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos)
        {
            AdjustPeakPointByHeight(peakPointRightValue, footPointRightValue, out float xValue, out float yValue);
            return ConvertValuesToPos(xValue, yValue, originalPos, xAxisMaxPos, yAxisMaxPos);
        }

        public Vector2 FootPointRightPos(Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos)
        {
            float xValue = footPointRightValue;
            float yValue = 0;
            return ConvertValuesToPos(xValue, yValue, originalPos, xAxisMaxPos, yAxisMaxPos);
        }

        public Vector2 ConvertValuesToPos(float xValue, float yValue, Vector2 originalPos, Vector2 xAxisMaxPos, Vector2 yAxisMaxPos)
        {
            float posX = originalPos.x + (xValue / fuzzification.maxValue) * (xAxisMaxPos.x - originalPos.x);
            float posY = originalPos.y + (yValue / 1.0f) * (yAxisMaxPos.y - originalPos.y);
            return new Vector2(posX, posY);
        }

        // A Trapezoid with height 1
        //      *----* (peakPointValue, 1)         
        //     /      \
        //    /        \
        //   *----------* (footPointValue, 0)
        //
        // A Trapezoid with height 0.5
        //     *------* (o_peakPointValueX, o_peakPointValueY)
        //    /        \
        //   *----------*
        public void AdjustPeakPointByHeight(float peakPointValue, float footPointValue, out float o_peakPointValueX, out float o_peakPointValueY)
        {
            Vector2 v = new Vector2(peakPointValue - footPointValue, 1 - 0);
            v *= height;
            o_peakPointValueX = footPointValue + v.x;
            o_peakPointValueY = v.y;
        }
    }
}