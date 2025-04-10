using System.IO;
using Beamable.Server.Common;
using NUnit.Framework;

namespace tests.BigStreamTests;

public class BigStreamTests
{
    [Test]
    public void Simple()
    {
        var bs = new BigStream(maxStreamSize: 2);
        bs.Write(new byte[]{1,2,3,4,5}, 1, 3);
        
        Assert.That(bs.innerStreams.Count, Is.EqualTo(2));

        bs.Position = 0;
        
        var buffer = new byte[4];
        var read = bs.Read(buffer, 1, 3);
        Assert.That(read, Is.EqualTo(3));
        
        Assert.That(buffer[1], Is.EqualTo(2));
        Assert.That(buffer[2], Is.EqualTo(3));
        Assert.That(buffer[3], Is.EqualTo(4));

        bs.Position = 1;
        bs.Write(new byte[]{100,101}, 0, 2);
        bs.Position = 0;

        read = bs.Read(buffer, 0, 3);
        Assert.That(read, Is.EqualTo(3));

        Assert.That(buffer[0], Is.EqualTo(2));
        Assert.That(buffer[1], Is.EqualTo(100));
        Assert.That(buffer[2], Is.EqualTo(101));

        
        bs.Seek(0, SeekOrigin.Begin);
        Assert.That(bs.Position, Is.EqualTo(0));

        bs.Position = 3;
        bs.Seek(0, SeekOrigin.Current);
        Assert.That(bs.Position, Is.EqualTo(3));
        
        bs.Position = 1;
        bs.Seek(0, SeekOrigin.End);
        Assert.That(bs.Position, Is.EqualTo(3));


        bs.Position = 0;
        read = 1;
        var iterations = 0;
        while (read > 0 && iterations++ < 10)
        {
            read = bs.Read(buffer, 0, 1);
        }
        
        Assert.That(iterations, Is.EqualTo(4));
    }
}