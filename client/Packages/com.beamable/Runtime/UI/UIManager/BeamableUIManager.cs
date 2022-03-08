using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using UnityEngine;
using UnityEngine.Events;

//using UnityEngine.Timeline;

public class BeamableUIManager : MonoBehaviour
{
    public IReadOnlyList<BeamableView> LoadedViewPrefabs;

    public ViewStateController Test;

    private void Start()
    {
        var component = GetComponent<Animation>();
        component.Play("New");
        Test.ControlledScriptHandle = this;
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


[Serializable]
public class ViewStateController // TODO: Make this name not suck so much....
{
    [Flags]
    public enum StateApplier
    {
        Timeline = 1 << 0,
        LegacyAnimation = 1 << 1,
        Coroutine = 1 << 2,
    }

    public delegate IEnumerator TransitionCoroutineSignature(GameObject controlledGameObject, StateProfile runningProfile);
    
    [Serializable]
    public class StateProfile
    {
        public string Name;

        public StateApplier Appliers;

        //public TimelineAsset TimelineToPlay;
        public float TimelineDelay;
        public StateProfileTransitionEventSet TimelineEvents;
        
        public AnimationClip AnimationToPlay;
        public float AnimationDelay;
        public StateProfileTransitionEventSet AnimatorEvents;
        
        // Built via ReflectionCache systems looking into IBeamableUIView types
        public string CoroutineToRun;
        public float CoroutineDelay;
        public TransitionCoroutineSignature Coroutine;
        public StateProfileTransitionEventSet CoroutineEvents;

        public StateProfileTransitionEventSet TransitionEvents;
    }

    [Serializable]
    public struct StateProfileTransitionEventSet
    {
        public UnityEvent OnStartTransition;
        public List<UnityEvent> TriggeredEvents;
        public UnityEvent OnEndTransition;
    }

    public List<StateProfile> AvailableStates = new List<StateProfile>() { new StateProfile(){Name = "Show", Coroutine = CoroutineTransitionA}, new StateProfile(){Name = "Idle", Coroutine = CoroutineTransitionA}, new StateProfile(){Name = "Hidden", Coroutine = CoroutineTransitionA}};
    public int ActiveStateProfileIdx;

    public MonoBehaviour ControlledScriptHandle;
    public bool IsTransitioning = false;

    public IEnumerator GoToState(string stateName)
    {
        if (IsTransitioning) yield break;
        
        var profile = AvailableStates.First(sp => sp.Name == stateName);
        IsTransitioning = true;
        
        var coroutines = new List<Coroutine>();
        if (profile.Appliers.HasFlag(StateApplier.Coroutine))
            coroutines.Add(ControlledScriptHandle.StartCoroutine(RunCoroutineTransition(profile)));
        
        if(profile.Appliers.HasFlag(StateApplier.LegacyAnimation))
            coroutines.Add(ControlledScriptHandle.StartCoroutine(RunLegacyAnimationTransition(profile)));


        yield return coroutines;

        IEnumerator RunCoroutineTransition(StateProfile profile)
        {
            yield return new WaitForSeconds(profile.CoroutineDelay);
            yield return ControlledScriptHandle.StartCoroutine(profile.Coroutine.Invoke(ControlledScriptHandle.gameObject, profile));
            
            profile.CoroutineEvents.OnEndTransition.Invoke();
            IsTransitioning = false;
        }
        
        IEnumerator RunLegacyAnimationTransition(StateProfile profile)
        {
            yield return new WaitForSeconds(profile.AnimationDelay);

            var animation = ControlledScriptHandle.GetComponent<Animation>();
            if(animation.GetClip(profile.AnimationToPlay.name) == null)
                animation.AddClip(profile.AnimationToPlay, profile.AnimationToPlay.name);

            animation.Play(profile.AnimationToPlay.name);

            yield return new WaitUntil(() => !animation.isPlaying);

            profile.AnimatorEvents.OnEndTransition.Invoke();
            IsTransitioning = false;
        }
    }

    

    public static IEnumerator CoroutineTransitionA(GameObject controlledGameObject, StateProfile runningProfile)
    {
        var name = controlledGameObject.name;
        yield return new WaitForSeconds(1f);
        Debug.Log($"Name = {name} / profile = {runningProfile.Name}");
    }
    
    public void TriggerEvent(int index)
    {
        AvailableStates[ActiveStateProfileIdx].AnimatorEvents.TriggeredEvents[index].Invoke();
    }
}
