using System;
using System.Collections.Generic;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Content;
using UnityEngine;

namespace Beamable.Server
{

   [ContentType("api")]
   public class PlatformApiContent : ContentObject
   {

      [ContentField("serviceType")]
      public PlatformServiceType ServiceType;

      [ContentField("service")]
      public string Service;

      [ContentField("route")]
      public string Route;

      [ContentField("headers")]
      public SerializableDictionaryStringToString Headers;

      [ContentField("body")]
      public string Body;
   }

   public enum PlatformServiceType
   {
      UserMicroservice, ObjectService, BasicService
   }

   [ContentType("api")]
   [Agnostic]
   [Serializable]
   public class ApiContent : ContentObject, ISerializationCallbackReceiver
   {
      [ContentField("description")]
      [Tooltip("Write a summary of this api call")]
      public OptionalString Description;

      [ContentField("method")]
      [HideInInspector]
      public Method Method = Method.POST;

      [ContentField("route")]
      [Tooltip("The route information for the api call")]
      public ServiceRoute ServiceRoute = new ServiceRoute();

      [ContentField("variables")]
      [SerializeField]
      private RouteVariables _variables = new RouteVariables();
      public ApiVariable[] Variables => _variables.Variables;

      [ContentField("parameters")]
      [Tooltip("The required parameters of the api call")]
      public RouteParameters Parameters = new RouteParameters();


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
      public string TypeName;

      public static readonly string TYPE_NUMBER = "number";
      public static readonly string TYPE_BOOLEAN = "bool";
      public static readonly string TYPE_STRING = "string";
      public static readonly string TYPE_OBJECT = "object";

      public static string GetTypeName(Type parameterType)
      {
         switch (Type.GetTypeCode(parameterType))
         {
            case TypeCode.Boolean: return TYPE_BOOLEAN;
            case TypeCode.String: return TYPE_STRING;
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
               return TYPE_NUMBER;
            default: return TYPE_OBJECT;
         }
      }
   }

   [Serializable]
   public class OptionalApiVariableReference : Optional<ApiVariableReference> {}

   [Serializable]
   public class ServiceRoute
   {
      public string Service;
      public string Endpoint;
      public PlatformServiceType Type;
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
      [ContentField("name")]
      [Tooltip("The name of this parameter")]
      public string Name;

      [ContentField("variableRef")]
      [Tooltip("If you are using a variable, which variable is this parameter bound to?")]
      public OptionalApiVariableReference variableReference;

      [ContentField("json")]
      [Tooltip("The raw json payload of this parameter")]
      public string Data;

      [ContentField("parameterType")]
      [Tooltip("The type of this parameter")]
      public string TypeName;
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