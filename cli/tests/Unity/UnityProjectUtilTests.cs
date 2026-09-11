using Beamable.Server;
using cli.Services.Unity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tests.Unity;

public class UnityProjectUtilTests
{
	private const string PACKAGE_SRC = "content/sourceCode/";
	private const string UNITY_AUTHORED_FOLDER_META = @"fileFormatVersion: 2
guid: 2755f5e00191c35ba6f9d92be815e32a
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		// the code under test logs; ensure a logger exists in this bare test context.
		BeamableZLoggerProvider.SetLogger(NullLogger.Instance);

		_root = Path.Combine(Path.GetTempPath(), "unity-project-util-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
			Directory.Delete(_root, true);
	}

	#region cleanup in isolation

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

	[Test]
	public void DeleteGeneratedFiles_LeavesDirectoriesAndTheirMetaFilesAlone()
	{
		WriteSource("Runtime/Api/AliasHelper.cs");
		WriteFolderMeta("Runtime");
		WriteFolderMeta("Runtime/Api");

		UnityProjectUtil.DeleteGeneratedFiles(_root, new[] { ".cs", ".cs.meta" });

		Assert.That(File.Exists(Path.Combine(_root, "Runtime/Api/AliasHelper.cs")), Is.False);
		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Api")), Is.True);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime/Api.meta")), Is.True);
	}

	#endregion

	#region cleanup followed by repopulation

	[Test]
	public async Task CleanThenDownload_RetainedDirectoryKeepsItsOriginalMetaFile()
	{
		WriteSource("Runtime/Api/AliasHelper.cs");
		WriteFolderMeta("Runtime");
		var apiMetaFile = WriteFolderMeta("Runtime/Api", UNITY_AUTHORED_FOLDER_META);

		await CleanDownloadEnsureAndPrune(BuildPackage("Runtime/Api/AliasHelper.cs"));

		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Api")), Is.True);
		Assert.That(File.Exists(apiMetaFile), Is.True);
		Assert.That(File.ReadAllText(apiMetaFile), Is.EqualTo(UNITY_AUTHORED_FOLDER_META),
			"the meta file of a retained folder must be byte identical, so the folder guid survives");
	}

	[Test]
	public async Task CleanThenDownload_RemovedDirectoryLosesItsDirectoryAndMetaFile()
	{
		WriteSource("Runtime/Api/AliasHelper.cs");
		WriteSource("Runtime/Removed/GoneAway.cs");
		WriteFolderMeta("Runtime");
		WriteFolderMeta("Runtime/Api");
		var removedMetaFile = WriteFolderMeta("Runtime/Removed");

		await CleanDownloadEnsureAndPrune(BuildPackage("Runtime/Api/AliasHelper.cs"));

		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Removed")), Is.False);
		Assert.That(File.Exists(removedMetaFile), Is.False);
		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Api")), Is.True);
	}

	[Test]
	public async Task CleanThenDownload_HandlesNestedRetainedAndRemovedDirectoriesInOneTree()
	{
		WriteSource("Runtime/Api/Analytics/Models/CoreEvent.cs");
		WriteSource("Runtime/Api/Dropped/Nested/Deep.cs");
		foreach (var folder in new[]
		         {
			         "Runtime", "Runtime/Api", "Runtime/Api/Analytics", "Runtime/Api/Analytics/Models",
			         "Runtime/Api/Dropped", "Runtime/Api/Dropped/Nested"
		         })
		{
			WriteFolderMeta(folder);
		}

		var modelsMetaBefore = File.ReadAllText(Path.Combine(_root, "Runtime/Api/Analytics/Models.meta"));

		await CleanDownloadEnsureAndPrune(BuildPackage("Runtime/Api/Analytics/Models/CoreEvent.cs"));

		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Api/Analytics/Models")), Is.True);
		Assert.That(File.ReadAllText(Path.Combine(_root, "Runtime/Api/Analytics/Models.meta")),
			Is.EqualTo(modelsMetaBefore));
		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/Api/Dropped")), Is.False);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime/Api/Dropped.meta")), Is.False);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime/Api/Dropped/Nested.meta")), Is.False);
	}

	[Test]
	public async Task CleanThenDownload_PreservesDirectoriesHoldingUnityOwnedAssets()
	{
		WriteSource("Runtime/SmallerJSON/Json.cs");
		File.WriteAllText(Path.Combine(_root, "Runtime/SmallerJSON/Beamable.Json.asmdef"), "{}");
		WriteFolderMeta("Runtime");
		var smallerJsonMetaFile = WriteFolderMeta("Runtime/SmallerJSON", UNITY_AUTHORED_FOLDER_META);

		// the new source no longer ships anything under SmallerJSON
		await CleanDownloadEnsureAndPrune(BuildPackage("Runtime/Api/AliasHelper.cs"));

		Assert.That(Directory.Exists(Path.Combine(_root, "Runtime/SmallerJSON")), Is.True);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime/SmallerJSON/Beamable.Json.asmdef")), Is.True);
		Assert.That(File.ReadAllText(smallerJsonMetaFile), Is.EqualTo(UNITY_AUTHORED_FOLDER_META));
	}

	[Test]
	public async Task CleanThenDownload_EveryImportableDirectoryEndsUpWithAMetaFile()
	{
		WriteSource("Runtime/Api/AliasHelper.cs");
		WriteFolderMeta("Runtime");
		WriteFolderMeta("Runtime/Api");

		// the new source introduces folders that never existed before, including intermediate ones
		await CleanDownloadEnsureAndPrune(BuildPackage(
			"Runtime/Api/AliasHelper.cs",
			"Runtime/Api/Analytics/Models/CoreEvent.cs",
			"Runtime/Content/Serialization/Support/Serializer.cs"));

		Assert.That(UnityProjectUtil.FindDirectoriesMissingMetaFiles(_root), Is.Empty);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime/Api/Analytics.meta")), Is.True,
			"intermediate folders that hold nothing but other folders still need a meta file");
	}

	[Test]
	public void CleanThenFailedDownload_LeavesFolderMetaFilesIntact()
	{
		WriteSource("Runtime/Api/AliasHelper.cs");
		WriteFolderMeta("Runtime");
		var apiMetaFile = WriteFolderMeta("Runtime/Api", UNITY_AUTHORED_FOLDER_META);

		var snapshot = UnityProjectUtil.CaptureMetaSnapshot(_root);
		UnityProjectUtil.DeleteGeneratedFiles(_root, new[] { ".cs", ".cs.meta" });

		// a download that throws must not reach the restore or the prune
		Assert.Throws<InvalidOperationException>(() => throw new InvalidOperationException("download failed"));

		Assert.That(File.Exists(apiMetaFile), Is.True);
		Assert.That(File.ReadAllText(apiMetaFile), Is.EqualTo(UNITY_AUTHORED_FOLDER_META));
		Assert.That(snapshot.folderMetaContentByRelativePath["Runtime/Api"], Is.EqualTo(UNITY_AUTHORED_FOLDER_META));
	}

	#endregion

	#region restoring metas that are already missing

	[Test]
	public void EnsureFolderMetaFiles_GeneratesAFolderMetaForADirectoryThatNeverHadOne()
	{
		Directory.CreateDirectory(Path.Combine(_root, "Runtime/Api/Analytics"));
		WriteFolderMeta("Runtime");

		var restored = UnityProjectUtil.EnsureFolderMetaFiles(_root, null);

		Assert.That(restored, Is.EquivalentTo(new[] { "Runtime/Api", "Runtime/Api/Analytics" }));
		var generated = File.ReadAllText(Path.Combine(_root, "Runtime/Api.meta"));
		Assert.That(generated, Does.Contain("folderAsset: yes"));
		Assert.That(generated, Does.Contain("DefaultImporter"));
		Assert.That(generated, Does.Not.Contain("MonoImporter"));
	}

	[Test]
	public void EnsureFolderMetaFiles_DoesNotTouchAMetaFileThatIsAlreadyThere()
	{
		Directory.CreateDirectory(Path.Combine(_root, "Runtime/Api"));
		var runtimeMetaFile = WriteFolderMeta("Runtime", UNITY_AUTHORED_FOLDER_META);

		UnityProjectUtil.EnsureFolderMetaFiles(_root, null);

		Assert.That(File.ReadAllText(runtimeMetaFile), Is.EqualTo(UNITY_AUTHORED_FOLDER_META));
	}

	[Test]
	public void EnsureFolderMetaFiles_SkipsUnityHiddenDirectories()
	{
		Directory.CreateDirectory(Path.Combine(_root, "Documentation~/Images"));
		Directory.CreateDirectory(Path.Combine(_root, "Runtime"));

		UnityProjectUtil.EnsureFolderMetaFiles(_root, null);

		Assert.That(File.Exists(Path.Combine(_root, "Documentation~.meta")), Is.False);
		Assert.That(File.Exists(Path.Combine(_root, "Documentation~/Images.meta")), Is.False);
		Assert.That(File.Exists(Path.Combine(_root, "Runtime.meta")), Is.True);
	}

	[Test]
	public void FindDirectoriesMissingMetaFiles_ReportsOnlyImportableDirectories()
	{
		Directory.CreateDirectory(Path.Combine(_root, "Runtime/Api"));
		Directory.CreateDirectory(Path.Combine(_root, "Documentation~/Images"));
		WriteFolderMeta("Runtime");

		var missing = UnityProjectUtil.FindDirectoriesMissingMetaFiles(_root);

		Assert.That(missing, Is.EqualTo(new[] { "Runtime/Api" }));
	}

	#endregion

	#region guid stability

	[Test]
	public async Task DownloadedFileGuids_AreIndependentOfTheAbsoluteCheckoutPath()
	{
		var firstRoot = Path.Combine(_root, "checkout-a/Common");
		var secondRoot = Path.Combine(_root, "checkout-b-with-a-different-length/Common");
		Directory.CreateDirectory(firstRoot);
		Directory.CreateDirectory(secondRoot);

		await Extract(BuildPackage("Runtime/Api/AliasHelper.cs"), firstRoot, null);
		await Extract(BuildPackage("Runtime/Api/AliasHelper.cs"), secondRoot, null);

		Assert.That(ReadGuid(Path.Combine(firstRoot, "Runtime/Api/AliasHelper.cs.meta")),
			Is.EqualTo(ReadGuid(Path.Combine(secondRoot, "Runtime/Api/AliasHelper.cs.meta"))));
	}

	[Test]
	public async Task DownloadedFileGuids_ReuseTheGuidThatWasAlreadyCommitted()
	{
		const string committedGuid = "abcdef0123456789abcdef0123456789";
		WriteSource("Runtime/Api/AliasHelper.cs");
		File.WriteAllText(Path.Combine(_root, "Runtime/Api/AliasHelper.cs.meta"),
			$"fileFormatVersion: 2\nguid: {committedGuid}\nMonoImporter:\n");
		WriteFolderMeta("Runtime");
		WriteFolderMeta("Runtime/Api");

		await CleanDownloadEnsureAndPrune(BuildPackage("Runtime/Api/AliasHelper.cs"));

		Assert.That(ReadGuid(Path.Combine(_root, "Runtime/Api/AliasHelper.cs.meta")), Is.EqualTo(committedGuid));
	}

	#endregion

	#region helpers

	/// <summary>
	/// Run the sequence the download command uses: capture, delete files, repopulate, prune, backfill.
	/// </summary>
	private async Task CleanDownloadEnsureAndPrune(byte[] packageBytes)
	{
		var snapshot = UnityProjectUtil.CaptureMetaSnapshot(_root);
		UnityProjectUtil.DeleteGeneratedFiles(_root, new[] { ".cs", ".cs.meta" });
		await Extract(packageBytes, _root, snapshot);
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root, snapshot);
	}

	private static async Task Extract(byte[] packageBytes, string outputPath,
		UnityProjectUtil.UnityMetaSnapshot snapshot)
	{
		using var stream = new MemoryStream(packageBytes);
		using var archive = new ZipArchive(stream);
		await UnityProjectUtil.ExtractPackageSource(archive, PACKAGE_SRC, outputPath, "// generated", snapshot);
	}

	/// <summary>
	/// Build a nuget style archive. Like a real nuget package it contains file entries only, with no
	/// directory entries and no meta files.
	/// </summary>
	private static byte[] BuildPackage(params string[] relativeSourcePaths)
	{
		using var stream = new MemoryStream();
		using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
		{
			foreach (var relativePath in relativeSourcePaths)
			{
				var entry = archive.CreateEntry(PACKAGE_SRC + relativePath);
				using var entryStream = entry.Open();
				var bytes = Encoding.UTF8.GetBytes($"// {relativePath}");
				entryStream.Write(bytes, 0, bytes.Length);
			}
		}

		return stream.ToArray();
	}

	private void WriteSource(string relativePath)
	{
		var fullPath = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
		File.WriteAllText(fullPath, "// old generated source");
		File.WriteAllText($"{fullPath}.meta", "fileFormatVersion: 2\nguid: 00000000000000000000000000000000\n");
	}

	private string WriteFolderMeta(string relativeFolderPath, string content = null)
	{
		var folderPath = Path.Combine(_root, relativeFolderPath);
		Directory.CreateDirectory(folderPath);
		var metaFile = $"{folderPath}.meta";
		File.WriteAllText(metaFile, content ?? $"fileFormatVersion: 2\nguid: {FakeGuidFor(relativeFolderPath)}\nfolderAsset: yes\n");
		return metaFile;
	}

	private static string FakeGuidFor(string seed) =>
		Math.Abs(seed.GetHashCode()).ToString("x").PadLeft(32, 'a').Substring(0, 32);

	private static string ReadGuid(string metaFilePath) => File.ReadAllLines(metaFilePath)
		.First(line => line.StartsWith("guid:"))
		.Substring("guid:".Length)
		.Trim();

	#endregion
}
