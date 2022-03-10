using System.Collections.Generic;
using Beamable.Common;
using UnityEngine;

//using UnityEngine.Timeline;

public class BeamableUIManager : MonoBehaviour
{
    public IReadOnlyList<BeamableViewGroup> LoadedViewPrefabs;

    public ViewTransitionCoordinator Test;

    private void Start()
    {
        var component = GetComponent<Animation>();
        component.Play("New");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(Test.GoToState("Show"));
        if (Input.GetKeyDown(KeyCode.M))
            StartCoroutine(Test.GoToState("Hide"));
        if (Input.GetKeyDown(KeyCode.N))
            StartCoroutine(Test.GoToState("Idle"));
    }


    public void TriggerUnityEventsAtIndex(int index)
    {
        Test.TriggerEvent(index);
    }

    public void Log(int Index)
    {
        Debug.Log($"Called from Index {Index}");
    }
}
