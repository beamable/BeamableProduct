using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common.Pooling;
using NUnit.Framework;

namespace microserviceTests;

public class StringBuilderPoolTests
{
    [Test]
    public async Task MultiThreadedAccess_Breaks()
    {
        var taskCount = 100_000;
        var threadCount = 1;
        var tasks = new Task[taskCount * threadCount];
        var threads = new List<Thread>();
        var failCount = 0;
        var successCount = 0;

        for (var t = 0; t < threadCount; t++)
        {
            var tIndex = t;
            var thread = new Thread(() =>
            {
                Thread.Sleep(100);
                for (var i = 0; i < taskCount; i++)
                {
                    var iIndex = i;
                    
                    Thread.Sleep(0);
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(1);
                            using var builder = StringBuilderPool.StaticPool.Spawn();
                            await Task.Delay(1);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Kaboom?");
                            Console.WriteLine($" {ex.Message}\n{ex.StackTrace}");
                            Interlocked.Increment(ref failCount);
                        }
                    });

                    var taskIndex = tIndex * taskCount + iIndex;
                    tasks[taskIndex] = task;
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }
        
        await Task.WhenAll(tasks);
        
        Assert.That(failCount, Is.Zero);
        Assert.That(successCount, Is.EqualTo(threadCount * taskCount));
    }
}