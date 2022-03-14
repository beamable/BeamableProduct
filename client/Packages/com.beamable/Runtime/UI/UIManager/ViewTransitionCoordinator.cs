using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ViewTransitionSystem : MonoBehaviour
{
    
    public Dictionary<string, List<string>> AvailableTransitions;

    
    // ID OF THESE IS = "ID_₢_TransitionName"
    public Dictionary<string, ViewTransitionCoordinator.TransitionMode> ValidTransitionModes;
    public Dictionary<string, ViewTransitionCoordinator.EventSet> TransitionEvents;
    public Dictionary<string, ViewTransitionCoordinator.CoroutineTransitionProfile> CoroutineTransitionProfiles;
    public Dictionary<string, ViewTransitionCoordinator.LegacyAnimationTransitionProfile> LegacyAnimationTransitionProfiles;

}

[Serializable]
public class ViewTransitionCoordinator : MonoBehaviour
{
    [Flags]
    public enum TransitionMode
    {
        Timeline = 1 << 0,
        LegacyAnimation = 1 << 1,
        Animator = 1 << 2,
        Coroutine = 1 << 3,
    }

    public delegate IEnumerator TransitionCoroutineSignature(GameObject controlledGameObject, CoroutineTransitionProfile runningProfile);

    [Serializable]
    public struct LegacyAnimationTransitionProfile
    {
        public AnimationClip AnimationToPlay;
        public float AnimationDelay;
        public EventSet AnimationEvents;
    }

    [Serializable]
    public struct CoroutineTransitionProfile
    {
        // Built via ReflectionCache systems looking into IBeamableUIView types
        public string CoroutineToRun;
        public float CoroutineDelay;
        public TransitionCoroutineSignature Coroutine;
        public EventSet CoroutineEvents;
    }

    [Serializable]
    public struct EventSet
    {
        public UnityEvent OnStartTransition;
        public List<UnityEvent> TriggeredEvents;
        public UnityEvent OnEndTransition;
    }

    public bool IsTransitioning = false;
    
    public List<CoroutineTransitionProfile> AvailableTransitions;
    public int ActiveStateProfileIdx;

    /// <summary>
    /// Fire and forget method to start a transition --- exists here to allow UnityEvents to be used to trigger Transitions. 
    /// </summary>
    public void Transition(string stateName) => StartCoroutine(GoToState(stateName));

    public IEnumerator GoToState(string stateName)
    {
        if (IsTransitioning) yield break;
        
        // var profile = AvailableTransitions.First(sp => sp.Name == stateName);
        // IsTransitioning = true;
        //
        // var coroutines = new List<Coroutine>();
        // if (profile.Appliers.HasFlag(TransitionMode.Coroutine))
        //     coroutines.Add(ControlledScriptHandle.StartCoroutine(RunCoroutineTransition(profile)));
        //
        // if(profile.Appliers.HasFlag(TransitionMode.LegacyAnimation))
        //     coroutines.Add(ControlledScriptHandle.StartCoroutine(RunLegacyAnimationTransition(profile)));
        //
        //
        // yield return coroutines;
        //
        // IEnumerator RunCoroutineTransition(CoroutineTransitionProfile profile)
        // {
        //     yield return new WaitForSeconds(profile.CoroutineDelay);
        //     yield return ControlledScriptHandle.StartCoroutine(profile.Coroutine.Invoke(ControlledScriptHandle.gameObject, profile));
        //     
        //     profile.CoroutineEvents.OnEndTransition.Invoke();
        //     IsTransitioning = false;
        // }
        //
        // IEnumerator RunLegacyAnimationTransition(CoroutineTransitionProfile profile)
        // {
        //     yield return new WaitForSeconds(profile.LegacyAnimationTransitionProfile.AnimationDelay);
        //
        //     var animation = ControlledScriptHandle.GetComponent<Animation>();
        //     if(animation.GetClip(profile.LegacyAnimationTransitionProfile.AnimationToPlay.name) == null)
        //         animation.AddClip(profile.LegacyAnimationTransitionProfile.AnimationToPlay, profile.LegacyAnimationTransitionProfile.AnimationToPlay.name);
        //
        //     animation.Play(profile.LegacyAnimationTransitionProfile.AnimationToPlay.name);
        //
        //     yield return new WaitUntil(() => !animation.isPlaying);
        //
        //     profile.LegacyAnimationTransitionProfile.AnimatorEvents.OnEndTransition.Invoke();
        //     IsTransitioning = false;
        // }
    }

    

    public static IEnumerator CoroutineTransitionA(GameObject controlledGameObject, CoroutineTransitionProfile runningProfile)
    {
        // var name = controlledGameObject.name;
        // yield return new WaitForSeconds(1f);
        // Debug.Log($"Name = {name} / profile = {runningProfile.Name}");
        yield break;
    }
    
    public void TriggerEvent(int index)
    {
        // AvailableTransitions[ActiveStateProfileIdx].AnimatorEvents.TriggeredEvents[index].Invoke();
    }
}
