// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.BeamCli.Contracts;
using System;

namespace Beamable.Common.BeamCli
{
	public interface IBeamCommand
	{
		void SetCommand(string command);
		Promise Run();
		void Cancel();
		IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb);
		IBeamCommand On(Action<ReportDataPointDescription> cb);
		IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb);
		IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> cb);
	}

	public class BeamCommandWrapper
	{
		public IBeamCommand Command { get; set; }
		public Promise Run() => Command
			.Run();

		public void Cancel() => Command.Cancel();

		public BeamCommandWrapper()
		{
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb) => Command.OnError(cb);
		public IBeamCommand OnLog(Action<ReportDataPoint<CliLogMessage>> cb) => Command.On("logs", cb);
	}

	public interface IResultChannel
	{
		string ChannelName { get; }
	}

	public interface IBeamCommandFactory
	{
		IBeamCommand Create();
		void ClearAll();
	}

	public interface IBeamCommandArgs
	{
		string Serialize();
	}
}
