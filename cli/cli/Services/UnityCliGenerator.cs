using Beamable.Common.Announcements;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using Beamable.Common.Groups;
using Beamable.Common.Inventory;
using Beamable.Common.Leaderboards;
using Beamable.Common.Shop;
using Beamable.Common.Tournaments;
using Beamable.Experimental.Common.Calendars;
using Beamable.Server.Common;
using cli.Unreal;
using System.CodeDom;
using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Beamable.Server;

namespace cli.Services;

public class CliGeneratorContext
{
	public List<BeamCommandDescriptor> Commands;
	public BeamCommandDescriptor Root;
}

public interface ICliGenerator
{
	List<GeneratedFileDescriptor> Generate(CliGeneratorContext context);
}

public class UnityCliGenerator : ICliGenerator
{
	public List<GeneratedFileDescriptor> Generate(CliGeneratorContext context)
	{
		// the following commands are more complicated and either use nullables or enums
		var invalidCommands = new string[]
		{
			"beam services register",
			"beam services modify",
			"beam services enable",
			"beam oapi generate",
		};

		var files = new List<GeneratedFileDescriptor>();
		foreach (var command in context.Commands)
		{
			if (!command.hasValidOutput && command.executionPath != "beam") continue;
			if (invalidCommands.Contains(command.executionPath)) continue;
			files.Add(Generate(command));
		}

		// var me = context.Commands.First(x => x.executionPath == "beam me");
		// var resultTypes = RecurseTypes(me.resultStreams.Select(x => x.runtimeType));
		var resultTypes = RecurseTypes(context.Commands.SelectMany(command => command.resultStreams.Select(s => s.runtimeType)));

		foreach (var resultType in resultTypes)
		{
			if (resultType.Namespace.StartsWith("Beamable.Common"))
			{
				Log.Debug("skipping type " + resultType.FullName);
				continue;
			}

			var unit = new CodeCompileUnit();
			var root = new CodeNamespace("Beamable.Editor.BeamCli.Commands");
			root.Imports.Add(new CodeNamespaceImport("Beamable.Common"));
			root.Imports.Add(new CodeNamespaceImport("Beamable.Common.BeamCli"));
			unit.Namespaces.Add(root);

			var decl = GenerateResultStreamType(resultType);
			root.Types.Add(decl);
			var srcCode = UnityHelper.GenerateCsharp(unit);

			var fileName = ConvertToSnakeCase(decl.Name) + ".cs";
			files.Add(new GeneratedFileDescriptor
			{
				Content = srcCode,
				FileName = fileName
			});
		}

		var output = new List<GeneratedFileDescriptor>();
		output.AddRange(files);
		output.AddRange(GenerateMetaFiles(files));

		return output;
	}

	public static List<GeneratedFileDescriptor> GenerateMetaFiles(List<GeneratedFileDescriptor> sourceFiles) =>
		GenerateMetaFiles(sourceFiles.Select(x => x.FileName).ToList());
	
	const string GUID_TEMPLATE = "{GUID_REPLACE}";
	// TODO: do we need to update these meta file generations for future versions of Unity?
	const string META_CONTENT_TEMPLATE = @"fileFormatVersion: 2
guid: " + GUID_TEMPLATE + @"
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

