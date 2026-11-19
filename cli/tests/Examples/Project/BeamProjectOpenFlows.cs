using System.IO;
using System.Threading.Tasks;
using cli.Services;
using Moq;
using NUnit.Framework;

namespace tests.Examples.Project;

public partial class BeamProjectNewFlows : CLITestExtensions
{
    [TestCase("Example", ".")]
    [TestCase("Example", "toast")]
    [TestCase("Example", "with a space")]
    public void Open(string serviceName, string dir)
    {
        Mock<IFileOpenerService> mockOpenable=null;
        { // setup
            NewProject_Init_NoSlnConfig(serviceName, dir);
            _mockObjects.Clear();
            
            Directory.SetCurrentDirectory(dir);
        }

        { // act
            RunFull(new string[] { "project", "open", "--quiet" }, assertExitCode: true, builder =>
            {
                builder.RemoveIfExists<IFileOpenerService>();
                builder.AddSingleton<IFileOpenerService>(p =>
                {
                    mockOpenable = new Mock<IFileOpenerService>();
                    mockOpenable
                        .Setup(x => x.OpenFileWithDefaultApp(It.Is<string>(str => str.Contains(dir))))
                        .Returns(() => Task.CompletedTask)
                        .Verifiable();
                    
                    return mockOpenable.Object;
                });
            });
            _mockObjects.Clear();

        }

        { // assert
            Assert.That(mockOpenable, Is.Not.Null);
            mockOpenable.Verify();
        }

    }

    [Test]
    public void Open_AddsProjectsMissingFromTheSolution()
    {
        const string serviceName = "Example";
        const string slnFile = "BeamableServices.sln";
        NewProject_Init_NoSlnConfig(serviceName, ".");
        _mockObjects.Clear();

        // A solution that lost its projects: `project open` makes a new one and must put the service back.
        File.Delete(slnFile);
        RunFull(new string[] { "project", "open", "--quiet", "--only-generate" }, assertExitCode: true);
        _mockObjects.Clear();
        Assert.That(File.ReadAllText(slnFile), Does.Contain($"{serviceName}.csproj"));

        // Opening again, with the service already in the solution, must also succeed. Since .NET SDK 11,
        // `dotnet sln add` fails for a project the solution already has.
        RunFull(new string[] { "project", "open", "--quiet", "--only-generate" }, assertExitCode: true);
        _mockObjects.Clear();
        Assert.That(File.ReadAllText(slnFile), Does.Contain($"{serviceName}.csproj"));
    }
}
