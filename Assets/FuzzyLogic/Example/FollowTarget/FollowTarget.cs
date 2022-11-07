using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FuzzyLogicSystem;

public class FollowTarget : MonoBehaviour
{
    public Transform target = null;

    public Transform source = null;

    public TextAsset fuzzyLogicData = null;

    private FuzzyLogic fuzzyLogic = null;

    private void Start()
    {
        fuzzyLogic = FuzzyLogic.Deserialize(fuzzyLogicData.bytes, null);
    }

    private void Update()
    {
        fuzzyLogic.evaluate = true;
        fuzzyLogic.GetFuzzificationByName("distance").value = Vector3.Distance(target.position, source.position);

        float speed = fuzzyLogic.Output() * fuzzyLogic.defuzzification.maxValue;
        source.position = Vector3.MoveTowards(source.position, target.position, speed * Time.deltaTime);
    }
}