	public static readonly Dictionary<string, string> sourceToHardcodedGuid = new Dictionary<string, string>
	{
		[nameof(ContentObject)] = "c658d5adea78044eb8b9a850f7830051",
		[nameof(LeaderboardContent)] = "ebf776d7c945048daab1e231d27dcdeb",
		[nameof(AnnouncementContent)] = "8b8612187b1a246e5b691c90486deb34",
		[nameof(AnnouncementApiContent)] = "4d6d527a776e4dad8b86964e1a98e8b0",
		[nameof(CalendarContent)] = "0c222bf6b195348e5be37e5cf0169f08",
		[nameof(GroupDonationsContent)] = "fb76ecebaba24fcd9f9c681e977213c1",
		[nameof(CurrencyContent)] = "7071e886d4789435d9ad361e916b2207",
		[nameof(ItemContent)] = "4e65df06d8f8e499788b3ed5b359d54a",
		[nameof(VipContent)] = "d9d0bb41924546a4fb9bf0beb0a1f7ee",
		[nameof(EmailContent)] = "b5d38e7e955cc47f9933ec9a3a1e2c5a",
		[nameof(EventContent)] = "11f962489e5a44d7c877b4a5b71f4d0e",
		[nameof(SimGameType)] = "404f8a85a397642f5b56ce1f2d8cebea",
		[nameof(ListingContent)] = "14ae295abc57d4046906c98c15f3ea23",
		[nameof(SKUContent)] = "b5bfeeca7cf3246b3a89418e98719de7",
		[nameof(StoreContent)] = "a658558ea86c749979b310e828e3892e",
		[nameof(TournamentContent)] = "6da5f82a5df104fd4b67a842fafa5c5a",
		[nameof(ApiContent)] = "3c1cf2c6cf6d4c0c91cc218a5b733932",
	};
	
	public static List<GeneratedFileDescriptor> GenerateMetaFiles(List<string> sourceFiles)
	{
		var metas = new List<GeneratedFileDescriptor>(sourceFiles.Count);

		using var md5 = MD5.Create();
		foreach (var sourceFile in sourceFiles)
		{

			if (!sourceToHardcodedGuid.TryGetValue(Path.GetFileNameWithoutExtension(sourceFile), out var metaGuid))
			{
				var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sourceFile));
				var guid = new Guid(hashedBytes);
				metaGuid = guid.ToString().Replace("-", "");
			}
			
