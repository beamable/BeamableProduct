using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.Content;

public class BakePublishTargetTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task PublishKeepsOutputDirectoryAndRealmArgumentsSeparate(bool windowsSeparators)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "microservice/microservice/Targets/Beamable.Microservice.Runtime.targets")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null, "Could not locate the runtime targets in the checkout.");

        var directory = Path.Combine(Path.GetTempPath(), "beam bake publish " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // Exercise the production target with a recording executable, independently of the other
            // service build hooks. On Windows the SDK supplies PublishDir with a trailing backslash.
            var targets = XDocument.Load(Path.Combine(root.FullName, "microservice/microservice/Targets/Beamable.Microservice.Runtime.targets"));
            var bakeTarget = targets.Root.Elements("Target").Single(t => (string)t.Attribute("Name") == "BeamBakeServerContent");
            new XDocument(new XElement("Project", bakeTarget)).Save(Path.Combine(directory, "Bake.targets"));
            await File.WriteAllTextAsync(Path.Combine(directory, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>");
            await File.WriteAllTextAsync(Path.Combine(directory, "Recorder.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <BeamProjectType>service</BeamProjectType>
                    <BeamBakeServerContent>true</BeamBakeServerContent>
                    <BeamBakeCid>123</BeamBakeCid>
                    <BeamBakePid>test</BeamBakePid>
                    <NuGetAudit>false</NuGetAudit>
                  </PropertyGroup>
                  <Import Project="Bake.targets" />
                  <Target Name="ConfigureRecorder" BeforeTargets="BeamBakeServerContent">
                    <PropertyGroup>
                      <BeamableDotnetPath>dotnet</BeamableDotnetPath>
                      <BeamableTool>&quot;$(TargetPath)&quot;</BeamableTool>
                      <PublishDir Condition="'$(SimulateWindowsSeparators)'=='true'">$(PublishDir.Replace('/', '\'))</PublishDir>
                    </PropertyGroup>
                  </Target>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "Program.cs"), """
                using System;
                using System.IO;
                using System.Text.Json;
                var output = args[Array.IndexOf(args, "--output-dir") + 1];
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "arguments.json"), JsonSerializer.Serialize(args));
                File.WriteAllText(Path.Combine(output, "bakedContent.bytes"), "content fixture");
                File.WriteAllText(Path.Combine(output, "bakedManifest.bytes"), "manifest fixture");
                """);

            var publishDirectory = Path.Combine(directory, "published support");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            foreach (var argument in new[] { "publish", "Recorder.csproj", "--output", publishDirectory, "--nologo", "--verbosity", "quiet", "-m:1", "-p:UseSharedCompilation=false" })
                process.StartInfo.ArgumentList.Add(argument);
            process.StartInfo.ArgumentList.Add("-p:SimulateWindowsSeparators=" + windowsSeparators.ToString().ToLowerInvariant());
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
            Assert.That(process.ExitCode, Is.Zero, await stdout + await stderr);

            var arguments = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Path.Combine(publishDirectory, "arguments.json")));
            Assert.That(arguments, Is.EqualTo(new[]
            {
                "content", "bake", "--source", "published", "--format", "server", "--manifest-id", "global",
                "--output-dir", publishDirectory.Replace('\\', '/') + "/", "--cid", "123", "--pid", "test"
            }));
            Assert.That(File.Exists(Path.Combine(publishDirectory, "bakedContent.bytes")), Is.True);
            Assert.That(File.Exists(Path.Combine(publishDirectory, "bakedManifest.bytes")), Is.True);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
