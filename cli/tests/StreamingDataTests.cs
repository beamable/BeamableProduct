using Beamable.Common.BeamCli;
using cli;
using cli.Services;
using NUnit.Framework;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace tests;

public class StreamingDataTests
{
	[Test]
	public void OutputTypesAreDeclaredInTheCLiAssembly()
	{
		var app = new App();
		app.Configure();
		app.Build();

		var commands = app.InstantiateAllCommands();
		Assert.That(commands, Is.Not.Null);

		var cliAssembly = typeof(App).Assembly;

		foreach (var command in commands)
		{
			if (CliGenerator.CommandTypesToReject.Contains(command.GetType())) continue;
			
			var commandType = command.GetType();
			var resultStreamGenType = typeof(IResultSteam<,>);
			var inputGenType = typeof(IHasArgs<>);

			var allInterfaces = commandType.GetInterfaces();
			var resultStreamTypeArgs = new List<Type[]>();
			var inputTypeArgs = new List<Type[]>();
			foreach (var subInterface in allInterfaces)
			{
				if (!subInterface.IsGenericType) continue;
				if (subInterface.GetGenericTypeDefinition() == resultStreamGenType)
				{
					var genArgs = subInterface.GetGenericArguments();
					resultStreamTypeArgs.Add(genArgs);
				}

				if (subInterface.GetGenericTypeDefinition() == inputGenType)
				{
					var genArgs = subInterface.GetGenericArguments();
					inputTypeArgs.Add(genArgs);
				}

			}

			void CheckTypes(List<Type> types, string messageFragment)
			{
				foreach (var type in types)
				{
					var declaringAssembly = type.Assembly;
					var hasCliContractAttribute = type.GetCustomAttribute<CliContractTypeAttribute>() != null;
					if (!hasCliContractAttribute && cliAssembly != declaringAssembly)
					{
						Assert.Fail(@$"Command=[{commandType.Name}] references {messageFragment} Type=[{type.Name}], which not declared in the CLI Assembly.
The CLI's output types should be defined in the CLI assembly to reduce the likelihood of accidental type changes. If the output type is changed, then
that qualifies as a potential breaking change in the CLI. ");
					}
				}
			}

			var allInputTypes = UnityCliGenerator.RecurseTypes(inputTypeArgs.SelectMany(x => x), true);
			var allOutputTypes = UnityCliGenerator.RecurseTypes(resultStreamTypeArgs.SelectMany(x => x), true);

			CheckTypes(allInputTypes, "input");
			CheckTypes(allOutputTypes, "output");

		}
	}
}