			metas.Add(new GeneratedFileDescriptor
			{
				FileName = sourceFile + ".meta",
				Content = META_CONTENT_TEMPLATE.Replace(GUID_TEMPLATE, metaGuid)
			});
		}
		return metas;
	}

	public static GeneratedFileDescriptor GenerateMetaFile(string sourceFile)
	{
		using var md5 = MD5.Create();
		{
			if (!UnityCliGenerator.sourceToHardcodedGuid.TryGetValue(Path.GetFileNameWithoutExtension(sourceFile),
				    out var metaGuid))
			{
				var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sourceFile));
				var guid = new Guid(hashedBytes);
				metaGuid = guid.ToString().Replace("-", "");
			}
			
			return new GeneratedFileDescriptor
			{
				FileName = sourceFile + ".meta",
				Content = META_CONTENT_TEMPLATE.Replace(GUID_TEMPLATE, metaGuid)
			};
		}
	}

	public static List<Type> RecurseTypes(IEnumerable<Type> inputTypes, bool includeTypesFromInvalidAssembly = false)
	{
		var output = new HashSet<Type>();
		var toExplore = new Queue<Type>();
		foreach (var inputType in inputTypes)
		{
			// Log.Information("Looking at type : " + inputType.Name);
			toExplore.Enqueue(inputType);
		}

		var validAssembly = typeof(UnityCliGenerator).Assembly;
		while (toExplore.Count > 0)
		{
			var curr = toExplore.Dequeue();
			if (output.Contains(curr)) continue; // we have already seen this type.

			var assembly = curr.Assembly;
			if (assembly != validAssembly)
			{
				if (!includeTypesFromInvalidAssembly)
				{
					continue; // skip this type, it cannot be generated.
				}

				if (curr.IsPrimitive) continue;
				if (curr == typeof(string)) continue;
				if (curr.IsArray) continue;
				if (curr.IsGenericType && curr.GetGenericTypeDefinition() == typeof(Nullable<>)) continue;
				if (curr.IsGenericType && curr.GetGenericTypeDefinition() == typeof(List<>)) continue;
				if (curr.IsGenericType && curr.GetGenericTypeDefinition() == typeof(Dictionary<,>)) continue;
			}
			var fields = UnityJsonContractResolver.GetSerializedFields(curr);
			foreach (var field in fields)
			{
				toExplore.Enqueue(field.FieldType);
				if (field.FieldType.IsGenericType)
				{
					var genericArgs = field.FieldType.GetGenericArguments();
					foreach (var genericArg in genericArgs)
					{
						toExplore.Enqueue(genericArg);
					}
				}

				if (field.FieldType.IsArray)
				{
					toExplore.Enqueue(field.FieldType.GetElementType());
				}
			}
			output.Add(curr);
		}

		return output.ToList();
	}

	public static GeneratedFileDescriptor Generate(BeamCommandDescriptor command)
	{
		var unit = new CodeCompileUnit();
		var root = new CodeNamespace("Beamable.Editor.BeamCli.Commands");
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common"));
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common.BeamCli"));
		unit.Namespaces.Add(root);

		root.Types.Add(GenerateArgType(command));
		root.Types.Add(GenerateCodeType(command));
		root.Types.Add(GenerateReturnType(command));


		var srcCode = UnityHelper.GenerateCsharp(unit);
		var fileName = ConvertToSnakeCase(command.executionPath) + ".cs";
		return new GeneratedFileDescriptor { Content = srcCode, FileName = fileName.Capitalize() };
	}

	public static string GetArgClassName(BeamCommandDescriptor descriptor)
	{
		return ConvertToSnakeCase(descriptor.ExecutionPathAsCapitalizedStringWithoutBeam() + "Args");
	}

	public static string GetReturnClassName(BeamCommandDescriptor descriptor)
	{
		return ConvertToSnakeCase(descriptor.ExecutionPathAsCapitalizedStringWithoutBeam() + "Wrapper");
	}

	public static CodeTypeReference GetFieldReference(Type runtimeType)
	{
		if (runtimeType.Assembly == typeof(UnityCliGenerator).Assembly)
		{
			var type = new CodeTypeReference(GetResultClassName(runtimeType));
			if (runtimeType.IsArray)
			{
				type = new CodeTypeReference(type, 1);
			}

			return type;
		}

		if (runtimeType.IsGenericType)
		{
			var def = runtimeType.GetGenericTypeDefinition();
			if (def.IsAssignableFrom(typeof(List<>)))
			{
				var genArg = runtimeType.GetGenericArguments()[0];
				var elementReference = GetFieldReference(genArg);
				var listReference = new CodeTypeReference(typeof(List<>));
				listReference.TypeArguments.Add(elementReference);
				return listReference;
				// return new CodeTypeReference(elementReference, 1);
			}
		}

		return new CodeTypeReference(runtimeType);
	}

	public static string GetResultClassName(Type runtimeType)
	{
		if (runtimeType.Namespace.StartsWith("Beamable.Common"))
		{
			return runtimeType.FullName;
		}

		var name = runtimeType.Name.Replace("[]", "");
		
		return ConvertToSnakeCase("Beam" + name);
	}


	public static CodeTypeDeclaration GenerateArgType(BeamCommandDescriptor descriptor)
	{
		const string argsVar = "genBeamCommandArgs";
		const string strVar = "genBeamCommandStr";


		var name = GetArgClassName(descriptor);
		var type = new CodeTypeDeclaration(name)
		{
			IsPartial = true
		};
		type.BaseTypes.Add(new CodeTypeReference(typeof(IBeamCommandArgs)));


		var method = new CodeMemberMethod
		{
			Name = nameof(IBeamCommandArgs.Serialize),
			ReturnType = new CodeTypeReference(typeof(string)),
			Attributes = MemberAttributes.Public,
			Comments =
			{
				new CodeCommentStatement(new CodeComment("<summary>Serializes the arguments for command line usage.</summary>", true))
			}
		};
		type.Members.Add(method);

		var argReference = new CodeVariableReferenceExpression(argsVar);
		var createArgListStatement = new CodeVariableDeclarationStatement(typeof(List<string>), argsVar, new CodeObjectCreateExpression(typeof(List<string>)));
		method.Statements.Add(new CodeCommentStatement("Create a list of arguments for the command"));
		method.Statements.Add(createArgListStatement);


		try
		{
			foreach (var arg in descriptor.command.Arguments)
			{
				var parameter = CreateParameter(arg);
				var fieldDecl = new CodeMemberField(parameter.Type, parameter.Name);
				fieldDecl.Comments.Add(new CodeCommentStatement($"<summary>{arg.Description}</summary>", true));
				fieldDecl.Attributes = MemberAttributes.Public;
				type.Members.Add(fieldDecl);
				var parameterReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldDecl.Name);

				var valueIsNotNullExpr = new CodeBinaryOperatorExpression(parameterReference,
					CodeBinaryOperatorType.IdentityInequality,
					new CodeDefaultValueExpression(parameter.Type));
				var conditional = new CodeConditionStatement(valueIsNotNullExpr);
				var toStringedParameter = new CodeMethodInvokeExpression(parameterReference, nameof(string.ToString));
				var addStatement =
					new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), toStringedParameter);
				conditional.TrueStatements.Add(addStatement);

				if (parameter.CustomAttributes.Count > 0)
				{
					method.Statements.Add(new CodeCommentStatement(
						$"If the {parameter.Name} value was not default, then add it to the list of args."));
					method.Statements.Add(conditional);
				}
				else
				{
					method.Statements.Add(
						new CodeCommentStatement($"Add the {parameter.Name} value to the list of args."));
					method.Statements.Add(addStatement);
				}
			}

			foreach (var option in descriptor.command.Options)
			{
				var parameter = CreateParameter(option);
				var fieldDecl = new CodeMemberField(parameter.Type, parameter.Name);
				fieldDecl.Comments.Add(new CodeCommentStatement($"<summary>{option.Description}</summary>", true));
				fieldDecl.Attributes = MemberAttributes.Public;

				type.Members.Add(fieldDecl);

				var parameterReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldDecl.Name);

				var valueIsNotNullExpr = new CodeBinaryOperatorExpression(parameterReference,
					CodeBinaryOperatorType.IdentityInequality,
					new CodeDefaultValueExpression(parameter.Type));
				var conditional = new CodeConditionStatement(valueIsNotNullExpr);
				CodeMethodInvokeExpression addStatement;
				
				var optionalVal =
					new CodeBinaryOperatorExpression(new CodePrimitiveExpression("--" + option.Name + "="),
						CodeBinaryOperatorType.Add, parameterReference);
				addStatement =
					new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), optionalVal);
				

				if (option.AllowMultipleArgumentsPerToken || option.ValueType.GetElementType() != null)
				{
					var loopInitExpr =
						new CodeVariableDeclarationStatement(typeof(int), "i", new CodePrimitiveExpression(0));
					var loopRef = new CodeVariableReferenceExpression("i");
					var loopTestExpr = new CodeBinaryOperatorExpression(loopRef, CodeBinaryOperatorType.LessThan,
						new CodeFieldReferenceExpression(parameterReference, "Length"));
					var loopIncExpr = new CodeAssignStatement(loopRef,
						new CodeBinaryOperatorExpression(loopRef, CodeBinaryOperatorType.Add,
							new CodePrimitiveExpression(1)));
					var loopStatement = new CodeIterationStatement(loopInitExpr, loopTestExpr, loopIncExpr);

					conditional.TrueStatements.Add(loopStatement);

					var arrayIndexValue = new CodeArrayIndexerExpression(parameterReference, loopRef);
					var optionalArrVal =
						new CodeBinaryOperatorExpression(new CodePrimitiveExpression("--" + option.Name + "="),
							CodeBinaryOperatorType.Add, arrayIndexValue);
					var addArrStatement =
						new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), optionalArrVal);

					loopStatement.Statements.Add(new CodeCommentStatement("The parameter allows multiple values"));
					loopStatement.Statements.Add(addArrStatement);
				}
				else
				{

					conditional.TrueStatements.Add(addStatement);
				}

				if (parameter.CustomAttributes.Count > 0)
				{
					method.Statements.Add(new CodeCommentStatement(
						$"If the {parameter.Name} value was not default, then add it to the list of args."));
					method.Statements.Add(conditional);
				}
				else
				{
					method.Statements.Add(
						new CodeCommentStatement($"Add the {parameter.Name} value to the list of args."));
					method.Statements.Add(addStatement);
				}
			}

		}
		catch (UnityCliGenerationException ex)
		{
			Log.Error($"path=[{descriptor.executionPath}] message=[{ex.Message}]");
			throw;
		}


		var createStrStatement = new CodeVariableDeclarationStatement(typeof(string), strVar,
			new CodePrimitiveExpression(""));
		var strReference = new CodeVariableReferenceExpression(strVar);
		method.Statements.Add(createStrStatement);

		var joinStatement = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),
			nameof(string.Join), new CodePrimitiveExpression(" "), argReference);
		var strAssignment = new CodeAssignStatement(strReference, joinStatement);
		method.Statements.Add(new CodeCommentStatement("Join all the args with spaces"));
		method.Statements.Add(strAssignment);

		var returnStatement = new CodeMethodReturnStatement(strReference);
		method.Statements.Add(returnStatement);

		return type;
	}

	public static CodeTypeDeclaration GenerateResultStreamType(Type runtimeType)
	{
		var type = new CodeTypeDeclaration
		{
			Name = GetResultClassName(runtimeType),
#pragma warning disable SYSLIB0050
			TypeAttributes = TypeAttributes.Serializable | TypeAttributes.Public,
#pragma warning restore SYSLIB0050
			CustomAttributes = new CodeAttributeDeclarationCollection
			{
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute)))
			},
			IsPartial = true
		};

		foreach (var field in UnityJsonContractResolver.GetSerializedFields(runtimeType))
		{
			var fieldDecl = new CodeMemberField
			{
				Name = field.Name,
				Type = GetFieldReference(field.FieldType),
				Attributes = MemberAttributes.Public
			};
			type.Members.Add(fieldDecl);

		}

		return type;
	}

	public static CodeTypeDeclaration GenerateReturnType(BeamCommandDescriptor descriptor)
	{
		var type = new CodeTypeDeclaration
		{
			IsPartial = true,
			Name = GetReturnClassName(descriptor),
			BaseTypes =
			{
				new CodeTypeReference(typeof(BeamCommandWrapper))
			}
		};

		foreach (var result in descriptor.resultStreams)
		{
			var callbackName = $"On{result.channel.Capitalize()}{result.runtimeType.Name}";
			if (result.runtimeType.IsSubclassOf(typeof(ErrorOutput)))
			{
				callbackName = $"On{result.channel.Capitalize()}";
			}
			var method = new CodeMemberMethod
			{
				Name = callbackName,
				Attributes = MemberAttributes.Public,
				ReturnType = new CodeTypeReference(type.Name)
			};

			var argTypeRef = new CodeTypeReference($"System.Action<ReportDataPoint<{GetResultClassName(result.runtimeType)}>>");
			method.Parameters.Add(new CodeParameterDeclarationExpression { Name = "cb", Type = argTypeRef });

			var commandRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), nameof(BeamCommandWrapper.Command));
			var methodInvocStatement =
				new CodeMethodInvokeExpression(commandRef, "On", new CodePrimitiveExpression(result.channel), new CodeVariableReferenceExpression("cb"));
			method.Statements.Add(methodInvocStatement);

			var returnStatement = new CodeMethodReturnStatement(new CodeThisReferenceExpression());
			method.Statements.Add(returnStatement);


			type.Members.Add(method);
		}

		return type;
	}

	public static CodeTypeDeclaration GenerateCodeType(BeamCommandDescriptor descriptor)
	{
		const string strVar = "genBeamCommandStr";
		const string argsVar = "genBeamCommandArgs";
		const string wrapperVar = "genBeamCommandWrapper";

		var type = new CodeTypeDeclaration("BeamCommands")
		{
			IsPartial = true
		};

		var method = new CodeMemberMethod
		{
			Name = ConvertToSnakeCase(descriptor.ExecutionPathAsCapitalizedStringWithoutBeam()),
			ReturnType = new CodeTypeReference(GetReturnClassName(descriptor)),
			Attributes = MemberAttributes.Public
		};

		var createArgListStatement = new CodeVariableDeclarationStatement(typeof(List<string>), argsVar, new CodeObjectCreateExpression(typeof(List<string>)));
		var argReference = new CodeVariableReferenceExpression(argsVar);
		method.Statements.Add(new CodeCommentStatement("Create a list of arguments for the command"));
		method.Statements.Add(createArgListStatement);


		var statementCollection = new CodeStatementCollection();
		var curr = descriptor;
		while (curr != null)
		{
			var argClassName = GetArgClassName(curr);
			var commandName = curr.command.Name.Replace("Beamable.Tools", "beam");
			var parameter =
				new CodeParameterDeclarationExpression(argClassName,
					(ConvertToSnakeCase(commandName) + "Args").UnCapitalize());



			if (curr.command.Arguments.Count > 0 || curr.command.Options.Count > 0)
			{
				var parameterReference = new CodeVariableReferenceExpression(parameter.Name);
				if (parameter.Name != "beamArgs")
				{
					method.Parameters.Insert(0, parameter);
				}
				else
				{
					parameterReference = new CodeVariableReferenceExpression("defaultBeamArgs");
				}
				var serializeStatement =
					new CodeMethodInvokeExpression(parameterReference, nameof(IBeamCommandArgs.Serialize));
				var addArgsStatement = new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), serializeStatement);
				statementCollection.Insert(0, new CodeExpressionStatement(addArgsStatement));

			}
			var addPathStatement = new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), new CodePrimitiveExpression(commandName));
			statementCollection.Insert(0, new CodeExpressionStatement(addPathStatement));



			curr = curr.parent;
		}

		method.Statements.AddRange(statementCollection);


		var factoryReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_factory");
		var createMethodCall = new CodeMethodInvokeExpression(factoryReference, nameof(IBeamCommandFactory.Create));
		var instanceAssignment =
			new CodeVariableDeclarationStatement(typeof(IBeamCommand), "command", createMethodCall);
		var instanceReference = new CodeVariableReferenceExpression("command");
		method.Statements.Add(new CodeCommentStatement("Create an instance of an IBeamCommand"));
		method.Statements.Add(instanceAssignment);

		var joinStatement = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),
			nameof(string.Join), new CodePrimitiveExpression(" "), argReference);
		var createStrStatement = new CodeVariableDeclarationStatement(typeof(string), strVar,
			joinStatement);
		var strReference = new CodeVariableReferenceExpression(strVar);
		method.Statements.Add(new CodeCommentStatement("Join all the command paths and args into one string"));
		method.Statements.Add(createStrStatement);

		var setCommandMethodCall = new CodeMethodInvokeExpression(instanceReference, nameof(IBeamCommand.SetCommand), strReference);
		method.Statements.Add(new CodeCommentStatement("Configure the command with the command string"));
		method.Statements.Add(setCommandMethodCall);

		var wrapperCreationStatement = new CodeVariableDeclarationStatement(GetReturnClassName(descriptor), wrapperVar, new CodeObjectCreateExpression(GetReturnClassName(descriptor)));
		var wrapperReference = new CodeVariableReferenceExpression(wrapperVar);
		var setWrapperStatement = new CodeAssignStatement(new CodeFieldReferenceExpression(wrapperReference, nameof(BeamCommandWrapper.Command)),
			instanceReference);
		// var setCommandStatement = new CodeMethodInvokeExpression(wrapperReference, nameof(BeamCommandWrapper))


		method.Statements.Add(wrapperCreationStatement);
		method.Statements.Add(setWrapperStatement);


		var returnStatement = new CodeMethodReturnStatement(wrapperReference);
		method.Statements.Add(new CodeCommentStatement("Return the command!"));
		method.Statements.Add(returnStatement);

		type.Members.Add(method);

		// generate result stream methods...
		// foreach (var result in descriptor.resultStreams)
		// {
		// 	var resultMethod = CreateResultStreamMethod(descriptor, result);
		// 	type.Members.Add(resultMethod);
		// }


		return type;
	}


	public static void AddDefaultValue(CodeParameterDeclarationExpression parameter, CodeTypeReference type)
	{
		parameter.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DefaultParameterValueAttribute)),
				new CodeAttributeArgument(
					new CodeDefaultValueExpression(type))));
		parameter.CustomAttributes.Add(
			new CodeAttributeDeclaration(new CodeTypeReference(typeof(OptionalAttribute))));
	}

	public static CodeParameterDeclarationExpression CreateParameter(Argument arg)
	{
		var name = ConvertToSnakeCase(arg.Name);
		var parameter = new CodeParameterDeclarationExpression { Type = new CodeTypeReference(arg.ValueType), Name = name };

		if (arg.HasDefaultValue)
		{
			AddDefaultValue(parameter, parameter.Type);

			parameter.UserData["desc"] = $"(default={arg?.GetDefaultValue()?.ToString()}) ";

		}
		parameter.UserData["desc"] += arg.Description;
		return parameter;
	}


	public static CodeParameterDeclarationExpression CreateParameter(Option option)
	{
		if (option.IsRequired) throw new UnityCliGenerationException($"required options are not supported name=[{option.Name}]");

		var name = ConvertToSnakeCase(option.Name);
		var type = new CodeTypeReference(option.ValueType);
		if (option.AllowMultipleArgumentsPerToken)
		{
			type.ArrayRank = 1;
		}
		if (option.AllowMultipleArgumentsPerToken && option.ValueType.IsAssignableTo(typeof(IEnumerable<string>)))
		{
			type = new CodeTypeReference(typeof(string));
			type.ArrayRank = 1;
		}
		var parameter = new CodeParameterDeclarationExpression(type, name);
		var field = typeof(Option).GetField("_argument", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null) throw new InvalidOperationException("must have default value arg");
		var internalArg = field?.GetValue(option) as Argument;
		if (internalArg?.HasDefaultValue ?? false)
		{
			parameter.UserData["desc"] = option.IsRequired ? "" : $"(default={internalArg?.GetDefaultValue()?.ToString()}) ";
		}
		parameter.UserData["desc"] += option.Description;
		AddDefaultValue(parameter, type);

		return parameter;
	}


	/// <summary>
	/// Written by ChatGPT
	/// This function splits the input string into an array of words using the "-" character as a separator. It then converts each word to title case (capitalizing the first letter and leaving the rest in lowercase), except for the first word which is converted to lowercase. Finally, it joins the words back together with no separator to form the snake case string.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public static string ConvertToSnakeCase(string input)
	{
		// Split the input string into an array of words
		string[] words = input.Split(new char[] { '-', ' ' });

		// Convert the first word to lowercase and keep the rest of the words in title case
		for (int i = 1; i < words.Length; i++)
		{
			words[i] = words[i].Substring(0, 1).ToUpper() + words[i].Substring(1);
		}

		// Join the words back together with no separator
		string snakeCase = string.Join("", words);

		return snakeCase;
	}

	public class UnityCliGenerationException : Exception
	{
		public UnityCliGenerationException(string message) : base(message)
		{

		}
	}
}
