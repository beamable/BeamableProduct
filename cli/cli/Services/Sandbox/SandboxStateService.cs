using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace cli.Services.Sandbox;

/// <summary>
/// Persists per-sandbox join codes to a local-only directory so <c>beam sandbox ps</c>
/// can recover the code after the user scrolls past <c>start</c>'s output. The file is
/// scoped per service name (e.g. <c>BeamSandbox_4827_abcd.code</c>) and mode 0600 on
/// POSIX. The realm token never gives an attacker access to these files, so a remote
/// same-account session can list the sandbox via discovery but cannot read its code.
/// </summary>
public sealed class SandboxStateService
{
	public const string SandboxDirName = "sandbox";

	private readonly string _dir;

	public SandboxStateService(string? overrideRoot = null)
	{
		var beamableRoot = overrideRoot ?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".beamable");
		_dir = Path.Combine(beamableRoot, SandboxDirName);
	}

	public string DirectoryPath => _dir;

	public string PathForService(string serviceName)
	{
		if (string.IsNullOrWhiteSpace(serviceName))
			throw new ArgumentException("serviceName must be non-empty", nameof(serviceName));
		return Path.Combine(_dir, serviceName + ".code");
	}

	public void WriteCode(string serviceName, string code)
	{
		Directory.CreateDirectory(_dir);
		var path = PathForService(serviceName);
		File.WriteAllText(path, code);
		TrySetOwnerOnlyPermissions(path);
	}

	public string? ReadCode(string serviceName)
	{
		var path = PathForService(serviceName);
		if (!File.Exists(path)) return null;
		try { return File.ReadAllText(path).Trim(); }
		catch (IOException) { return null; }
	}

	public void RemoveCode(string serviceName)
	{
		var path = PathForService(serviceName);
		try { if (File.Exists(path)) File.Delete(path); }
		catch (IOException) { /* best-effort */ }
	}

	public IEnumerable<(string serviceName, string code)> ListLocalSandboxes()
	{
		if (!Directory.Exists(_dir)) yield break;
		foreach (var file in Directory.EnumerateFiles(_dir, "*.code"))
		{
			var serviceName = Path.GetFileNameWithoutExtension(file);
			string code;
			try { code = File.ReadAllText(file).Trim(); }
			catch (IOException) { continue; }
			yield return (serviceName, code);
		}
	}

	[UnsupportedOSPlatform("windows")]
	private static void SetUnixFileMode(string path)
	{
		File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
	}

	private static void TrySetOwnerOnlyPermissions(string path)
	{
		if (OperatingSystem.IsWindows()) return;
		try { SetUnixFileMode(path); } catch { /* best-effort */ }
	}
}
