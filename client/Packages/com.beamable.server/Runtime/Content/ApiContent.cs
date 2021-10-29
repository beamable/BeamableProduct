using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using UnityEngine;

namespace Beamable.Server
{
   [ContentType("api")]
   [Agnostic]
   [Serializable]
   public class ApiContent : ContentObject, ISerializationCallbackReceiver
   {
      public string Description;

      public ServiceRoute ServiceRoute;

      [ContentField]
      [SerializeField]
      [HideInInspector]
      private RouteVariables _variables = new RouteVariables();

      public ApiVariable[] Variables => _variables.Variables;

      public RouteParameters Parameters;

      protected virtual ApiVariable[] GetVariables()
      {
         return new ApiVariable[] { };
      }

      public void OnBeforeSerialize()
      {
         _variables.Variables = GetVariables();
      }

      public void OnAfterDeserialize()
      {
         // don't do anything special...
      }
   }

   public class ApiVariableBag : Dictionary<string, object>{}

   [Serializable]
   public class ApiVariableReference
   {
      public string Name;
   }

   [Serializable]
   public class ApiVariable
   {
      public string Name;
   }

   [Serializable]
   public class OptionalApiVariableReference : Optional<ApiVariableReference> {}

   [Serializable]
   public class ServiceRoute
   {
      public string Service;
      public string Endpoint;
   }

   [Serializable]
   public class RouteVariables
   {
      public ApiVariable[] Variables;
   }

   [Serializable]
   public class RouteParameters
   {
      // [ContentField]

      public RouteParameter[] Parameters;

      // public void OnBeforeSerialize()
      // {
      //    Variables = GetVariables();
      // }
      //
      // public void OnAfterDeserialize()
      // {
      //
      // }
      //
      // public virtual ApiVariable[] GetVariables()
      // {
      //    return new ApiVariable[] { }; // by default, a route takes no variables.
      // }
   }

   [Serializable]
   public class RouteParameter
   {
      // public int Index;
      public string Name;
      public OptionalApiVariableReference variableReference;
      public OptionalString Data;
   }

   [Serializable]
   public class ApiRef : ContentRef<ApiContent>
   {
      public ApiRef()
      {

      }

      public ApiRef(string id)
      {
         Id = id;
      }
   }

}