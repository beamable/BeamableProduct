using Beamable.Common;
using cli.Services;
using System.CommandLine;

namespace cli.FederationCommands;

/// <summary>
/// This <see cref="CommandGroup"/> defines a set of commands to modify the local settings for any particular federation whenever you are running a service locally.
/// Mostly, this allows us to add custom rules for which traffic a particular microservice will "steal".
/// </summary>
public class FederationLocalSettingsCommand : CommandGroup
{
	/// <summary>
	/// Implement this on any of the commands we make that belong to <see cref="FederationLocalSettingsCommand"/> groups.
	/// </summary>
	public interface ILocalSettingsArgs
	{
		public string BeamoId { get; set; }
		public string FederationId { get; set; }
	}

	public FederationLocalSettingsCommand() : base("local-settings", "Get/Set the local settings for any particular federation")
	{
	}

	public override void Configure()
	{
	}

	/// <summary>
	/// Commands under this pallet should always return a <see cref="IFederation.ILocalSettings"/> implementing object as its <see cref="AtomicCommand{TArgs,TResult}"/> result.
	/// </summary>
	public class Get : CommandGroup
	{
		public Get() : base("get", "Get the local settings for any particular federation")
		{
		}

		public override void Configure()
		{
		}
	}

	/// <summary>
	/// Commands under this pallet should have correct semantics for each existing potential configuration option of the federation it represents.
	/// </summary>
	public class Set : CommandGroup
	{
		public Set() : base("set", "Sets the local settings for any particular federation")
		{
		}

		public override void Configure()
		{
		}
	}

	public static void AddFederationLocalSettingsSharedOptions<TArgs>(AppCommand<TArgs> command) where TArgs : CommandArgs, ILocalSettingsArgs
	{
		var beamoIdOpt = new Option<string>("--beamo-id", () => "", "The Beamo ID for the microservice whose federation you want to configure");
		command.AddOption(beamoIdOpt, (args, val) => args.BeamoId = val);

		var fedIdOpt = new Option<string>("--fed-id", () => "", "The Federation ID for the federation instance you want to configure");
		command.AddOption(fedIdOpt, (args, val) => args.FederationId = val);
	}

	public static void ValidateFederationLocalSettingsSharedOptions<TArgs>(TArgs args, Type federationType) where TArgs : CommandArgs, ILocalSettingsArgs
	{
		if (!((CommandArgs)args).BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.ContainsKey(args.BeamoId))
			throw new CliException("Invalid beamo-id specified", 2, true);

		var sd = ((CommandArgs)args).BeamoLocalSystem.BeamoManifest.ServiceDefinitions.First(s => s.BeamoId == args.BeamoId);
		if (sd.Protocol is not BeamoProtocolType.HttpMicroservice)
			throw new CliException("Non-Microservice beamo-id specified", 3, true);

		if (!sd.SourceGenConfig.Federations.TryGetValue(args.FederationId, out var federations))
			throw new CliException($"Specified beamo-id does not have a federation with the given id [{args.FederationId}]", 4, true);

		var hasFederation = false;
		foreach (var cfg in federations)
		{
			hasFederation |= federationType.GetNameWithoutGenericArity().Equals(cfg.Interface);
		}

		if (!hasFederation)
			throw new CliException($"Specified beamo-id does not have an {federationType.GetNameWithoutGenericArity()} implementation", 5, true);
	}
}
