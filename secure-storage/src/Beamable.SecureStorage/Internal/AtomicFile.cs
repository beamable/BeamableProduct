using System;
using System.IO;

namespace Beamable.Security.Internal
{
	internal static class AtomicFile
	{
		internal static void Write(string path, byte[] bytes, Action<string, string>? replace = null)
		{
			string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					stream.Write(bytes, 0, bytes.Length);
					stream.Flush(true);
				}

				// Never delete the old snapshot before replacing it. If replacement is unsupported,
				// fail and leave the old snapshot intact instead of using a non-atomic fallback.
				if (File.Exists(path))
				{
					if (replace != null) { replace(temporaryPath, path); }
					else { File.Replace(temporaryPath, path, null); }
				}
				else
				{
					File.Move(temporaryPath, path);
				}
			}
			finally
			{
				try { File.Delete(temporaryPath); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
		}
	}
}
