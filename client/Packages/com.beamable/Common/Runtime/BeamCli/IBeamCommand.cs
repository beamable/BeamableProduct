using System;

namespace Beamable.Common.BeamCli
{
	public interface IBeamCommand
	{
		void SetCommand(string command);
		Promise Run();
		IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb);
		IBeamCommand On(Action<ReportDataPointDescription> cb);
	}

	public interface IBeamCommandFactory
	{
		IBeamCommand Create();
	}

	public interface IBeamCommandArgs
	{
		string Serialize();
	}
}
