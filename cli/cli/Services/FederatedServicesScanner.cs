using Beamable.Common.Semantics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Spectre.Console;
using System.Text;

namespace cli.Services;

public class FederatedServicesScanner
{
	public class Data
	{
		public string ServiceName { get; set; }
		public List<String> FederatedLoginImplementations { get; set; } = new List<string>();
		public List<String> FederatedInventoryImplementations { get; set; } = new List<string>();

		public string GetFederatedLoginImplementations()
		{
			if (FederatedLoginImplementations.Count == 0)
				return string.Empty;
			
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < FederatedLoginImplementations.Count; i++)
			{
				builder.Append(FederatedLoginImplementations[i]);

				if (i == FederatedLoginImplementations.Count - 1)
					continue;

				builder.Append(", ");
			}

			return builder.ToString();
		}
		
		public string GetFederatedInventoryImplementations()
		{
			if (FederatedInventoryImplementations.Count == 0)
				return string.Empty;
			
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < FederatedInventoryImplementations.Count; i++)
			{
				builder.Append(FederatedInventoryImplementations[i]);

				if (i == FederatedInventoryImplementations.Count - 1)
					continue;

				builder.Append(", ");
			}

			return builder.ToString();
		}
	}

	private readonly List<Data> _scannedData = new List<Data>();

	public FederatedServicesScanner()
	{
		if (!MSBuildLocator.IsRegistered)
		{
			MSBuildLocator.RegisterDefaults();
		}
	}

	private string SelectSolution(string workingDirectory)
	{
		string targetPath;
		
		List<string> solutionPaths = Directory.GetFiles(workingDirectory, "*.sln",
			SearchOption.TopDirectoryOnly).ToList();

		List<string> solutionFiles = new();

		foreach (string solutionPath in solutionPaths)
		{
			string[] split = solutionPath.Split(Path.DirectorySeparatorChar);
			string solutionName = split[^1].Split(".")[0];
			solutionFiles.Add(solutionName);
		}

		switch (solutionFiles.Count)
		{
			case 0:
				throw new CliException(
					$"No solution files found in {workingDirectory} directory");
			case 1:
				targetPath = solutionFiles[0];
				break;
			default:
			{
				solutionFiles.Add("cancel");

				string selection = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Select solution You would like to scan for federated services:")
						.AddChoices(solutionFiles)
				);

				if (selection == "cancel")
				{
					return String.Empty;
				}

				targetPath = selection;
				break;
			}
		}

		return $"{targetPath}.sln";
	}

	public void ScanSolution(string workingDirectory)
	{
		_scannedData.Clear();

		string solutionPath = SelectSolution(workingDirectory);

		if (solutionPath == string.Empty)
			return;
		
		MSBuildWorkspace workspace = MSBuildWorkspace.Create();
		Solution solution = workspace.OpenSolutionAsync(solutionPath).Result;

		foreach (Project project in solution.Projects)
		{
			Compilation compilation = project.GetCompilationAsync().Result;

			if (compilation != null)
			{
				foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
				{
					SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
					foreach (SyntaxNode root in syntaxTree.GetRoot().DescendantNodes())
					{
						if (semanticModel.GetDeclaredSymbol(root) is ITypeSymbol typeSymbol)
						{
							string serviceName = ParseServiceName(typeSymbol.ToString());

							var data = _scannedData.Find(data => data.ServiceName == serviceName);
							if (data == null)
							{
								data = new Data { ServiceName = serviceName };
								_scannedData.Add(data);
							}

							foreach (INamedTypeSymbol typeSymbolAllInterface in typeSymbol.AllInterfaces)
							{
								string interfaceName = ParseInterfaceName(typeSymbolAllInterface.ToString());
								string identityName = ParseCloudIdentity(typeSymbolAllInterface.ToString());

								switch (interfaceName)
								{
									case "IFederatedLogin":
										if (!data.FederatedLoginImplementations.Contains(identityName))
										{
											data.FederatedLoginImplementations.Add(identityName);
										}

										break;
									case "IFederatedInventory":
										if (!data.FederatedInventoryImplementations.Contains(identityName))
										{
											data.FederatedInventoryImplementations.Add(identityName);
										}

										break;
								}
							}
						}
					}
				}
			}
		}
	}

	private string ParseServiceName(string data)
	{
		return data.Split(".").Last();
	}

	private string ParseInterfaceName(string data)
	{
		return data.Split("<").First().Split(".").Last();
	}

	private string ParseCloudIdentity(string data)
	{
		string last = data.Split("<").Last();
		string substring = last.Substring(0, last.Length - 1);
		return substring.Split(".").Last();
	}

	public Data GetData(string serviceId)
	{
		return _scannedData.Find(data => data.ServiceName == serviceId);
	}
}
