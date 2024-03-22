namespace cli.Utils;

public static class PathUtils
{
	public static string LocalizeSlashes(this string path)
	{
		return path.Replace('/', Path.DirectorySeparatorChar);
	}
}

public static class BFile
{
	public static bool Exists(string path) => File.Exists(path.LocalizeSlashes());
	public static string ReadAllText(string path) => File.ReadAllText(path.LocalizeSlashes());
}
