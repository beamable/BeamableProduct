using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server;
using Beamable.Platform.SDK;
using UnityEngine;
using System.Text;

namespace Beamable.Server.Editor.CodeGen
{
   public class ClientCodeGenerator
   {
       public MicroserviceDescriptor Descriptor { get; }

       /// <summary>
      /// Define the compile unit to use for code generation.
      /// </summary>
      CodeCompileUnit targetUnit;

      /// <summary>
      /// The only class in the compile unit. This class contains 2 fields,
      /// 3 properties, a constructor, an entry point, and 1 simple method.
      /// </summary>
      private CodeTypeDeclaration targetClass;

      private CodeTypeDeclaration parameterClass;

      private string TargetClassName => $"{Descriptor.Name}Client";
      private string TargetParameterClassName => GetTargetParameterClassName(Descriptor);

      private List<CallableMethodInfo> _callableMethods = new List<CallableMethodInfo>();

      public const string PARAMETER_STRING = "Parameter";
      public const string CLIENT_NAMESPACE = "Beamable.Server.Clients";

      public static string GetTargetParameterClassName(MicroserviceDescriptor descriptor) =>
          $"MicroserviceParameters{descriptor.Name}Client";

      public static string GetParameterClassName(Type parameterType)
      {
		  return $"{PARAMETER_STRING}{GetTypeStr(parameterType).Replace(".","_")}";
      }

      public static Type GetDataWrapperTypeForParameter(MicroserviceDescriptor descriptor, Type parameterType)
      {
          var name =
              $"{CLIENT_NAMESPACE}.{GetTargetParameterClassName(descriptor)}+{GetParameterClassName(parameterType)}, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
          var t = Type.GetType(name, true, true);
          return t;
      }

      private static string GetTypeStr(Type type)
      {
          StringBuilder retType = new StringBuilder();

          if (type.IsGenericType)
          {
              string[] parentType = type.FullName.Split('`');

              Type[] arguments = type.GetGenericArguments();

              StringBuilder argList = new StringBuilder();
              foreach (Type t in arguments)
              {
                  string arg = GetTypeStr(t);
                  if (argList.Length > 0)
                      argList.AppendFormat("_{0}", arg);
                  else
                      argList.Append(arg);
              }

              if (argList.Length > 0)
                  retType.AppendFormat("{0}_{1}", parentType[0], argList.ToString());
          }
          else if (type.IsArray)
          {
              retType.AppendFormat("{0}_{1}", type.BaseType, GetTypeStr(type.GetElementType()));
          }
          else
          {
              return type.ToString();
          }

          return retType.ToString();
      }

      /// <summary>
      /// Define the class.
      /// </summary>
      /// <param name="serviceObject"></param>
      public ClientCodeGenerator(MicroserviceDescriptor descriptor)
      {
          Descriptor = descriptor;
          targetUnit = new CodeCompileUnit();
          CodeNamespace samples = new CodeNamespace(CLIENT_NAMESPACE);

          samples.Imports.Add(new CodeNamespaceImport("System"));
          samples.Imports.Add(new CodeNamespaceImport("Beamable.Platform.SDK"));
          samples.Imports.Add(new CodeNamespaceImport("Beamable.Server"));

          targetClass = new CodeTypeDeclaration(TargetClassName);
          targetClass.IsClass = true;
          targetClass.TypeAttributes =
              TypeAttributes.Public | TypeAttributes.Sealed;
          targetClass.BaseTypes.Add(new CodeTypeReference(typeof(MicroserviceClient)));

          parameterClass = new CodeTypeDeclaration(TargetParameterClassName);
          parameterClass.IsClass = true;
          parameterClass.TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Serializable;
          // parameterClass.BaseTypes.Add();

          targetClass.Comments.Add(new CodeCommentStatement($"<summary> A generated client for <see cref=\"{Descriptor.Type.FullName}\"/> </summary", true));

          samples.Types.Add(targetClass);
          samples.Types.Add(parameterClass);
          targetUnit.Namespaces.Add(samples);

          // need to scan and get methods.
          var allMethods = descriptor.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
          var allParameterTypes = new HashSet<Type>();
          foreach (var method in allMethods)
          {
              var clientCallable = method.GetCustomAttribute<ClientCallableAttribute>();
              if (clientCallable == null)
              {
                  continue;
              }

              var callable = new CallableMethodInfo
              {
                  MethodInfo = method,
                  ClientCallable = clientCallable
              };
              _callableMethods.Add(callable);

              AddCallableMethod(callable, allParameterTypes);
          }

          foreach (var parameterType in allParameterTypes)
          {
              AddParameterClass(parameterType);
          }

      }

