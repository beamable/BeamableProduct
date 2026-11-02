using System;
using System.Net.Http;
using System.Net.Security;
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
    
    [TestCase(true, "prod-api.studiogames.services/", "prod.content.studiogames.services")]
    [TestCase(true, "prod-api.studiogames.services/toast", "prod.content.studiogames.services")]

    // HOST as the orchestrator actually sets it: a websocket URI, sometimes with a port
    [TestCase(true, "wss://api.beamable.com/socket", "content.beamable.com")]
    [TestCase(true, "wss://api.beamable.com:443/socket", "content.beamable.com")]
    [TestCase(false, "wss://api.beamable.com/socket", "somewhere.else.com")]
    [TestCase(true, "wss://dev.api.beamable.com/socket", "dev-content.beamable.com")]
    [TestCase(true, "wss://prod-api.studiogames.services/socket", "prod-content.studiogames.services")]
    [TestCase(false, "wss://prod-api.studiogames.services/socket", "content.beamable.com")]
    [TestCase(true, "custom.domain.land:443", "content.domain.land")]

    // a local stack deploying to Fargate: HOST is two labels under beamable.com
    [TestCase(true, "wss://justin.ms-local.beamable.com/socket", "local-content.beamable.com")]
    [TestCase(false, "wss://justin.ms-local.beamable.com/socket", "somewhere.else.com")]
    [TestCase(false, "wss://justin.ms-local.beamable.com/socket", "notbeamable.com")]
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

    // Beamable's content CDNs serve publicly trusted certificates, so a bad one is rejected.
    // Private clouds keep skipping validation for their own domain.
    [TestCase(false, "wss://api.beamable.com/socket", "content.beamable.com", SslPolicyErrors.RemoteCertificateChainErrors)]
    [TestCase(false, "wss://api.beamable.com/socket", "content.beamable.com", SslPolicyErrors.RemoteCertificateNameMismatch)]
    [TestCase(false, null, "content.beamable.com", SslPolicyErrors.RemoteCertificateChainErrors)]
    [TestCase(false, "wss://justin.ms-local.beamable.com/socket", "local-content.beamable.com", SslPolicyErrors.RemoteCertificateChainErrors)]
    [TestCase(true, "wss://prod-api.studiogames.services/socket", "prod-content.studiogames.services", SslPolicyErrors.RemoteCertificateChainErrors)]
    [TestCase(false, "wss://prod-api.studiogames.services/socket", "somewhere.else.com", SslPolicyErrors.None)]
    public void CertificateErrors(bool expected, string host, string request, SslPolicyErrors errors)
    {
        var resolver = new DefaultContentResolver(new TestArgs
        {
            Host = host
        });
        var result = resolver.ServerCertificateCustomValidationCallback(new HttpRequestMessage
        {
            RequestUri = new Uri("https://" + request)
        }, null, null, errors);

        Assert.That(result, Is.EqualTo(expected), $"{request} with errors=[{errors}] against host=[{host}]");
    }
}