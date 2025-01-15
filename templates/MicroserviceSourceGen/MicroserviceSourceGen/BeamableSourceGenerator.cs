using Beamable.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using UnityEngine;
using SourceProductionContext = Microsoft.CodeAnalysis.SourceProductionContext;
#pragma warning disable CS0618 // Type or member is obsolete

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
			.Where(text => text.Path.EndsWith(MicroserviceFederationsConfig.CONFIG_FILE_NAME, StringComparison.OrdinalIgnoreCase))
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
		if (config == null)
			return;


		// Generate the partial declarations with the interfaces based on the federation configs
		var isFedValid = true;

		// Each partial declaration results in a Microservice info
		// We should keep only the one that has the declared attribute here for now.
		// TODO: When we add ClientCallable signature validation, we'll need to apply it to each part of the class before moving along and
		// TODO: validating that we only have a single microservice. 
		var mergedInfos = new List<MicroserviceInfo>();
		foreach (MicroserviceInfo microserviceInfo in infos)
		{
			isFedValid &= ValidateFederations(context, microserviceInfo, config);
			if (!mergedInfos.Any(i => i.Name.Equals(microserviceInfo.Name)))
			{
				if (microserviceInfo.MicroserviceAttributeLocation != null)
				{
					mergedInfos.Add(microserviceInfo);
				}
			}
		}

		if (infos.Length > 0 && mergedInfos.Count == 0)
		{
			var err = Diagnostic.Create(Diagnostics.Srv.MissingMicroserviceId, infos[0].MicroserviceClassLocation);
			context.ReportDiagnostic(err);
			return;
		}

		if (!isFedValid)
			return;

		// Validate the microservice declaration.
		var isMsValid = ValidateMicroserviceDeclaration(context, mergedInfos.ToImmutableArray());

		// If we don't have config OR the microservices are not one, we can't generate code. 
		if (!isMsValid)
			return;

		// Add a report that we successfully generated the federation code.
		var federationCodeGenSuccess = Diagnostic.Create(Diagnostics.Fed.FederationCodeGeneratedProperly, null);
		context.ReportDiagnostic(federationCodeGenSuccess);

		return;

		static MicroserviceFederationsConfig ParseBeamSourceGen(SourceProductionContext context, ImmutableArray<(string Path, string Text)> federationConfigFiles)
		{
			if (federationConfigFiles.Length <= 0)
			{
				var err = Diagnostic.Create(Diagnostics.Cfg.NoSourceGenConfigFound, null);
				context.ReportDiagnostic(err);
				return null;
			}

			if (federationConfigFiles.Length > 1)
			{
				var errDetails = string.Join("\n", federationConfigFiles.Select(t => t.Path));
				var error = Diagnostic.Create(Diagnostics.Cfg.MultipleSourceGenConfigsFound, null, errDetails);
				context.ReportDiagnostic(error);
				return null;
			}

			try
			{
				var sourceGenConfig = JsonSerializer.Deserialize<MicroserviceFederationsConfig>(federationConfigFiles[0].Text, new JsonSerializerOptions { IncludeFields = true });
				var success = Diagnostic.Create(Diagnostics.Cfg.DeserializedSourceGenConfig,
					null,
					federationConfigFiles[0].Text);
				context.ReportDiagnostic(success);
				return sourceGenConfig;
			}
			catch (Exception ex)
			{
				var error = Diagnostic.Create(Diagnostics.Cfg.FailedToDeserializeSourceGenConfig,
					null,
					ex.ToString(),
					federationConfigFiles[0].Text);
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
				var errDetails = string.Join(", ", infos.Select(t => t.Name));
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

		static bool ValidateFederations(SourceProductionContext context, MicroserviceInfo info, MicroserviceFederationsConfig federationConfig)
		{
			var isValid = true;
			var federations = federationConfig.Federations;

			Dictionary<string, (string Id, string Interface)> flatConfig = federations.SelectMany(kvp => kvp.Value.Select(f => (kvp.Key, f.Interface))).ToDictionary(x => $"{x.Key}/{x.Interface}");

			var flatCode = info.ImplementedFederations.Where(f => f.Id != null).ToDictionary(x => $"{x.Id}/{x.Federation.Interface}");
			var flatIds = flatConfig.Select(f => f.Value.Id).ToList();
			flatIds.AddRange(flatCode.Where(f => f.Value.Id != null).Select(f => f.Value.Id));

			foreach (var fed in info.ImplementedFederations)
			{
				if (fed.Id == null)
				{
					var error = Diagnostic.Create(Diagnostics.Fed.FederationIdMissingAttribute, fed.Location, fed.Id);
					context.ReportDiagnostic(error);
					isValid = false;
				}
			}

			var flatIdSet = new HashSet<string>(flatIds);

			var configsThatDoNotExistInCode = flatConfig.Keys.Except(flatCode.Keys).ToList();
			var codeThatDoesNotExistInConfig = flatCode.Keys.Except(flatConfig.Keys).ToList();

			foreach (var configKey in configsThatDoNotExistInCode)
			{
				var (fedId, fedInterface) = flatConfig[configKey];
				isValid = false;
				var error = Diagnostic.Create(
					Diagnostics.Fed.ConfiguredFederationMissingFromCode,
					info.MicroserviceClassLocation,
					info.Name,
					fedId,
					fedInterface);
				context.ReportDiagnostic(error);
			}

			foreach (var codeKey in codeThatDoesNotExistInConfig)
			{
				var (fedId, fedClassName, fedInstConfig, location) = flatCode[codeKey];
				isValid = false;
				var error = Diagnostic.Create(
					Diagnostics.Fed.DeclaredFederationMissingFromSourceGenConfig,
					info.MicroserviceClassLocation,
					info.Name,
					fedId,
					fedInstConfig.Interface);
				context.ReportDiagnostic(error);
			}

			foreach (var id in flatIdSet)
			{
				if (!ValidateId(id))
				{
					// EMIT FEDERATION INVALID ID
					var error = Diagnostic.Create(Diagnostics.Fed.DeclaredFederationInvalidFederationId, info.MicroserviceClassLocation, info.Name, id);
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
	}

	public readonly record struct MicroserviceInfo : IEquatable<MicroserviceInfo>
	{
		public string Namespace { get; }
		public string Name { get; }
		public Location MicroserviceClassLocation { get; }
		public string ServiceId { get; }
		public bool HasMicroserviceAttribute { get; }
		public Location MicroserviceAttributeLocation { get; }
		public bool IsPartial { get; }
		public List<(string Id, string ClassName, FederationInstanceConfig Federation, Location Location)> ImplementedFederations { get; }

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

				var fedAttribute = federationIdType
					.GetAttributes()
					.FirstOrDefault(a => a?.AttributeClass?.Name == nameof(FederationIdAttribute));

				var fedValue = fedAttribute?.ConstructorArguments.FirstOrDefault();
				if (fedValue == null)
				{
					id = null;
				}
				else
				{
					id = fedValue.Value.Value?.ToString();
				}

				ImplementedFederations.Add((
					id!,
					className,
					new FederationInstanceConfig() { Interface = federationInterfaceName },
					federationIdType.Locations[0]
				));
			}

			// Check for the microservice attribute so we can validate its name does not have any invalid characters.
			var serviceId = "";

			var microserviceAttr = type.GetAttributes().FirstOrDefault(a => a?.AttributeClass?.Name == nameof(MicroserviceAttribute));

			HasMicroserviceAttribute = microserviceAttr != null;
			if (microserviceAttr != null)
			{
				HasMicroserviceAttribute = true;
				MicroserviceAttributeLocation = microserviceAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
			}

			if (microserviceAttr?.ConstructorArguments.Length > 0)
			{
				serviceId = microserviceAttr.ConstructorArguments[0].Value?.ToString();
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
