using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using System.CommandLine;
using System.Text;

namespace cli.Version;

public class ConstructVersionCommandArgs : CommandArgs
{
	public bool isNightly;
	public bool isPreviewRc;
	public bool isProduction;
	public bool isExp;
	public int major, minor, patch, rc, expRc;

	public bool failIfExists;

}

public class ConstructVersionOutput
{
	public string versionString;
	public string versionPrefix;
	public string versionSuffix;
	public bool exists;
}

public class ConstructVersionCommand : AtomicCommand<ConstructVersionCommandArgs, ConstructVersionOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public ConstructVersionCommand() : base("construct", "constructs a beamable version string with the given configuration")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<int>("major", "The major semantic version number"), (args, i) => args.major = i);
		AddArgument(new Argument<int>("minor", "The minor semantic version number"), (args, i) => args.minor = i);
		AddArgument(new Argument<int>("patch", "The patch semantic version number"), (args, i) => args.patch = i);
		AddOption(
			new Option<bool>("--validate",
				"When true, the command will return a non zero exit code if the specified version already exists on Nuget"),
			(args, i) => args.failIfExists = i);
		AddOption(
			new Option<bool>("--nightly",
				"Sets the version string to a nightly version number, and will include the date string automatically"),
			(args, i) => args.isNightly = i);
		AddOption(
			new Option<int>("--rc",
				"Sets the version string to a release candidate version number"),
			(args, i) =>
			{
				if (i > 0)
				{
					args.isPreviewRc = true;
					args.rc = i;
				}
			});
		AddOption(
			new Option<int>("--exp",
				"Sets the version string to an experimental version number"),
			(args, i) =>
			{
				if (i > 0)
				{
					args.isExp = true;
					args.expRc = i;
				}
			});
		AddOption(
			new Option<bool>("--prod",
				"Sets the version string to a production version number"),
			(args, i) => args.isProduction = i);
	}

	public override async Task<ConstructVersionOutput> GetResult(ConstructVersionCommandArgs args)
	{
		CheckOnlyOneFlagIsSet(args);
		PackageVersion version;
		if (args.isNightly)
		{
			if (args.major != 0 || args.minor != 0 || args.patch != 0)
			{
				throw new CliException(
					"when creating a nightly package, the major, minor, and patch numbers must be zero.");
			}
			var time = GenerateNightlyTimestamp();
			version = new PackageVersion(
				major: 0,
				minor: 0,
				patch: 0,
				isPreview: true,
				nightlyTime: time);
		} 
		else if (args.isPreviewRc)
		{
			version = new PackageVersion(
				major: args.major,
				minor: args.minor,
				patch: args.patch,
				isPreview: true,
				rc: args.rc);
		}
		else if (args.isExp)
		{
			version = new PackageVersion(
				major: args.major,
				minor: args.minor,
				patch: args.patch,
				isPreview: false,
				isExperimental: true,
				rc: args.expRc);
		}
		else if (args.isProduction)
		{
			version = new PackageVersion(
				major: args.major,
				minor: args.minor,
				patch: args.patch);
		}
		else
		{
			throw new CliException("Must specify either nightly, rc, or prod");
		}

		var service = args.DependencyProvider.GetService<VersionService>();
		var data = await service.GetBeamableToolPackageVersions();

		var versionString = version.ToString();
		var exists = false;
		foreach (var instance in data)
		{
			if (string.Equals(versionString.ToLowerInvariant(), instance.packageVersion.ToLowerInvariant()))
			{
				exists = true;
				break;
			}
		}

		if (exists && args.failIfExists)
		{
			throw new CliException("version already exists");
		}

		var versionPrefix = $"{version.Major}.{version.Minor}.{version.Patch}";
		var versionSuffix = "";
		if (versionString.Length > versionPrefix.Length + 1)
		{
			versionSuffix = versionString.Substring(versionPrefix.Length + 1);
		}
		return new ConstructVersionOutput
		{
			exists = exists, 
			versionString = versionString,
			versionPrefix = versionPrefix,
			versionSuffix = versionSuffix
		};
	}

	static void CheckOnlyOneFlagIsSet(ConstructVersionCommandArgs args)
	{
		var count = 0;
		count += args.isNightly ? 1 : 0;
		count += args.isProduction ? 1 : 0;
		count += args.isPreviewRc ? 1 : 0;
		count += args.isExp ? 1 : 0;

		if (count > 1)
		{
			throw new CliException("Cannot specify multiple build types. Select nightly, rc, or prod");
		}
	}

	static long GenerateNightlyTimestamp()
	{
		// the nightly time format is 
		//          YYYYMMDDHHMM
		// example: 202403051817
		
		// good thing they don't start with leading zeros, 
		//  because our SDK code needs the data as a NUMBER, 
		//  not a string, which is silly, but workable.

		var now = DateTimeOffset.UtcNow;
		var str = $"{now:yyyyMMddHHmm}";
		Log.Verbose($"version string=[{str}]");
		var numeric = long.Parse(str);
		return numeric;
	}
}
