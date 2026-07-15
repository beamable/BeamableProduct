using System;
using System.IO;
using System.Linq;
using System.Text;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxFileServiceTests
{
	private string _root = null!;
	private SandboxFileService _fs = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "sandbox-fs-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
		_fs = new SandboxFileService();
	}

	[TearDown]
	public void TearDown()
	{
		try { Directory.Delete(_root, recursive: true); } catch { }
	}

	private string P(params string[] segments) => Path.Combine(new[] { _root }.Concat(segments).ToArray());

	// --- ListDir -----------------------------------------------------------------------

	[Test]
	public void ListDir_EmptyDir_ReturnsEmpty()
	{
		var listing = _fs.ListDir(_root, showHidden: false);
		Assert.That(listing.Entries, Is.Empty);
	}

	[Test]
	public void ListDir_MixedEntries_DirectoriesFirst_AlphaWithinKind()
	{
		Directory.CreateDirectory(P("b-dir"));
		Directory.CreateDirectory(P("a-dir"));
		File.WriteAllText(P("z-file.txt"), "");
		File.WriteAllText(P("a-file.txt"), "");

		var listing = _fs.ListDir(_root, showHidden: false);
		var names = listing.Entries.Select(e => e.Name).ToArray();
		Assert.That(names, Is.EqualTo(new[] { "a-dir", "b-dir", "a-file.txt", "z-file.txt" }));
	}

	[Test]
	public void ListDir_SkipsHiddenByDefault()
	{
		File.WriteAllText(P(".env"), "secret=1");
		File.WriteAllText(P("visible.txt"), "");
		var listing = _fs.ListDir(_root, showHidden: false);
		Assert.That(listing.Entries.Select(e => e.Name), Does.Not.Contain(".env"));
		Assert.That(listing.Entries.Select(e => e.Name), Contains.Item("visible.txt"));
	}

	[Test]
	public void ListDir_ShowHidden_IncludesDotfiles()
	{
		File.WriteAllText(P(".env"), "");
		var listing = _fs.ListDir(_root, showHidden: true);
		Assert.That(listing.Entries.Select(e => e.Name), Contains.Item(".env"));
	}

	[Test]
	public void ListDir_SkipsDefaultExcludedDirs()
	{
		Directory.CreateDirectory(P("node_modules"));
		Directory.CreateDirectory(P(".git"));
		Directory.CreateDirectory(P("bin"));
		Directory.CreateDirectory(P("obj"));
		Directory.CreateDirectory(P("src"));

		var listing = _fs.ListDir(_root, showHidden: true);
		var names = listing.Entries.Select(e => e.Name).ToArray();
		Assert.That(names, Does.Not.Contain("node_modules"));
		Assert.That(names, Does.Not.Contain(".git"));
		Assert.That(names, Does.Not.Contain("bin"));
		Assert.That(names, Does.Not.Contain("obj"));
		Assert.That(names, Contains.Item("src"));
	}

	[Test]
	public void ListDir_MissingDir_Throws()
	{
		var ex = Assert.Throws<SandboxFileException>(() => _fs.ListDir(P("nonexistent"), false));
		Assert.That(ex!.Code, Is.EqualTo("not-found"));
	}

	// --- Stat --------------------------------------------------------------------------

	[Test]
	public void Stat_File_ReturnsHashAndSize()
	{
		var path = P("foo.txt");
		File.WriteAllText(path, "hello");
		var stat = _fs.Stat(path);
		Assert.That(stat.Kind, Is.EqualTo("file"));
		Assert.That(stat.Size, Is.EqualTo(5));
		Assert.That(stat.ContentHash, Is.Not.Null.And.Length.EqualTo(64)); // SHA-256 hex
		Assert.That(stat.ModifiedAtUnixMs, Is.GreaterThan(0));
	}

	[Test]
	public void Stat_Dir_NoHash()
	{
		Directory.CreateDirectory(P("sub"));
		var stat = _fs.Stat(P("sub"));
		Assert.That(stat.Kind, Is.EqualTo("dir"));
		Assert.That(stat.ContentHash, Is.Null);
	}

	[Test]
	public void Stat_Missing_Throws()
	{
		var ex = Assert.Throws<SandboxFileException>(() => _fs.Stat(P("nope")));
		Assert.That(ex!.Code, Is.EqualTo("not-found"));
	}

	// --- Read --------------------------------------------------------------------------

	[Test]
	public void Read_FullFile_ReturnsContents()
	{
		var path = P("a.txt");
		File.WriteAllText(path, "hello world");
		var result = _fs.Read(path, null, null);
		Assert.That(result.Contents, Is.EqualTo("hello world"));
		Assert.That(result.Size, Is.EqualTo(11));
	}

	[Test]
	public void Read_Range_ReturnsSubstring()
	{
		var path = P("a.txt");
		File.WriteAllText(path, "0123456789");
		var result = _fs.Read(path, rangeStart: 2, rangeEnd: 7);
		Assert.That(result.Contents, Is.EqualTo("23456"));
	}

	[Test]
	public void Read_OversizeUnranged_Throws()
	{
		// Write a file just over the 5 MB cap.
		var path = P("big.txt");
		var content = new string('x', SandboxFileService.DefaultReadCapBytes + 1);
		File.WriteAllText(path, content);
		var ex = Assert.Throws<SandboxFileException>(() => _fs.Read(path, null, null));
		Assert.That(ex!.Code, Is.EqualTo("too-large"));
	}

	[Test]
	public void Read_OversizeRanged_OK()
	{
		var path = P("big.txt");
		var content = new string('x', SandboxFileService.DefaultReadCapBytes + 1);
		File.WriteAllText(path, content);
		// Range request bypasses the cap; only the requested slice is materialized.
		var result = _fs.Read(path, rangeStart: 0, rangeEnd: 100);
		Assert.That(result.Contents.Length, Is.EqualTo(100));
	}

	[Test]
	public void Read_MissingFile_Throws()
	{
		var ex = Assert.Throws<SandboxFileException>(() => _fs.Read(P("nope.txt"), null, null));
		Assert.That(ex!.Code, Is.EqualTo("not-found"));
	}

	// --- Write -------------------------------------------------------------------------

	[Test]
	public void Write_NewFile_CreatesIt()
	{
		var path = P("new.txt");
		var result = _fs.Write(path, "hello", expectedContentHash: null);
		Assert.That(File.ReadAllText(path), Is.EqualTo("hello"));
		Assert.That(result.ContentHash, Is.Not.Null);
	}

	[Test]
	public void Write_CreatesParentDirs()
	{
		var path = P("nested", "deeper", "file.txt");
		_fs.Write(path, "content", null);
		Assert.That(File.Exists(path), Is.True);
	}

	[Test]
	public void Write_ExpectedHashMatches_Succeeds()
	{
		var path = P("a.txt");
		_fs.Write(path, "original", null);
		var currentHash = SandboxFileService.ComputeFileHash(path);
		Assert.DoesNotThrow(() => _fs.Write(path, "updated", expectedContentHash: currentHash));
		Assert.That(File.ReadAllText(path), Is.EqualTo("updated"));
	}

	[Test]
	public void Write_ExpectedHashMismatch_RaisesConflict_WithCurrentHash()
	{
		var path = P("a.txt");
		_fs.Write(path, "original", null);
		var stale = "0000000000000000000000000000000000000000000000000000000000000000";
		var ex = Assert.Throws<SandboxFileConflictException>(() => _fs.Write(path, "updated", expectedContentHash: stale));
		Assert.That(ex!.CurrentContentHash, Is.EqualTo(SandboxFileService.ComputeFileHash(path)));
		Assert.That(File.ReadAllText(path), Is.EqualTo("original"));
	}

	[Test]
	public void Write_Atomic_NoOrphanTempOnSuccess()
	{
		var path = P("a.txt");
		_fs.Write(path, "v1", null);
		_fs.Write(path, "v2", expectedContentHash: SandboxFileService.ComputeFileHash(path));
		var siblings = Directory.GetFiles(_root, "a.txt.tmp.*");
		Assert.That(siblings, Is.Empty);
	}

	// --- Delete ------------------------------------------------------------------------

	[Test]
	public void Delete_File_Removes()
	{
		var path = P("a.txt");
		File.WriteAllText(path, "x");
		_fs.Delete(path);
		Assert.That(File.Exists(path), Is.False);
	}

	[Test]
	public void Delete_Directory_RefusesAndExplains()
	{
		Directory.CreateDirectory(P("sub"));
		var ex = Assert.Throws<SandboxFileException>(() => _fs.Delete(P("sub")));
		Assert.That(ex!.Code, Is.EqualTo("is-directory"));
	}

	[Test]
	public void Delete_Missing_Throws()
	{
		Assert.Throws<SandboxFileException>(() => _fs.Delete(P("nope")));
	}

	// --- Rename ------------------------------------------------------------------------

	[Test]
	public void Rename_File_Moves()
	{
		var from = P("a.txt");
		var to = P("renamed", "b.txt");
		File.WriteAllText(from, "x");
		_fs.Rename(from, to);
		Assert.That(File.Exists(from), Is.False);
		Assert.That(File.ReadAllText(to), Is.EqualTo("x"));
	}

	[Test]
	public void Rename_Directory_Moves()
	{
		Directory.CreateDirectory(P("src"));
		File.WriteAllText(P("src", "f.txt"), "x");
		_fs.Rename(P("src"), P("dst"));
		Assert.That(Directory.Exists(P("src")), Is.False);
		Assert.That(File.ReadAllText(P("dst", "f.txt")), Is.EqualTo("x"));
	}

	[Test]
	public void Rename_MissingSource_Throws()
	{
		Assert.Throws<SandboxFileException>(() => _fs.Rename(P("nope"), P("dest")));
	}

	// --- MakeDir -----------------------------------------------------------------------

	[Test]
	public void MakeDir_Recursive_Creates()
	{
		_fs.MakeDir(P("a", "b", "c"));
		Assert.That(Directory.Exists(P("a", "b", "c")), Is.True);
	}

	[Test]
	public void MakeDir_Idempotent()
	{
		_fs.MakeDir(P("a"));
		Assert.DoesNotThrow(() => _fs.MakeDir(P("a")));
	}
}
