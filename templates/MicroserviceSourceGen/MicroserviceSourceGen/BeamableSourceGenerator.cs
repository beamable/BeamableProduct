using Beamable.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using SourceProductionContext = Microsoft.CodeAnalysis.SourceProductionContext;

namespace Beamable.Server;

/// <summary>
/// A sample source generator that creates C# classes based on the text file (in this case, Domain Driven Design ubiquitous language registry).
/// When using a simple text file as a baseline, we can create a non-incremental source generator.
/// </summary>
[Generator]
public class BeamableSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var microserviceTypes = context.SyntaxProvider
			.CreateSyntaxProvider(CouldBeMicroserviceAsync, GetMicroserviceInfo)
			.Where(type => type != default)!
			.Collect<MicroserviceInfo>();

		var sourceConfigText = context.AdditionalTextsProvider
			.Where(text => text.Path.EndsWith(MicroserviceSourceGenConfig.CONFIG_FILE_NAME, StringComparison.OrdinalIgnoreCase))
			.Select((text, token) => (Path: text.Path, Text: text.GetText(token)?.ToString()))
			.Where(text => text.Item2 is not null)!
			.Collect<ValueTuple<string, string>>();

		context.RegisterSourceOutput(microserviceTypes.Combine(sourceConfigText), GenerateCode);
		return;

		static bool CouldBeMicroserviceAsync(SyntaxNode syntaxNode, CancellationToken cancellationToken)
		{
			if (syntaxNode is not ClassDeclarationSyntax c)
				return false;

			return c.BaseList?.Types.Count > 0;
		}

		static MicroserviceInfo GetMicroserviceInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			var type = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration) as INamedTypeSymbol;

			return type is null || type.BaseType?.Name != "Microservice" ? default : new MicroserviceInfo(type);
		}
	}

	private static void GenerateCode(SourceProductionContext context, (ImmutableArray<MicroserviceInfo> Left, ImmutableArray<(string, string)> Right) args)
	{
		var (infos, sourceGenConfig) = args;

		// Parse the config
		var config = ParseBeamSourceGen(context, sourceGenConfig);

		// Validate the microservice declaration.
		var isMsValid = ValidateMicroserviceDeclaration(context, infos);

		// If we don't have config OR the microservices are not one, we can't generate code. 
		if (config == null || !isMsValid)
			return;

		var info = infos[0];

		// Generate the partial declarations with the interfaces based on the federation configs
		var isFedValid = ValidateFederations(context, info, config);

		if (!isFedValid)
			return;

		// Generate the code for federations
		GenerateFederationCode(context, info, config);

		return;

		static MicroserviceSourceGenConfig? ParseBeamSourceGen(SourceProductionContext context, ImmutableArray<(string Path, string Text)> beamSourceGenConfigFiles)
		{
			if (beamSourceGenConfigFiles.Length <= 0)
			{
				var err = Diagnostic.Create(Diagnostics.Cfg.NoSourceGenConfigFound, null);
				context.ReportDiagnostic(err);
				return null;
			}

			if (beamSourceGenConfigFiles.Length > 1)
			{
				var errDetails = string.Join("\n", beamSourceGenConfigFiles.Select(t => t.Path));
				var error = Diagnostic.Create(Diagnostics.Cfg.MultipleSourceGenConfigsFound, null, errDetails);
				context.ReportDiagnostic(error);
				return null;
			}

			try
			{
				var sourceGenConfig = JsonSerializer.Deserialize<MicroserviceSourceGenConfig>(beamSourceGenConfigFiles[0].Text, new JsonSerializerOptions { IncludeFields = true });
				var success = Diagnostic.Create(Diagnostics.Cfg.DeserializedSourceGenConfig,
					null,
					beamSourceGenConfigFiles[0].Text);
				context.ReportDiagnostic(success);
				return sourceGenConfig;
			}
			catch (Exception ex)
			{
				var error = Diagnostic.Create(Diagnostics.Cfg.FailedToDeserializeSourceGenConfig,
					null,
					ex.ToString(),
					beamSourceGenConfigFiles[0].Text);
				context.ReportDiagnostic(error);

				return null;
			}
		}

		static bool ValidateMicroserviceDeclaration(SourceProductionContext context, ImmutableArray<MicroserviceInfo> infos)
		{
			if (infos.Length == 0)
			{
				var err = Diagnostic.Create(Diagnostics.Srv.NoMicroserviceClassesDetected, null);
				context.ReportDiagnostic(err);
				return false;
			}

			if (infos.Length > 1)
			{
				var errDetails = string.Join("\n", infos.Select(t => t.Name));
				var error = Diagnostic.Create(Diagnostics.Srv.MultipleMicroserviceClassesDetected, null, errDetails);
				context.ReportDiagnostic(error);
				return false;
			}

			var isValid = true;
			var info = infos[0];
			if (!info.IsPartial)
			{
				var err = Diagnostic.Create(Diagnostics.Srv.NonPartialMicroserviceClassDetected, info.MicroserviceClassLocation);
				context.ReportDiagnostic(err);
				isValid = false;
			}

			if (string.IsNullOrEmpty(info.ServiceId))
			{
				var err = Diagnostic.Create(Diagnostics.Srv.MissingMicroserviceId, info.MicroserviceClassLocation);
				context.ReportDiagnostic(err);
				isValid = false;
			}

			if (!ValidateId(info.ServiceId))
			{
				var err = Diagnostic.Create(Diagnostics.Srv.InvalidMicroserviceId, info.MicroserviceAttributeLocation);
				context.ReportDiagnostic(err);
				isValid = false;
			}

			return isValid;

			// First digit can't be a number
			// Alphanumeric + "_"
			static bool ValidateId(string id)
			{
				if (string.IsNullOrEmpty(id)) return false;
				var isValid = true;
				for (int charIdx = 0; charIdx < id.Length; charIdx++)
				{
					var currChar = id[charIdx];
					if (charIdx == 0)
					{
						isValid &= char.IsLetter(currChar);
					}
					else
					{
						isValid &= char.IsLetterOrDigit(currChar) || currChar.Equals('_');
					}
				}

				return isValid;
			}
		}

		static bool ValidateFederations(SourceProductionContext context, MicroserviceInfo info, MicroserviceSourceGenConfig beamSourceGenConfig)
		{
			var isValid = true;
			var federations = beamSourceGenConfig.Federations;
			foreach (var (id, _, federation) in info.ImplementedFederations)
			{
				if (string.IsNullOrEmpty(id))
				{
					// TODO: EMIT WE COULDN'T FIND YOUR ID FROM YOUR HANDWRITTEN TYPE BECAUSE IT WAS NOT A STRING LITERAL. PLEASE DO NOT USE CONSTS AND USE ONE OF THESE FORMATS.
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationMissingFederationId, info.MicroserviceClassLocation, info.Name, id, federation.Interface);
					context.ReportDiagnostic(error);
					isValid = false;
				}
				else if (!federations.TryGetValue(id, out var declaredInstances))
				{
					// EMIT FEDERATION_ID NOT DECLARED
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationMissingFromSourceGenConfig, info.MicroserviceClassLocation, info.Name, id, federation.Interface);
					context.ReportDiagnostic(error);
					isValid = false;
				}
				// If we can't find the specific interface declared for this Id in the config file, this is an error.
				else if (!declaredInstances.Contains(federation))
				{
					// EMIT FEDERATION MISSING
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationMissingFromSourceGenConfig, info.MicroserviceClassLocation, info.Name, id, federation.Interface);
					context.ReportDiagnostic(error);
					isValid = false;
				}
				else
				{
					// No need to do anything if we are already declared.
				}

				if (!ValidateId(id))
				{
					// EMIT FEDERATION INVALID ID
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationInvalidFederationId, info.MicroserviceClassLocation, info.Name, id);
					context.ReportDiagnostic(error);
					isValid = false;
				}
			}
			
			foreach (var kvp in beamSourceGenConfig.Federations)
			{
				if (!ValidateId(kvp.Key))
				{
					// EMIT FEDERATION INVALID ID
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationInvalidFederationId, info.MicroserviceClassLocation, info.Name, kvp.Key);
					context.ReportDiagnostic(error);
					isValid = false;
				}
			}

			return isValid;
			
			// First digit can't be a number
			// Alphanumeric + "_"
			static bool ValidateId(string id)
			{
				if (string.IsNullOrEmpty(id)) return false;
				var isValid = true;
				for (int charIdx = 0; charIdx < id.Length; charIdx++)
				{
					var currChar = id[charIdx];
					if (charIdx == 0)
					{
						isValid &= char.IsLetter(currChar);
					}
					else
					{
						isValid &= char.IsLetterOrDigit(currChar) || currChar.Equals('_');
					}
				}

				return isValid;
			}
			
		}

		static void GenerateFederationCode(SourceProductionContext sourceProductionContext, MicroserviceInfo microserviceInfo, MicroserviceSourceGenConfig beamSourceGenConfig)
		{
			// Declare the FederationId file's "header"
			var federationIdsFile = new StringBuilder(2048);
			federationIdsFile.Append($@"
using Beamable.Common;
using Beamable.Server;

namespace {microserviceInfo.Namespace} {{
");

			// We have one partial file per-id that holds all the interfaces declarations for that id.
			var partialFileBuilder = new StringBuilder(2048);

			var federationIds = beamSourceGenConfig.Federations.Keys.ToImmutableArray();
			foreach (string federationId in federationIds)
			{
				string federationIdClassName;
				// If the Federation ID is NOT handwritten, generate code for the ID class.
				var implementedFederationId = microserviceInfo.ImplementedFederations.FirstOrDefault(fed => fed.Id == federationId);
				if (implementedFederationId == default)
				{
					federationIdClassName = string.Concat(federationId[0].ToString().ToUpper(), federationId.Substring(1, federationId.Length - 1), "Id");
					// Add the IFederatedId implementation
					federationIdsFile.AppendLine();
					federationIdsFile.Append($@"
	public class {federationIdClassName} : IFederationId {{
		public string UniqueName => ""{federationId}"";
	}}
");
				}
				// If it is handwritten, capture the class name for the id class so we can declare federations that are NOT handwritten.
				else
				{
					federationIdClassName = implementedFederationId.ClassName;
				}

				// Clear the text for this federation id's partial microservice declarations
				partialFileBuilder.Clear();
				partialFileBuilder.Append($@"
using Beamable.Common;
using Beamable.Server;

namespace {microserviceInfo.Namespace} {{
");

				// Add the partial declarations that implements the federation interfaces
				var interfaces = beamSourceGenConfig.Federations[federationId];
				foreach (var instanceConfig in interfaces)
				{
					// Only generate code for the federations that the user does not Hand-write
					var handwrittenImplementation = microserviceInfo.ImplementedFederations.FirstOrDefault(fed => fed.Id == federationId && fed.Federation.Interface == instanceConfig.Interface);
					if (handwrittenImplementation == default)
					{
						partialFileBuilder.AppendLine();
						partialFileBuilder.Append($@"
	public partial class {microserviceInfo.Name} : {instanceConfig.Interface}<{federationIdClassName}> {{	
	}}
");
					}
				}

				partialFileBuilder.AppendLine("}");

				sourceProductionContext.AddSource($"{microserviceInfo.Name}.{federationId}.g.cs", partialFileBuilder.ToString());
			}

			federationIdsFile.AppendLine("}");

			sourceProductionContext.AddSource($"{microserviceInfo.Name}.FederationIds.g.cs", federationIdsFile.ToString());

			// Add a report that we successfully generated the federation code.
			var federationCodeGenSuccess = Diagnostic.Create(Diagnostics.Fed.FederationCodeGeneratedProperly, null);
			sourceProductionContext.ReportDiagnostic(federationCodeGenSuccess);
		}
	}

	public readonly record struct MicroserviceInfo : IEquatable<MicroserviceInfo>
	{
		public string? Namespace { get; }
		public string Name { get; }
		public Location? MicroserviceClassLocation { get; }
		public string ServiceId { get; }
		public bool HasMicroserviceAttribute { get; }
		public Location? MicroserviceAttributeLocation { get; }
		public bool IsPartial { get; }
		public List<(string Id, string ClassName, FederationInstanceConfig Federation)> ImplementedFederations { get; }

		public MicroserviceInfo(INamedTypeSymbol type)
		{
			Namespace = type.ContainingNamespace.IsGlobalNamespace
				? null
				: type.ContainingNamespace.ToString();
			Name = type.Name;

			// Check if this is a partial class
			IsPartial = type.DeclaringSyntaxReferences
				.Any(syntax => syntax.GetSyntax() is ClassDeclarationSyntax declaration && declaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));

			// Gather all federation interfaces... so we can verify that they are declared in the Federation file.
			ImplementedFederations = new();
			foreach (INamedTypeSymbol i in type.Interfaces)
			{
				// We don't care about interfaces that are not inherited from IFederation
				if (!i.Interfaces.Any(parentInterface => parentInterface.Name == nameof(IFederation)))
					continue;

				var federationInterfaceName = i.Name;
				var id = "";

				// Find the first type arg of the federation interface that implements IFederationId
				var federationIdType = i.TypeArguments.First(t => t.Interfaces.Any(typeArgInterface => typeArgInterface.Name is nameof(IFederationId) or nameof(IThirdPartyCloudIdentity)));
				var className = federationIdType.Name;
				foreach (ISymbol m in federationIdType.GetMembers())
				{
					// If we can't find the Unique Name declaration...
					if (m is not IPropertySymbol { Name: nameof(IFederationId.UniqueName) } p)
						continue;

					// Find the id...
					var syntaxNode = p.GetMethod?.DeclaringSyntaxReferences[0].GetSyntax();
					if (syntaxNode is AccessorDeclarationSyntax accessor)
					{
						// UniqueName { get => "someId"; }
						if (accessor.ExpressionBody is { } arrowClause)
						{
							// Return that literal as the id.
							if (arrowClause!.Expression is LiteralExpressionSyntax) id = arrowClause!.Expression.ToString().Replace("\"", "");
							else id = "";
						}
						// UniqueName { get { return "someId"; } }
						else
						{
							// Find the return statement that returns a string literal
							var returnStatement = accessor.Body?.ChildNodes()
								.OfType<ReturnStatementSyntax>().First(r => r.Expression is LiteralExpressionSyntax);

							// Return that literal as the id.
							if (returnStatement!.Expression is LiteralExpressionSyntax) id = returnStatement!.Expression.ToString().Replace("\"", "");
							else id = "";
						}
					}
					// UniqueName => "someId";
					else if (syntaxNode is ArrowExpressionClauseSyntax arrowClause)
					{
						if (arrowClause.Expression is LiteralExpressionSyntax) id = arrowClause.Expression.ToString().Replace("\"", "");
						else id = "";
					}
					else
					{
						id = "";
					}
				}

				ImplementedFederations.Add((id, className, new FederationInstanceConfig() { Interface = federationInterfaceName }));
			}

			// Check for the microservice attribute so we can validate its name does not have any invalid characters.
			var serviceId = "";
			var declaringClass = (ClassDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax();
			MicroserviceClassLocation = declaringClass.GetLocation();
			foreach (var attrList in declaringClass.AttributeLists)
			{
				foreach (AttributeSyntax attr in attrList.Attributes)
				{
					HasMicroserviceAttribute = attr.Name.ToString().EndsWith(nameof(MicroserviceAttribute)) ||
					                           attr.Name.ToString().EndsWith(nameof(MicroserviceAttribute).Substring(0, nameof(MicroserviceAttribute).Length - "Attribute".Length));

					if (HasMicroserviceAttribute)
					{
						var argList = attr.ArgumentList;
						if (argList != null)
						{
							var arg = argList.Arguments[0];
							serviceId = arg.Expression.ToString().Replace("\"", "");
						}

						MicroserviceAttributeLocation = attr.GetLocation();

						break;
					}
				}

				if (HasMicroserviceAttribute)
					break;
			}

			ServiceId = serviceId;
		}


		public bool Equals(MicroserviceInfo other)
		{
			return Namespace == other.Namespace
			       && Name == other.Name
			       && HasMicroserviceAttribute == other.HasMicroserviceAttribute;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Name.GetHashCode();
				hashCode = (hashCode * 397) ^ HasMicroserviceAttribute.GetHashCode();

				return hashCode;
			}
		}
	}
}
