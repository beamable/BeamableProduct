
namespace Beamable.Server.Editor.DockerCommands
{
	public class PruneImageCommand : DockerCommandReturnable<bool>
	{
		private readonly IDescriptor _descriptor;
		private readonly bool _all;

		public PruneImageCommand(IDescriptor descriptor, bool all = true)
		{
			_descriptor = descriptor;
			_all = all;
		}

		public override string GetCommandString()
		{
			var cmd = $"{DockerCmd} image prune --filter \"label=beamable-service-name={_descriptor.Name}\" -f {(_all ? "-a" : "")}";
			return cmd;
		}

		protected override void Resolve()
		{
			Promise.CompleteSuccess(true);
		}
	}
}
