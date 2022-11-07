using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;

namespace FuzzyLogicSystem
{
    [Serializable]
    public class FuzzyLogic
    {
        #region Registered fuzzylogics

        // Store all registered fuzzyLogics here.
        private static List<FuzzyLogic> allRegisteredFuzzyLogics = new List<FuzzyLogic>();

        public static int NumberRegisteredFuzzyLogics()
        {
            return allRegisteredFuzzyLogics.Count;
        }

        public static FuzzyLogic GetRegisteredFuzzyLogic(string guid)
        {
            foreach (var item in allRegisteredFuzzyLogics)
            {
                if (item.guid == guid)
                {
                    return item;
                }
            }
            return null;
        }

        public static FuzzyLogic GetRegisteredFuzzyLogic(int index)
        {
            if (index < 0 || index >= allRegisteredFuzzyLogics.Count)
            {
                return null;
            }
            else
            {
                return allRegisteredFuzzyLogics[index];
            }
        }

        public static bool RegisterFuzzyLogic(FuzzyLogic fuzzyLogic)
        {
            if (fuzzyLogic == null)
            {
                return false;
            }

            if (QueryFuzzyLogic(fuzzyLogic.guid, out var _))
            {
                return false;
            }

            allRegisteredFuzzyLogics.Add(fuzzyLogic);
            return true;
        }

        public static bool UnregisterFuzzyLogic(FuzzyLogic fuzzyLogic)
        {
            if (fuzzyLogic == null)
            {
                return false;
            }

            int index = allRegisteredFuzzyLogics.IndexOf(fuzzyLogic);
            if (index == -1)
            {
                return false;
            }
            else
            {
                allRegisteredFuzzyLogics.RemoveAt(index);
                return true;
            }
        }

        public static void UnregisterAllFuzzyLogics()
        {
            allRegisteredFuzzyLogics.Clear();
        }

        public static bool QueryFuzzyLogic(string guid, out FuzzyLogic fuzzyLogic)
        {
            fuzzyLogic = null;
            foreach (var item in allRegisteredFuzzyLogics)
            {
                if (item.guid == guid)
                {
                    fuzzyLogic = item;
                    return true;
                }
            }
            return false;
        }

        public static bool IsRegisteredFuzzyLogic(string guid)
        {
            return QueryFuzzyLogic(guid, out var _);
        }

        public static bool IsCycleReference(FuzzyLogic fuzzyLogic)
        {
            return IsCycleReference_Internal(fuzzyLogic, fuzzyLogic.guid);
        }

