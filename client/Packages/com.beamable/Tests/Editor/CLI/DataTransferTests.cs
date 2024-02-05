using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests.CLI
{
	public class DataTransferTests
	{
		private string _validJson;
		private string _invalidJson;
		private string _incompleteData;
		
		[SetUp]
		public void Setup()
		{
			var obj = new ReportDataPointDescription() { };
			_validJson = JsonUtility.ToJson(obj);
			_invalidJson = "{{\"ts\":1707160518420,\"type\":\"stream\",\"data\":{\"host\":\"https://dev.api.beamable.com\",\"cid\":\"1706624984549280\",\"pid\":\"DE_1706624984549283\"}}\n\n";
			_incompleteData = "{\"ts\":1707160518420,\"type\":";
		}

		[Test]
		public void TestCommandCheckForData_JustAJson()
		{
			var buffer = _validJson;
			var success = BeamCommand.CheckForData(ref buffer, out var result, out _);
			
			Assert.IsTrue(success);
		}
		
		[Test]
		public void TestCommandCheckForData_JsonWithMoreData()
		{
			var buffer = _validJson + "\n\n{";
			var success = BeamCommand.CheckForData(ref buffer, out var result, out _);
			
			Assert.IsTrue(success);
			Assert.IsTrue(buffer.Equals("{"));
		}
		
		[Test]
		public void TestCommandCheckForData_IncompleteData()
		{
			var buffer = "{\"ts\":1707160518420,\"type\":";
			var success = BeamCommand.CheckForData(ref buffer, out var result, out _);

			Assert.IsFalse(success);
			Assert.IsTrue(buffer.Equals(_incompleteData));
		}
		
		[Test]
		public void TestCommandCheckForData_WrongJson()
		{
			var buffer = _invalidJson;

			var rgx = new Regex("JSON parse error");
			LogAssert.Expect(LogType.Error, rgx);
			BeamCommand.CheckForData(ref buffer, out var result, out _);
		}
	}
}
