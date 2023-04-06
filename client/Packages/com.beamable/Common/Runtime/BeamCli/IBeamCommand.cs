namespace Beamable.Common.BeamCli
{
	public interface IBeamCommand
	{
		void SetCommand(string command);
		Promise Run();
	}

	public interface IBeamCommandFactory
	{
		IBeamCommand Create();
	}
}
