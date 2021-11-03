using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Common.Player
{

   public interface ISdkEventService
   {
      Promise Add(SdkEvent evt);
      void Process();
      SdkEventConsumer Register(string source, SdkEventHandler handler);
      void Unregister(SdkEventConsumer consumer);
   }

   [System.Serializable]
   public class SdkEventService : ISdkEventService
   {
      private List<SdkEvent> _events = new List<SdkEvent>();

      private Dictionary<string, List<SdkEventConsumer>> _sourceToConsumers =
         new Dictionary<string, List<SdkEventConsumer>>();

      private Dictionary<SdkEvent, Promise> _eventToCompletion =
         new Dictionary<SdkEvent, Promise>();

      public Promise Add(SdkEvent evt)
      {
         _eventToCompletion[evt] = new Promise();
         _events.Add(evt);
         Process();
         return _eventToCompletion[evt];
      }

      public void Process()
      {
         foreach (var evt in _events)
         {
            if (_sourceToConsumers.TryGetValue(evt.Source, out var consumers))
            {
               for (var i = consumers.Count - 1; i >= 0; i--)
               {
                  var promise = consumers[i].Handler?.Invoke(evt);
                  promise?.Merge(_eventToCompletion[evt]);
               }
            }
         }
         _events.Clear();
      }

      public SdkEventConsumer Register(string source, SdkEventHandler handler)
      {
         var consumer = new SdkEventConsumer
         {
            Source = source,
            Handler = handler,
            Service = this
         };
         if (!_sourceToConsumers.TryGetValue(source, out var consumers))
         {
            consumers = new List<SdkEventConsumer>();
         }

         consumers.Add(consumer);
         _sourceToConsumers[source] = consumers;
         return consumer;
      }

      public void Unregister(SdkEventConsumer consumer)
      {
         if (_sourceToConsumers.TryGetValue(consumer.Source, out var consumers))
         {
            consumers.Remove(consumer);
         }
      }
   }

   public delegate Promise SdkEventHandler(SdkEvent evt);

   public class SdkEventConsumer
   {
      public string Source { get; set; }
      public SdkEventHandler Handler { get; set; }
      public SdkEventService Service { get; set; }

      public void Unsubscribe()
      {
         Service.Unregister(this);
      }
   }

   [Serializable]
   public class SdkEvent
   {
      [SerializeField]
      private string _source;

      [SerializeField]
      private string _event;

      [SerializeField]
      private string[] _args;

      public string Source => _source;
      public string Event => _event;
      public string[] Args => _args;

      public SdkEvent()
      {

      }

      public SdkEvent(string source, string evt, params string[] args)
      {
         _source = source;
         _event = evt;
         _args = args;
      }
   }
}