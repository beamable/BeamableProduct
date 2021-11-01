using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Content;
using UnityEngine;

namespace Beamable.Server
{
   [ContentType("api")]
   [Agnostic]
   [Serializable]
   public class ApiContent : ContentObject, ISerializationCallbackReceiver
   {
      public OptionalString Description;

      public ServiceRoute ServiceRoute;

      [ContentField]
      [SerializeField]
      private RouteVariables _variables = new RouteVariables();

      public RouteParameters Parameters;

      public ApiVariable[] Variables => _variables.Variables;

      protected virtual ApiVariable[] GetVariables()
      {
         return new ApiVariable[] { };
      }

      public void OnBeforeSerialize()
      {
         _variables.Variables = GetVariables();
         Parameters.ApiContent = this;
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
      public RouteParameter[] Parameters;

      [SerializeField]
      [HideInInspector]
      [IgnoreContentField]
      public ApiContent ApiContent;
   }

   [Serializable]
   public class RouteParameter
   {
      // public int Index;
      public string Name;
      public OptionalApiVariableReference variableReference;
      public string Data;

   }

   [Serializable]
   public class RouteParameter<T> : RouteParameter
   {
      public T TypeData;
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