      void AddParameterClass(Type parameterType)
      {
          var wrapper = new CodeTypeDeclaration(GetParameterClassName(parameterType));
          wrapper.IsClass = true;
          wrapper.CustomAttributes.Add(
              new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
          wrapper.TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Serializable;
          wrapper.BaseTypes.Add(
              new CodeTypeReference(typeof(MicroserviceClientDataWrapper<>).MakeGenericType(parameterType)));

          parameterClass.Members.Add(wrapper);
      }

      void AddCallableMethod(CallableMethodInfo info, HashSet<Type> parameterTypes)
      {
          // Declaring a ToString method
          CodeMemberMethod genMethod = new CodeMemberMethod();
          genMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
          genMethod.Name = info.MethodInfo.Name;

          // the input arguments...
          var serializationFields = new List<string>();
          var methodParams = info.MethodInfo.GetParameters();
          for (var i = 0; i < methodParams.Length; i++)
          {
              var methodParam = methodParams[i];
              var paramType = methodParam.ParameterType;
              var paramName = methodParam.Name;
              parameterTypes.Add(paramType);
              genMethod.Parameters.Add(new CodeParameterDeclarationExpression(paramType, paramName));

              var serializationFieldName = $"serialized_{paramName}";
              var declare = new CodeParameterDeclarationExpression(typeof(string), serializationFieldName);
              serializationFields.Add(serializationFieldName);

              var serializeInvoke = new CodeMethodInvokeExpression(
                  new CodeMethodReferenceExpression(
                      new CodeThisReferenceExpression(),
                      "SerializeArgument",
                      new CodeTypeReference[]
                      {
                          new CodeTypeReference(paramType),
                      }), new CodeExpression[]
                  {
                    new CodeArgumentReferenceExpression(paramName),
                  });

              var assignment = new CodeAssignStatement(declare, serializeInvoke);
              genMethod.Statements.Add(assignment);
          }


          // add some docstrings to the method.
          genMethod.Comments.Add(new CodeCommentStatement("<summary>", true));
          genMethod.Comments.Add(new CodeCommentStatement($"Call the {info.MethodInfo.Name} method on the {Descriptor.Name} microservice", true));

          genMethod.Comments.Add(new CodeCommentStatement($"<see cref=\"{Descriptor.Type.FullName}.{info.MethodInfo.Name}\"/>", true));
          genMethod.Comments.Add(new CodeCommentStatement("</summary>", true));

          // the return type needs to be wrapped up inside a Promise.
          var promiseType = typeof(Promise<>);
          var resultType = info.MethodInfo.ReturnType;
          if (resultType == typeof(void) || resultType == typeof(Promise))
          {
              resultType = typeof(Unit);
          }
          else if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Promise<>))
          {
              resultType = resultType.GetGenericArguments()[0];
          }

          var isAsync = null != info.MethodInfo.GetCustomAttribute<AsyncStateMachineAttribute>();
          if (isAsync)
          {
              if (typeof(Task).IsAssignableFrom(resultType) && resultType.IsGenericType)
              {
                  // oh, its an async call...
                  resultType = resultType.GetGenericArguments()[0];
              }
          }

          var genericPromiseType = promiseType.MakeGenericType(resultType);
          genMethod.ReturnType = new CodeTypeReference(genericPromiseType);

          // Declaring a return statement for method ToString.
          var returnStatement = new CodeMethodReturnStatement();

          var servicePath = info.ClientCallable.PathName;
          var serviceName = Descriptor.Name;
          if (string.IsNullOrEmpty(servicePath))
          {
              servicePath = info.MethodInfo.Name;
          }

         // servicePath = $"micro_{Descriptor.Name}/{servicePath}"; // micro is the feature name, so we don't accidently stop out an existing service.


          var serializedFieldVariableName = "serializedFields";
          var fieldDeclare = new CodeParameterDeclarationExpression(typeof(string[]), serializedFieldVariableName);
          var fieldReferences = serializationFields.Select(f => new CodeVariableReferenceExpression(f)).ToArray();
          var fieldCreate = new CodeArrayCreateExpression(typeof(string[]), fieldReferences);

          genMethod.Statements.Add(new CodeAssignStatement(fieldDeclare, fieldCreate));

          var requestInvokeExpr = new CodeMethodInvokeExpression(
              new CodeMethodReferenceExpression(
                  new CodeThisReferenceExpression(),
                  "Request",
                  new CodeTypeReference[]
                  {
                      new CodeTypeReference(resultType),
                  }),
              new CodeExpression[]
              {
                  // first argument is the service name
                  new CodePrimitiveExpression(serviceName),

                  // second argument is the path.
                  new CodePrimitiveExpression(servicePath),

                  // third argument is an array of pre-serialized json structures
                  new CodeVariableReferenceExpression(serializedFieldVariableName),
              });

          returnStatement.Expression = requestInvokeExpr;


          //returnStatement.ex
          genMethod.Statements.Add(returnStatement);
          targetClass.Members.Add(genMethod);
      }

      public void GenerateCSharpCode(string fileName)
      {
          CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
          CodeGeneratorOptions options = new CodeGeneratorOptions();
          options.BracingStyle = "C";
          using (StreamWriter sourceWriter = new StreamWriter(fileName))
          {
              provider.GenerateCodeFromCompileUnit(
                  targetUnit, sourceWriter, options);
          }
      }

      public class CallableMethodInfo
        {
            public MethodInfo MethodInfo;
            public ClientCallableAttribute ClientCallable;
        }
   }
}
