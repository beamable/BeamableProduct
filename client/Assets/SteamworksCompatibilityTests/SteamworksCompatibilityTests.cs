using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

public class SteamworksCompatibilityTests
{
    [Test]
    public void FirstpassSuppressionOnlyCoversSteamworks()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        Func<string, string> fullPath = path => Path.GetFullPath(Path.Combine(projectRoot, path)).Replace('\\', '/');
        var vendorDirectory = fullPath("Assets/Plugins/Steamworks.NET") + "/";
        var suppressionFile = fullPath("Assets/Plugins/BeamableCompatibility/GlobalSuppressions.cs");
        var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
        var firstpass = assemblies.SingleOrDefault(assembly => assembly.name == "Assembly-CSharp-firstpass");
        Assert.That(firstpass, Is.Not.Null, "Reassess Steamworks suppression placement if its assembly changes.");

        var sources = firstpass.sourceFiles.Select(fullPath).ToArray();
        Assert.That(sources, Does.Contain(suppressionFile));
        var vendorFiles = Directory.GetFiles(vendorDirectory, "*.cs", SearchOption.AllDirectories).Select(fullPath).ToArray();
        Assert.That(vendorFiles, Is.Not.Empty);
        Assert.That(sources, Is.EquivalentTo(vendorFiles.Concat(new[] { suppressionFile })),
            "UAC0008 is suppressed assembly-wide. Unrelated sources or a vendor assembly change require reassessment.");
    }
}
