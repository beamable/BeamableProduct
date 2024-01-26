using System;

namespace Beamable.Common.BeamCli
{
	public interface IBeamCommand
	{
		void SetCommand(string command);
		Promise Run();
		IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb);
		IBeamCommand On(Action<ReportDataPointDescription> cb);
		IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb);
	}

	public class BeamCommandWrapper
	{
		public IBeamCommand Command { get; set; }
		public Promise Run() => Command
			.Run();

		public BeamCommandWrapper()
		{
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb) => Command.OnError(cb);
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
