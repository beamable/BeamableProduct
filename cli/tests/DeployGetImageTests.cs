using System.Text;
using cli;
using NUnit.Framework;

namespace tests;

public class DeployGetImageTests
{
    [Test]
    public void Test()
    {
        var success = BuildkitStatusUtil.TryGetImageId(
            "writing image sha256:09990e99bdada1c9cebef658b560b56cc59d0b9485d6e763528d24d5a9c49e26", out var image);
        
        Assert.That(success, Is.True);
        Assert.That(image, Is.EqualTo("sha256:09990e99bdada1c9cebef658b560b56cc59d0b9485d6e763528d24d5a9c49e26"));
    }

    [Test]
    public void Extract_FromActualCustomerLogs()
    {
        var str =
            "\r\n2025/07/28 09:33:53 http2: server: error reading preface from client //./pipe/dockerDesktopLinuxEngine: file has already been closed{\"vertexes\":[{\"digest\":\"sha256:fbce4a65adf1e25319ad129936c2d21f3d382d2d1cbdd096c5568e478d0b310a\",\"name\":\"[internal] load build definition from Dockerfile\"}]}{\"vertexes\":[{\"digest\":\"sha256:fbce4a65adf1e25319ad129936c2d21f3d382d2d1cbdd096c5568e478d0b310a\",\"name\":\"[internal] load build definition from Dockerfile\",\"started\":\"2025-07-28T16:33:53.5921041Z\",\"completed\":\"2025-07-28T16:33:53.5921553Z\"}]}";
        var builder = new StringBuilder();
        builder.Append(str);
        var extracted = ServicesBuildCommand.TryExtractAllMessages(builder, out var messages);
        
        Assert.IsTrue(extracted);
        
        Assert.AreEqual(2, messages.Count);
    }
    
    [Test]
    public void Extract_Hypothetical_After()
    {
        var str =
            "\r\n2025/07/28 09:33:53 http2: server: error reading preface from client //./pipe/dockerDesktopLinuxEngine: file has already been closed{\"vertexes\":[{\"digest\":\"sha256:fbce4a65adf1e25319ad129936c2d21f3d382d2d1cbdd096c5568e478d0b310a\",\"name\":\"[internal] load build definition from Dockerfile\"}]}{\"vertexes\":[{\"digest\":\"sha256:fbce4a65adf1e25319ad129936c2d21f3d382d2d1cbdd096c5568e478d0b310a\",\"name\":\"[internal] load build definition from Dockerfile\",\"started\":\"2025-07-28T16:33:53.5921041Z\",\"completed\":\"2025-07-28T16:33:53.5921553Z\"}]}abc{";
        var builder = new StringBuilder();
        builder.Append(str);
        var extracted = ServicesBuildCommand.TryExtractAllMessages(builder, out var messages);
        
        Assert.IsTrue(extracted);
        
        Assert.AreEqual(2, messages.Count);

        builder.Append("}a");
        
        extracted = ServicesBuildCommand.TryExtractAllMessages(builder, out messages);
        Assert.IsTrue(extracted);
        Assert.AreEqual(1, messages.Count);

    }
}