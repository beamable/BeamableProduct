using cli;
using cli.Services;
using cli.Services.PortalExtension;
using NUnit.Framework;
using System.IO;

namespace tests.PortalExtensionTests;

/// <summary>
/// Covers <see cref="PortalExtensionObserver.CreateMetaDataFile"/>. It used to pass the metadata file's own
/// path to Directory.CreateDirectory, which created a directory named "metadata.json" whenever the build had
/// not produced the assets folder yet — and every later run then failed with "Access to the path ... is
/// denied", permanently wedging the extension.
/// </summary>
public class PortalExtensionMetadataFileTests
{
	private string _extensionDir;

	[SetUp]
	public void SetUp()
	{
		_extensionDir = Path.Combine(Path.GetTempPath(), "pe-metadata-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_extensionDir);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_extensionDir))
		{
			Directory.Delete(_extensionDir, true);
		}
	}

	private PortalExtensionObserver MakeObserver() => new()
	{
		ExtensionMetaData = new PortalExtensionDef
		{
			Name = "MyExt",
			AbsolutePath = _extensionDir,
			Properties = new PortalExtensionPackageProperties { IsPortalExtension = true, Version = "1.0.0" }
		}
	};

	[Test]
	public void CreateMetaDataFile_WritesAFile_WhenAssetsFolderDoesNotExist()
	{
		var observer = MakeObserver();
		Assert.That(Directory.Exists(Path.Combine(_extensionDir, "assets")), Is.False, "precondition");

		observer.CreateMetaDataFile();

		Assert.That(File.Exists(observer.MetadataPath), Is.True, "metadata.json should be a file");
		Assert.That(Directory.Exists(observer.MetadataPath), Is.False, "metadata.json must not be a directory");
		Assert.That(File.ReadAllText(observer.MetadataPath), Does.Contain("MyExt"));
	}

	[Test]
	public void CreateMetaDataFile_ReplacesAStaleDirectoryLeftByTheOldBug()
	{
		var observer = MakeObserver();
		Directory.CreateDirectory(observer.MetadataPath);
		Assert.That(Directory.Exists(observer.MetadataPath), Is.True, "precondition");

		observer.CreateMetaDataFile();

		Assert.That(File.Exists(observer.MetadataPath), Is.True);
		Assert.That(Directory.Exists(observer.MetadataPath), Is.False);
	}

	[Test]
	public void CreateMetaDataFile_OverwritesAnExistingMetadataFile()
	{
		var observer = MakeObserver();
		Directory.CreateDirectory(Path.Combine(_extensionDir, "assets"));
		File.WriteAllText(observer.MetadataPath, "stale");

		observer.CreateMetaDataFile();

		Assert.That(File.ReadAllText(observer.MetadataPath), Does.Not.Contain("stale"));
		Assert.That(File.ReadAllText(observer.MetadataPath), Does.Contain("MyExt"));
	}
}
