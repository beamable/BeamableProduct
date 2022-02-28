using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace microservice;

public static class Program
{
	static int Main(string[] args)
	{
		// TODO: Add client code generation based on sub type

		// var intOption = new Option<int>(
		// 	"--int-option",
		// 	getDefaultValue: () => 42,
		// 	description: "An option whose argument is parsed as an int");
		// var boolOption = new Option<bool>(
		// 	"--bool-option",
		// 	"An option whose argument is parsed as a bool");
		// var fileOption = new Option<FileInfo>(
		// 	"--file-option",
		// 	"An option whose argument is parsed as a FileInfo");

// Add the options to a root command:
		var rootCommand = new RootCommand
		{
			// intOption,
			// boolOption,
			// fileOption
		};

		rootCommand.Description = "Beamable Microservice Base Image Tooling";
		// rootCommand.SetHandler((int a, int b) =>
		// {
		// 	Console.WriteLine($"derp {a}+{b}={a+b}");
		//
		// });
		var addComm = new Command("add");
		var aArg = new Argument<int>("a", () => 1, "desc for a");
		var bArg = new Argument<int>("b", () => 1, "desc for b");
		addComm.AddArgument(aArg);
		addComm.AddArgument(bArg);
		addComm.SetHandler((int a, int b) =>
		{
			Console.WriteLine($"{a}+{b}={a+b}");
		}, aArg, bArg);
		var mult = new Command("mult");
		mult.AddArgument(aArg);
		mult.AddArgument(bArg);
		mult.SetHandler((int a, int b) =>
		{
			Console.WriteLine($"{a}*{b}={a*b}");

		}, aArg, bArg);
		rootCommand.AddCommand(addComm);
		rootCommand.AddCommand(mult);
		// rootCommand.SetHandler((int i, bool b, FileInfo f) =>
		// {
		// 	Console.WriteLine($"The value for --int-option is: {i}");
		// 	Console.WriteLine($"The value for --bool-option is: {b}");
		// 	Console.WriteLine($"The value for --file-option is: {f?.FullName ?? "null"}");
		// }, intOption, boolOption, fileOption);
		return rootCommand.Invoke(args);
	}

}