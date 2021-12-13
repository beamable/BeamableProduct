namespace Editor.Scripts.Versioning
{
  public static class VersionGenerator
  {
    public static string Generate()
    {
      return Git.GenerateSemanticCommitVersion();
    }
  }
}
