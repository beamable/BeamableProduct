using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using cli;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using tests.Examples.Init;

namespace tests.Examples.Init;

public partial class BeamInitFlows
{
    [TestCase(".")]
    public async Task ConfigFileContention_Patching(string initArg)
    {
        // it isn't great to pass null here, but we don't need to care about the binding context yet.
        var config = new ConfigService(null);
        config.SetWorkingDir(initArg);
        InEmptyDirectory(initArg);
        ResetConfigurator();

        var t = Task.Run(() =>
            {

                config.WriteConfig(c =>
                {
                    // the config is READ now...
                    Thread.Sleep(100);

                    // and after the sleep, we set something...
                    ConfigService.SetConfig("c", "d", c);
                });
            })
            ;

        var t2 = Task.Run(() =>
            {

                // at a similar time, try to set a to b. 
                config.WriteConfigString("a", "b");
            })
            ;

        await t;
        await t2;
        var configPath = Path.Combine(initArg, ".beamable/config.beam.json");
        var configJson = File.ReadAllText(configPath);
        var jObj = JsonConvert.DeserializeObject<JObject>(configJson);
        Console.WriteLine(configJson);

        Assert.IsTrue(jObj.TryGetValue("c", out var dToken), "there should be a key for 'c'");
        Assert.AreEqual("d", dToken.Value<string>(), "and it should have the value 'd'");
        
        
        Assert.IsTrue(jObj.TryGetValue("a", out var bToken), "there should be a key for 'a'");
        Assert.AreEqual("b", bToken.Value<string>(), "and it should have the value 'b'");
    }
    
    [TestCase(".")]
    public async Task ConfigFileContention(string initArg)
    {
        InEmptyDirectory(initArg);
        ResetConfigurator();

        var tasks = new List<Task<int>>();
        for (var i = 0; i < 200; i++)
        {
            var index = i;
            var t = Task.Run(() => RunFull(new string[]{"otel", "set-config", "Info", index.ToString(), "true", "--quiet"}, false));
            var t2 = Task.Run(() => RunFull(new string[]{"project", "add-paths", "--paths-to-ignore", $"toast{index}", "--quiet"}, false));
            
            tasks.Add(t);
            tasks.Add(t2);
        }

        var configPath = Path.Combine(initArg, ".beamable/config.beam.json");
		Assert.That(File.Exists(configPath), "there must be a config defaults file");

        foreach (var t in tasks)
        {
            var exitCode = await t;
            Assert.AreEqual(0, exitCode, "all tasks should have a successful exit code.");
        }
        
        var configJson = File.ReadAllText(configPath);
        var jObj = JsonConvert.DeserializeObject<JObject>(configJson);
        
        // TODO: make assertions about the jObj
    }
}