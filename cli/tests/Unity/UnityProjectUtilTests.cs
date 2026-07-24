using cli.Services.Unity;
using NUnit.Framework;
using System.IO;

namespace tests.Unity;

public class UnityProjectUtilTests
{
	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "unity-project-util-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
			Directory.Delete(_root, true);
	}

	[Test]
	public void DeleteAllFilesWithExtensionsAndEmptyDirectories_RemovesEmptyDirectoriesAndTheirMetaFiles()
	{
		var contractsDirectory = Path.Combine(_root, "Contracts");
		var bundlesServiceDirectory = Path.Combine(contractsDirectory, "BundlesService");
		var bundlesServiceMetaFile = $"{bundlesServiceDirectory}.meta";
		Directory.CreateDirectory(bundlesServiceDirectory);
		File.WriteAllText(Path.Combine(bundlesServiceDirectory, "BundleTagInfo.cs"), string.Empty);
		File.WriteAllText(Path.Combine(bundlesServiceDirectory, "BundleTagInfo.cs.meta"), string.Empty);
		File.WriteAllText(bundlesServiceMetaFile, string.Empty);

		UnityProjectUtil.DeleteAllFilesWithExtensionsAndEmptyDirectories(_root, new[] { ".cs", ".cs.meta" });

		Assert.That(Directory.Exists(_root), Is.True);
		Assert.That(Directory.Exists(bundlesServiceDirectory), Is.False);
		Assert.That(File.Exists(bundlesServiceMetaFile), Is.False);
	}

	[Test]
	public void DeleteAllFilesWithExtensionsAndEmptyDirectories_PreservesDirectoriesContainingUnityOwnedFiles()
	{
		var generatedDirectory = Path.Combine(_root, "Generated");
		var generatedDirectoryMetaFile = $"{generatedDirectory}.meta";
		var assemblyDefinitionFile = Path.Combine(generatedDirectory, "Beamable.Generated.asmdef");
		var generatedSourceFile = Path.Combine(generatedDirectory, "GeneratedSource.cs");
		Directory.CreateDirectory(generatedDirectory);
		File.WriteAllText(generatedSourceFile, string.Empty);
		File.WriteAllText(assemblyDefinitionFile, string.Empty);
		File.WriteAllText(generatedDirectoryMetaFile, string.Empty);

		UnityProjectUtil.DeleteAllFilesWithExtensionsAndEmptyDirectories(_root, new[] { ".cs", ".cs.meta" });

		Assert.That(File.Exists(generatedSourceFile), Is.False);
		Assert.That(Directory.Exists(generatedDirectory), Is.True);
		Assert.That(File.Exists(assemblyDefinitionFile), Is.True);
		Assert.That(File.Exists(generatedDirectoryMetaFile), Is.True);
	}
}
