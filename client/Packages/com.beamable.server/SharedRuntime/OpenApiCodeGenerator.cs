using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Editor;
using SharedRuntime;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Beamable.Server.Generator
{
	public class OpenApiCodeGenerator : ClientCodeGenerator
	{

		/// <summary>
		/// Define the class.
		/// </summary>
		/// <param name="descriptor"></param>
		public OpenApiCodeGenerator(OpenApiMicroserviceDescriptor descriptor) : base(descriptor)
		{ }

		protected override void AddFederatedLoginInterfaces()
		{
			// TODO AddFederatedLoginInterfaces
		}

		protected override void AddFederatedInventoryInterfaces()
		{
			// TODO AddFederatedInventoryInterfaces
		}
		
		protected override HashSet<Type> AddMethods()
		{
			// need to scan and get methods.
			var allMethods = ((OpenApiMicroserviceDescriptor)Descriptor).Methods;
			var allParameterTypes = new HashSet<Type>();
			foreach (var method in allMethods)
			{
				AddCallableMethod(method, ref allParameterTypes);
			}

			return allParameterTypes;
		}

		void AddCallableMethod(MicroserviceEndPointInfo info, ref HashSet<Type> parameterTypes)
		{
			// Declaring a ToString method
			CodeMemberMethod genMethod = new CodeMemberMethod();
			genMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			genMethod.Name = info.methodName;

			// the input arguments...
			var serializationFields = new List<string>();
			for (var i = 0; i < info.parameters.Count; i++)
			{
				var methodParam = info.parameters[i];
				var paramType = methodParam.type;
				var paramName = methodParam.name;
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
			genMethod.Comments.Add(new CodeCommentStatement($"Call the {info.methodName} method on the {Descriptor.Name} microservice", true));

			genMethod.Comments.Add(new CodeCommentStatement($"<see cref=\"{GetFullName()}.{info.methodName}\"/>", true));
			genMethod.Comments.Add(new CodeCommentStatement("</summary>", true));

			// the return type needs to be wrapped up inside a Promise.
			var promiseType = typeof(Promise<>);
			var resultType = info.returnType;
			if (resultType == typeof(void) || resultType == typeof(Promise))
			{
				resultType = typeof(Unit);
			}
			else if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Promise<>))
			{
				resultType = resultType.GetGenericArguments()[0];
			}

			// TODO
			// var isAsync = null != info.MethodInfo.GetCustomAttribute<AsyncStateMachineAttribute>();
			// if (isAsync)
			// {
			// 	if (typeof(Task).IsAssignableFrom(resultType) && resultType.IsGenericType)
			// 	{
			// 		// oh, its an async call...
			// 		resultType = resultType.GetGenericArguments()[0];
			// 	}
			// }

			var genericPromiseType = promiseType.MakeGenericType(resultType);
			genMethod.ReturnType = new CodeTypeReference(genericPromiseType);

			// Declaring a return statement for method ToString.
			var returnStatement = new CodeMethodReturnStatement();

			var servicePath = info.callableAttribute.PathName;
			var serviceName = Descriptor.Name;
			if (string.IsNullOrEmpty(servicePath))
			{
				servicePath = info.methodName;
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
	}
}
