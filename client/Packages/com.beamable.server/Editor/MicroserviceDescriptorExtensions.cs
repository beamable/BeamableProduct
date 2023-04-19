using Beamable.Editor.Environment;

namespace Beamable.Server.Editor
{
	public static class MicroserviceDescriptorExtensions
	{
		public static bool IsSourceCodeAvailableLocally(this IDescriptor descriptor)
		{
			return PackageUtil.DoesFileExistLocally(descriptor.AttributePath);
		}
	}
}
