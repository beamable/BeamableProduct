using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Beamable.Common.Dependencies;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice;

[TestFixture]
public class ArrayBodyParameterProviderTests
{
    [TestCase("[]")]
    [TestCase("[1,2]")]
    [TestCase("[{\"id\":\"event-1\"}]")]
    public void ArrayBody_WithoutParameters_PreservesBody(string body)
    {
        var context = CreateContext(body);
        var arguments = new AdaptiveParameterProvider(context)
            .GetParameters(CreateMethod(nameof(NoParameters)), null);

        Assert.That(arguments, Is.Empty);
        Assert.That(context.Body, Is.EqualTo(body));
    }

    [TestCase("[]")]
    [TestCase("[1,2]")]
    public void ArrayBody_WithInjection_ResolvesDependency(string body)
    {
        var dependency = new TestDependency();
        var builder = new DependencyBuilder();
        builder.AddSingleton(dependency);
        var provider = builder.Build();

        var arguments = new AdaptiveParameterProvider(CreateContext(body))
            .GetParameters(CreateMethod(nameof(InjectionOnly)), provider);

        Assert.That(arguments, Has.Length.EqualTo(1));
        Assert.That(arguments[0], Is.SameAs(dependency));
    }

    [TestCase("[]", nameof(BodyOnly))]
    [TestCase("[1,2]", nameof(BodyOnly))]
    [TestCase("[]", nameof(MixedParameters))]
    [TestCase("[1,2]", nameof(MixedParameters))]
    public void ArrayBody_WithBodyParameter_ReturnsActionable400(string body, string methodName)
    {
        // A null provider also verifies rejection happens before resolving dependencies.
        var exception = Assert.Throws<MicroserviceException>(() =>
            new AdaptiveParameterProvider(CreateContext(body))
                .GetParameters(CreateMethod(methodName), null));

        Assert.That(exception.ResponseStatus, Is.EqualTo(400));
        Assert.That(exception.Error, Is.EqualTo("inputParameterFailure"));
        Assert.That(exception.Message, Does.Contain("Context.Body"));
        Assert.That(exception.Message, Does.Contain("named method parameters"));
    }

    [TestCase("{}")]
    [TestCase("{\"payload\":[]}")]
    public void ObjectBody_WithoutParameters_RemainsAccepted(string body)
    {
        var arguments = new AdaptiveParameterProvider(CreateContext(body))
            .GetParameters(CreateMethod(nameof(NoParameters)), null);
        Assert.That(arguments, Is.Empty);
    }

    [TestCase("{\"items\":[1,2]}")]
    [TestCase("{\"payload\":[[1,2]]}")]
    public void ObjectBody_NamedAndLegacyArrayArguments_RemainAccepted(string body)
    {
        var arguments = new AdaptiveParameterProvider(CreateContext(body))
            .GetParameters(CreateMethod(nameof(BodyOnly)), null);
        Assert.That(arguments, Has.Length.EqualTo(1));
        Assert.That(arguments[0], Is.EqualTo(new[] { 1, 2 }));
    }

    [TestCase("null")]
    [TestCase("42")]
    [TestCase("\"text\"")]
    public void ScalarBody_IsNotImplicitlyAccepted(string body)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AdaptiveParameterProvider(CreateContext(body))
                .GetParameters(CreateMethod(nameof(NoParameters)), null));
    }

    private static MicroserviceRequestContext CreateContext(string body)
    {
        using var document = JsonDocument.Parse(body);
        return new MicroserviceRequestContext("test-cid", "test-pid")
        {
            BodyElement = document.RootElement.Clone()
        };
    }

    private static ServiceMethod CreateMethod(string name)
    {
        var parameters = typeof(ArrayBodyParameterProviderTests)
            .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).GetParameters();
        var method = new ServiceMethod
        {
            ParameterNames = parameters.Select(p => p.Name).ToList(),
            ParameterInfos = parameters.ToList(),
            Deserializers = new List<ParameterDeserializer>(),
            ParameterDeserializers = new Dictionary<string, ParameterDeserializer>()
        };
        foreach (var parameter in parameters)
        {
            ParameterDeserializer deserialize = json =>
                JsonSerializer.Deserialize(json, parameter.ParameterType);
            method.Deserializers.Add(deserialize);
            method.ParameterDeserializers.Add(parameter.Name, deserialize);
            method.ParameterSources.Add(parameter.Name,
                parameter.GetCustomAttribute<InjectAttribute>() != null
                    ? ParameterSource.Injection : ParameterSource.Body);
        }
        return method;
    }

    private static void NoParameters() { }
    private static void BodyOnly(int[] items) { }
    private static void InjectionOnly([Inject] TestDependency dependency) { }
    private static void MixedParameters([Inject] TestDependency dependency, int[] items) { }
    public class TestDependency { }
}
