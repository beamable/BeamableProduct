using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Notifications
{
    /// <summary>
    /// Marshals native callbacks (which may arrive off the main thread) onto Unity's
    /// main thread, where it is safe to raise events and touch the engine. A hidden,
    /// DontDestroyOnLoad GameObject pumps the queue every frame.
    /// </summary>
    internal sealed class Dispatcher : MonoBehaviour
    {
        private static Dispatcher _instance;
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static readonly object _lock = new object();

        internal static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("BeamableNotificationsDispatcher");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<Dispatcher>();
        }

        internal static void Run(Action action)
        {
            if (action == null) return;
            lock (_lock) { _queue.Enqueue(action); }
        }

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (_lock)
                {
                    if (_queue.Count == 0) break;
                    action = _queue.Dequeue();
                }
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
