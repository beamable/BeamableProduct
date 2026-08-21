using cli.Services.Unity;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace tests.Unity;

/// <summary>
/// The contract for preparing <c>com.beamable/Common</c> during a Unity SDK release.
///
/// These tests are red on purpose. They state the intended behavior of the
/// delete -> repopulate -> prune sequence that replaces the current
/// delete-and-prune-in-one-pass cleanup, which shipped 62 directories without their metas in
/// 6.1.0-PREVIEW.RC1 and RC2. Making them green is the implementation.
///
/// The sequence under test:
///   1. DeleteGeneratedFiles          -- remove generated sources, keep every directory and meta
///   2. (download or copy new source) -- modelled here by writing files directly
///   3. PruneEmptyDirectoriesAndMetaFiles -- drop what the new source did not repopulate
///   4. EnsureFolderMetaFiles         -- backfill metas for directories the new source introduced
///
/// Step 4 exists because steps 1-3 alone never give a meta to a directory that is absent from the
/// committed Unity tree but present in the replacement source. That is not hypothetical: on this
/// branch, Runtime/BeamCli/Contracts/BundlesService is exactly such a directory.
/// </summary>
public class UnityCommonTreeCleanupTests
{
	private const string GuidLine = "guid: 00000000000000000000000000000001";

	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "unity-common-tree-cleanup-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
			Directory.Delete(_root, true);
	}

	/// <summary>Create a directory holding one generated source file, its file meta, and a directory meta.</summary>
	private string GivenGeneratedDirectory(string relativePath, string sourceFileName, string folderMetaContent)
	{
		var directory = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, sourceFileName), "// generated");
		File.WriteAllText(Path.Combine(directory, $"{sourceFileName}.meta"), "fileFormatVersion: 2");
		File.WriteAllText($"{directory}.meta", folderMetaContent);
		return directory;
	}

	/// <summary>Write a replacement source file, as the nuget extraction would.</summary>
	private void WhenReplacementSourceArrives(string directory, string sourceFileName)
	{
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, sourceFileName), "// replacement");
		File.WriteAllText(Path.Combine(directory, $"{sourceFileName}.meta"), "fileFormatVersion: 2");
	}

	private static readonly string[] GeneratedExtensions = { ".cs", ".cs.meta" };

	[Test]
	public void DeleteGeneratedFiles_RemovesSourcesButLeavesDirectoriesAndTheirMetas()
	{
		var directory = GivenGeneratedDirectory("Runtime/Api", "AliasHelper.cs", GuidLine);

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);

		Assert.That(Directory.GetFiles(directory), Is.Empty, "generated sources and their metas should be gone");
		Assert.That(Directory.Exists(directory), Is.True, "the directory itself must survive step one");
		Assert.That(File.Exists($"{directory}.meta"), Is.True, "the directory meta must survive step one");
	}

	[Test]
	public void RetainedDirectory_KeepsItsOriginalFolderMetaAndGuid()
	{
		var directory = GivenGeneratedDirectory("Runtime/Api", "AliasHelper.cs", GuidLine);

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		WhenReplacementSourceArrives(directory, "AliasHelper.cs");
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		Assert.That(Directory.Exists(directory), Is.True);
		Assert.That(File.ReadAllText($"{directory}.meta"), Is.EqualTo(GuidLine),
			"a directory the new source repopulated must keep its established GUID untouched");
	}

	[Test]
	public void RemovedDirectory_LosesItsDirectoryAndFolderMeta()
	{
		// The 6.0.0-cycle case: the committed tree has a directory the replacement source dropped.
		var directory = GivenGeneratedDirectory("Runtime/BeamCli/Contracts/BundlesService", "BundleTagInfo.cs", GuidLine);

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		// no replacement source arrives for this directory
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		Assert.That(Directory.Exists(directory), Is.False);
		Assert.That(File.Exists($"{directory}.meta"), Is.False, "an orphaned directory meta must not ship");
	}

	[Test]
	public void NewlyAddedDirectory_ReceivesAFolderMeta()
	{
		// The inverse case, live on this branch: the replacement source carries a directory the
		// committed Unity tree does not have, so there is no meta to preserve and one must be made.
		var added = Path.Combine(_root, "Runtime/BeamCli/Contracts/BundlesService");

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		WhenReplacementSourceArrives(added, "BundleTagInfo.cs");
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		Assert.That(Directory.Exists(added), Is.True);
		Assert.That(File.Exists($"{added}.meta"), Is.True,
			"a directory introduced by the replacement source must be given a meta, or Unity ignores it");
		Assert.That(File.ReadAllText($"{added}.meta"), Does.Contain("guid: "));
	}

	[Test]
	public void NestedRetainedAndRemovedDirectories_AreResolvedIndependently()
	{
		var retained = GivenGeneratedDirectory("Runtime/Api", "AliasHelper.cs", GuidLine);
		var retainedChild = GivenGeneratedDirectory("Runtime/Api/Analytics", "AnalyticsService.cs", GuidLine);
		var removedChild = GivenGeneratedDirectory("Runtime/Api/Retired", "RetiredService.cs", GuidLine);

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		WhenReplacementSourceArrives(retained, "AliasHelper.cs");
		WhenReplacementSourceArrives(retainedChild, "AnalyticsService.cs");
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		Assert.That(Directory.Exists(retained), Is.True);
		Assert.That(File.ReadAllText($"{retained}.meta"), Is.EqualTo(GuidLine));
		Assert.That(Directory.Exists(retainedChild), Is.True);
		Assert.That(File.ReadAllText($"{retainedChild}.meta"), Is.EqualTo(GuidLine));
		Assert.That(Directory.Exists(removedChild), Is.False);
		Assert.That(File.Exists($"{removedChild}.meta"), Is.False);
	}

	[Test]
	public void DirectoryHoldingAUnityOwnedAsset_IsNeverPruned()
	{
		var directory = GivenGeneratedDirectory("Runtime/SmallerJSON", "Json.cs", GuidLine);
		var assemblyDefinition = Path.Combine(directory, "SmallerJSON.asmdef");
		File.WriteAllText(assemblyDefinition, "{}");

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		// no replacement source arrives, but the asmdef means the directory is not empty
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		Assert.That(Directory.Exists(directory), Is.True);
		Assert.That(File.Exists(assemblyDefinition), Is.True);
		Assert.That(File.ReadAllText($"{directory}.meta"), Is.EqualTo(GuidLine));
	}

	[Test]
	public void FailedRepopulation_LeavesFolderMetadataIntact()
	{
		// If the download throws, prune never runs, so the tree must still be recoverable on retry:
		// every directory and every directory meta still present, only generated sources missing.
		var directory = GivenGeneratedDirectory("Runtime/Api", "AliasHelper.cs", GuidLine);

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);

		Assert.That(Directory.Exists(directory), Is.True);
		Assert.That(File.ReadAllText($"{directory}.meta"), Is.EqualTo(GuidLine));
	}

	[Test]
	public void AfterTheFullSequence_EveryDirectoryHasASiblingMeta()
	{
		// The invariant the release artifact must satisfy, and the one the shipped RC1 and RC2
		// tarballs violated 62 times over.
		var retained = GivenGeneratedDirectory("Runtime/Api", "AliasHelper.cs", GuidLine);
		GivenGeneratedDirectory("Runtime/Api/Retired", "RetiredService.cs", GuidLine);
		var added = Path.Combine(_root, "Runtime/Modules/Steam");

		UnityProjectUtil.DeleteGeneratedFiles(_root, GeneratedExtensions);
		WhenReplacementSourceArrives(retained, "AliasHelper.cs");
		WhenReplacementSourceArrives(added, "SteamService.cs");
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(_root);
		UnityProjectUtil.EnsureFolderMetaFiles(_root);

		var directoriesMissingMetas = Directory
			.GetDirectories(_root, "*", SearchOption.AllDirectories)
			.Where(directory => !File.Exists($"{directory}.meta"))
			.ToList();

		Assert.That(directoriesMissingMetas, Is.Empty,
			$"every importable directory needs a sibling meta; missing: {string.Join(", ", directoriesMissingMetas)}");
	}
}
