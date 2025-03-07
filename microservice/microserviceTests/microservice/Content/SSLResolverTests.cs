using System;
using System.Net.Http;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.Content;

public class SSLResolverTests
{
    // our dev environment has this annoying setup :( 
    [TestCase(true, "dev.api.beamable.com", "dev-content.beamable.com")]
    [TestCase(false, "dev.api.beamable.com", "somewhere.else.com")]

    [TestCase(true, null, "content.beamable.com")]
    [TestCase(false, null, "somewhere.else.com")]
    
    [TestCase(true, "localhost", "content.beamable.com")]
    [TestCase(false, "localhost", "somewhere.else.com")]

    [TestCase(true, "api.beamable.com", "content.beamable.com")]
    [TestCase(false, "api.beamable.com", "somewhere.else.com")]

    [TestCase(true, "custom.domain.land", "content.domain.land")]
    [TestCase(false, "custom.domain.land", "somewhere.else.com")]
    
    [TestCase(true, "prod-api.studiogames.services", "prod-content.studiogames.services")]
    [TestCase(false, "prod-api.studiogames.services", "somewhere.else.com")]

    [TestCase(true, "prod-api.studiogames.services", "prod.content.studiogames.services")]
    [TestCase(false, "prod-api.studiogames.services", "somewhere.else.com")]
    public void NoSslForHost(bool expected, string host, string request)
    {
        var resolver = new DefaultContentResolver(new TestArgs
        {
            Host = host
        });
        var result = resolver.ServerCertificateCustomValidationCallback(new HttpRequestMessage
        {
            RequestUri = new Uri("https://" + request)
        }, null, null, default);
        
        Assert.That(expected, Is.EqualTo(result), $"{request} should {(expected ? "not" : "")} care about SSL against host=[{host}]");
    }
}