        private static bool IsCycleReference_Internal(FuzzyLogic fuzzyLogic, string guid)
        {
            if (fuzzyLogic == null)
            {
                return false;
            }

            for (int i = 0; i < fuzzyLogic.NumberInferences(); i++)
            {
                var inference = fuzzyLogic.GetInference(i);

                if (IsRegisteredFuzzyLogic(inference.leftSideInputGUID))
                {
                    if (inference.leftSideInputGUID == guid)
                    {
                        return true;
                    }
                    else
                    {
                        if (IsCycleReference_Internal(GetRegisteredFuzzyLogic(inference.leftSideInputGUID), guid))
                        {
                            return true;
                        }
                    }
                }

                if (inference.op == Inference.OP.Or || inference.op == Inference.OP.And)
                {
                    if (IsRegisteredFuzzyLogic(inference.rightSideInputGUID))
                    {
                        if (inference.rightSideInputGUID == guid)
                        {
                            return true;
                        }
                        else
                        {
                            if (IsCycleReference_Internal(GetRegisteredFuzzyLogic(inference.rightSideInputGUID), guid))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Serialize and Deserialize

        private static byte[] _header = null;
        public static byte[] header
        {
            get
            {
                if (_header == null)
                {
                    _header = new byte[] { 0xab, 0xcd, 0xef, 0x12, 0x34, 0x56, 0x78, 0x90 };
                }
                return _header;
            }
        }

        // Using weak-safe header checker rather than hash of SHA to speed up deserialization at runtime.
        public static bool ValidateHeader(byte[] bytes)
        {
            if (bytes == null || bytes.Length < header.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < header.Length; i++)
                {
                    if (header[i] != bytes[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public static byte[] Serialize(FuzzyLogic fuzzyLogic)
        {
            if (fuzzyLogic == null)
            {
                return null;
            }
            else
            {
                string json = JsonUtility.ToJson(fuzzyLogic, true);
                byte[] data = Encoding.Default.GetBytes(json);
                byte[] bytes = new byte[header.Length + data.Length];
                Buffer.BlockCopy(header, 0, bytes, 0, header.Length);
                Buffer.BlockCopy(data, 0, bytes, header.Length, data.Length);
                return bytes;
            }
        }

        public static FuzzyLogic Deserialize(byte[] bytes, FuzzyLogic overwriteFuzzyLogic)
        {
            if (bytes == null || bytes.Length < header.Length)
            {
                return null;
            }
            else
            {
                if (ValidateHeader(bytes) == false)
                {
                    return null;
                }

                byte[] data = new byte[bytes.Length - header.Length];
                Buffer.BlockCopy(bytes, header.Length, data, 0, data.Length);
                string json = Encoding.Default.GetString(data);
                FuzzyLogic fuzzyLogic = overwriteFuzzyLogic;
                if (overwriteFuzzyLogic == null)
                {
                    fuzzyLogic = JsonUtility.FromJson<FuzzyLogic>(json);
                    fuzzyLogic.Initialize();
                }
                else
                {
                    JsonUtility.FromJsonOverwrite(json, overwriteFuzzyLogic);
                    fuzzyLogic.Initialize_Internal();
                }
                return fuzzyLogic;
            }
        }

        #endregion

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

        [SerializeField]
        private List<Fuzzification> fuzzifications = new List<Fuzzification>();

        [SerializeField]
        private Defuzzification _defuzzitication = null;
        public Defuzzification defuzzification
        {
            private set
            {
                _defuzzitication = value;
            }
            get
            {
                return _defuzzitication;
            }
        }

        [SerializeField]
        private List<Inference> inferences = new List<Inference>();

        private bool _initialized = false;
        public bool initialized
        {
            private set
            {
                _initialized = value;
            }
            get
            {
                return _initialized;
            }
        }

        private bool _updatingOutput = false;
        public bool evaluate
        {
            set
            {
                _updatingOutput = value;
            }
            get
            {
                return _updatingOutput;
            }
        }

        [SerializeField]
        private string _guid = null;
        public string guid
        {
            set
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

        public FuzzyLogic(string guid)
        {
            this.guid = guid;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            Initialize_Internal();
        }

        private void Initialize_Internal()
        {
            if (NumberFuzzifications() == 0)
            {
                AddFuzzification();
            }
            if (defuzzification == null)
            {
                defuzzification = new Defuzzification(Guid.NewGuid().ToString(), this);
            }
            if (NumberInferences() == 0)
            {
                AddInference();
            }

            foreach (var fuzzification in fuzzifications)
            {
                InitializeFuzzification(fuzzification);
            }

            InitializeFuzzification(defuzzification);

            foreach (var inference in inferences)
            {
                inference.fuzzyLogic = this;
            }
        }

        public void Update()
        {
            if (evaluate)
            {
                for (int trapezoidI = 0; trapezoidI < defuzzification.NumberTrapezoids(); trapezoidI++)
                {
                    var trapezoid = defuzzification.GetTrapezoid(trapezoidI);
                    trapezoid.height = 1;
                    for (int inferenceI = 0; inferenceI < NumberInferences(); inferenceI++)
                    {
                        var inference = GetInference(inferenceI);
                        if (inference.outputGUID == trapezoid.guid)
                        {
                            trapezoid.height = inference.Output();
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < defuzzification.NumberTrapezoids(); i++)
                {
                    var trapezoid = defuzzification.GetTrapezoid(i);
                    trapezoid.height = 1;
                }
            }
        }

        public float Output()
        {
            var _evaluate = evaluate;
            evaluate = true;
            Update();
            evaluate = _evaluate;
            return defuzzification.OutputValue(out var _, out var _).x / defuzzification.maxValue;
        }

        public bool IsFuzzificationGUID(string guid)
        {
            for (int i = 0; i < NumberFuzzifications(); i++)
            {
                if (GetFuzzification(i).guid == guid)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsFuzzificationTrapezoidGUID(string guid, out Fuzzification o_fuzzification, out TrapezoidFuzzySet o_trapezoid)
        {
            for (int fuzzificationI = 0; fuzzificationI < NumberFuzzifications(); fuzzificationI++)
            {
                var fuzzification = GetFuzzification(fuzzificationI);
                for (int trapezoidI = 0; trapezoidI < fuzzification.NumberTrapezoids(); trapezoidI++)
                {
                    if (fuzzification.GetTrapezoid(trapezoidI).guid == guid)
                    {
                        o_trapezoid = fuzzification.GetTrapezoid(trapezoidI);
                        o_fuzzification = fuzzification;
                        return true;
                    }
                }
            }
            o_trapezoid = null;
            o_fuzzification = null;
            return false;
        }

        public bool IsInferenceGUID(string guid)
        {
            for (int inferenceI = 0; inferenceI < NumberInferences(); inferenceI++)
            {
                if (GetInference(inferenceI).guid == guid)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDefuzzificationGUID(string guid)
        {
            return defuzzification.guid == guid;
        }

        public bool IsDefuzzificationTrapezoidGUID(string guid, out TrapezoidFuzzySet trapezoid)
        {
            for (int trapezoidI = 0; trapezoidI < defuzzification.NumberTrapezoids(); trapezoidI++)
            {
                if (defuzzification.GetTrapezoid(trapezoidI).guid == guid)
                {
                    trapezoid = defuzzification.GetTrapezoid(trapezoidI);
                    return true;
                }
            }
            trapezoid = null;
            return false;
        }

        public void AddInference()
        {
            CheckInitialized();
            var inference = new Inference(Guid.NewGuid().ToString());
            inference.fuzzyLogic = this;
            inferences.Add(inference);
        }

        public void RemoveInference(Inference inference)
        {
            CheckInitialized();
            int index = inferences.IndexOf(inference);
            CheckIndexOfInference(index);
            inferences.Remove(inference);
        }

        public int NumberInferences()
        {
            CheckInitialized();
            return inferences.Count;
        }

        public Inference GetInferenceByName(string name)
        {
            for (int i = 0; i < NumberInferences(); i++)
            {
                if (GetInference(i).name == name)
                {
                    return GetInference(i);
                }
            }
            return null;
        }

        public Inference GetInference(int index)
        {
            CheckInitialized();
            CheckIndexOfInference(index);
            return inferences[index];
        }

        public Inference GetInference(string guid)
        {
            for (int i = 0; i < NumberInferences(); i++)
            {
                if (GetInference(i).guid == guid)
                {
                    return GetInference(i);
                }
            }
            return null;
        }

        public void AddFuzzification()
        {
            CheckInitialized();
            var fuzzification = new Fuzzification(Guid.NewGuid().ToString(), this);
            fuzzifications.Add(fuzzification);
        }

        public void RemoveFuzzification(Fuzzification fuzzification)
        {
            CheckInitialized();
            int index = fuzzifications.IndexOf(fuzzification);
            CheckIndexOfFuzzification(index);
            fuzzifications.Remove(fuzzification);
        }

        public int NumberFuzzifications()
        {
            CheckInitialized();
            return fuzzifications.Count;
        }

        public Fuzzification GetFuzzificationByName(string name)
        {
            for (int i = 0; i < NumberFuzzifications(); i++)
            {
                if (GetFuzzification(i).name == name)
                {
                    return GetFuzzification(i);
                }
            }
            return null;
        }

        public Fuzzification GetFuzzification(int index)
        {
            CheckInitialized();
            CheckIndexOfFuzzification(index);
            return fuzzifications[index];
        }

        public Fuzzification GetFuzzification(string guid)
        {
            for (int i = 0; i < NumberFuzzifications(); i++)
            {
                if (GetFuzzification(i).guid == guid)
                {
                    return GetFuzzification(i);
                }
            }
            return null;
        }

        public int GetFuzzificationIndex(Fuzzification fuzzification)
        {
            CheckInitialized();
            int index = fuzzifications.IndexOf(fuzzification);
            CheckIndexOfFuzzification(index);
            return index;
        }

        private void CheckIndexOfFuzzification(int index)
        {
            if (index < 0 || index >= fuzzifications.Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private void CheckIndexOfInference(int index)
        {
            if (index < 0 || index >= inferences.Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private void CheckInitialized()
        {
            if (initialized == false)
            {
                throw new Exception("FuzzyLogic is not initialized.");
            }
        }

        private void InitializeFuzzification(Fuzzification fuzzification)
        {
            if (fuzzification != null)
            {
                fuzzification.fuzzyLogic = this;
                for (int trapezoidI = 0; trapezoidI < fuzzification.NumberTrapezoids(); trapezoidI++)
                {
                    var trapezoid = fuzzification.GetTrapezoid(trapezoidI);
                    trapezoid.fuzzyLogic = this;
                    trapezoid.fuzzification = fuzzification;
                    trapezoid.height = 1;
                    trapezoid.limitedValue = true;
                }
            }
        }
    }
}
