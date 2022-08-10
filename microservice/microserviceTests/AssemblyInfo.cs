using microserviceTests;
using NUnit.Framework;

[assembly: TimeoutWithTeardown(12000), NonParallelizable, PreventExecutionContextLeaksAttribute]
