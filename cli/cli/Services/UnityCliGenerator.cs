using Beamable.Common.BeamCli;
using cli.Unreal;
using Serilog;
using System.CodeDom;
using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;

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
			if (invalidCommands.Contains(command.executionPath)) continue;
			files.Add(Generate(context.Root, command));
		}
		return files;
	}

	public static GeneratedFileDescriptor Generate(BeamCommandDescriptor rootCommand, BeamCommandDescriptor command)
	{
		var unit = new CodeCompileUnit();
		var root = new CodeNamespace("Beamable.Editor.BeamCli.Commands");
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common"));
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common.BeamCli"));
		unit.Namespaces.Add(root);

		root.Types.Add(GenerateCodeType(rootCommand, command));
		
		var srcCode = UnityHelper.GenerateCsharp(unit);
		var fileName = ConvertToSnakeCase(command.executionPath) + ".cs";
		return new GeneratedFileDescriptor { Content = srcCode, FileName = fileName.Capitalize() };
	}

	public static CodeTypeDeclaration GenerateCodeType(BeamCommandDescriptor rootCommand, BeamCommandDescriptor descriptor)
	{
		const string strVar = "genBeamCommandStr";
		const string argsVar = "genBeamCommandArgs";
		
		var type = new CodeTypeDeclaration("BeamCommands")
		{
			IsPartial = true
		};

		var method = new CodeMemberMethod
		{
			Name = ConvertToSnakeCase(descriptor.ExecutionPathAsCapitalizedStringWithoutBeam()),
			ReturnType = new CodeTypeReference(typeof(IBeamCommand)),
			Attributes = MemberAttributes.Public
		};


		var createArgListStatement = new CodeVariableDeclarationStatement(typeof(List<string>), argsVar, new CodeObjectCreateExpression(typeof(List<string>)));
		var argReference = new CodeVariableReferenceExpression(argsVar);
		method.Statements.Add(new CodeCommentStatement("Create a list of arguments for the command"));
		method.Statements.Add(createArgListStatement);

		var createStrStatement = new CodeVariableDeclarationStatement(typeof(string), strVar,
			new CodePrimitiveExpression(descriptor.executionPath));
		var strReference = new CodeVariableReferenceExpression(strVar);
		method.Statements.Add(new CodeCommentStatement("Capture the path to the command"));
		method.Statements.Add(createStrStatement);

		var addStrStatement = new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), strReference);
		method.Statements.Add(new CodeCommentStatement("The first argument is always the path to the command itself"));
		method.Statements.Add(addStrStatement);

		void AddArgsAndOptions(BeamCommandDescriptor subCommand)
		{
			try
			{
				foreach (var arg in subCommand.command.Arguments)
				{
					var parameter = CreateParameter(arg);
					method.Parameters.Add(parameter);

					var parameterReference = new CodeVariableReferenceExpression(parameter.Name);

					var valueIsNotNullExpr = new CodeBinaryOperatorExpression(parameterReference,
						CodeBinaryOperatorType.IdentityInequality,
						new CodeDefaultValueExpression(parameter.Type));
					var conditional = new CodeConditionStatement(valueIsNotNullExpr);
					var addStatement =
						new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), parameterReference);
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

				foreach (var option in subCommand.command.Options)
				{
					var parameter = CreateParameter(option);
					method.Parameters.Add(parameter);


					var parameterReference = new CodeVariableReferenceExpression(parameter.Name);

					var valueIsNotNullExpr = new CodeBinaryOperatorExpression(parameterReference,
						CodeBinaryOperatorType.IdentityInequality,
						new CodeDefaultValueExpression(parameter.Type));
					var conditional = new CodeConditionStatement(valueIsNotNullExpr);



					var optionalVal =
						new CodeBinaryOperatorExpression(new CodePrimitiveExpression("--" + option.Name + "="),
							CodeBinaryOperatorType.Add, parameterReference);
					var addStatement =
						new CodeMethodInvokeExpression(argReference, nameof(List<int>.Add), optionalVal);

					if (option.AllowMultipleArgumentsPerToken)
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
				Log.Error($"path=[{subCommand.executionPath}] message=[{ex.Message}]");
				throw ex;
			}
		}
		AddArgsAndOptions(rootCommand);
		AddArgsAndOptions(descriptor);
		
		// sort the parameters so that optionals are always last
		var parameters = method.Parameters.Cast<CodeParameterDeclarationExpression>().ToList();
		parameters.Sort((a, b) => a.CustomAttributes.Count.CompareTo(b.CustomAttributes.Count));
		method.Parameters.Clear();
		method.Parameters.AddRange(parameters.ToArray());

		// create the method comments.
		method.Comments.Add(new CodeCommentStatement(new CodeComment($"<summary>{descriptor.command.Description}</summary>", true)));
		foreach (var parameter in parameters)
		{
			var desc = "";
			if (parameter.UserData.Contains("desc"))
			{
				desc = parameter.UserData["desc"].ToString();
			}
			method.Comments.Add(new CodeCommentStatement(new CodeComment($"<param name=\"{parameter.Name}\">{desc}</param>", true)));
		}
		
		
		var factoryReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_factory");
		var createMethodCall = new CodeMethodInvokeExpression(factoryReference, nameof(IBeamCommandFactory.Create));
		var instanceAssignment =
			new CodeVariableDeclarationStatement(typeof(IBeamCommand), "command", createMethodCall);
		var instanceReference = new CodeVariableReferenceExpression("command");
		method.Statements.Add(new CodeCommentStatement("Create an instance of an IBeamCommand"));
		method.Statements.Add(instanceAssignment);

		var joinStatement = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),
			nameof(string.Join), new CodePrimitiveExpression(" "), argReference);
		var strAssignment = new CodeAssignStatement(strReference, joinStatement);
		method.Statements.Add(new CodeCommentStatement("Join all the args with spaces"));
		method.Statements.Add(strAssignment);
		
		var setCommandMethodCall = new CodeMethodInvokeExpression(instanceReference, nameof(IBeamCommand.SetCommand), strReference);
		method.Statements.Add(new CodeCommentStatement("Configure the command with the command string"));
		method.Statements.Add(setCommandMethodCall);
		
		var returnStatement = new CodeMethodReturnStatement(instanceReference);
		method.Statements.Add(new CodeCommentStatement("Return the command!"));
		method.Statements.Add(returnStatement);
		
		type.Members.Add(method);
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
			parameter.UserData["desc"] = option.IsRequired ? "": $"(default={internalArg?.GetDefaultValue()?.ToString()}) ";
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
		string[] words = input.Split(new char[]{'-', ' '});

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
