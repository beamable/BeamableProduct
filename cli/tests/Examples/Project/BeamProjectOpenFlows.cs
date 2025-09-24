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
}
