using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Content;

namespace Beamable.Editor.ContentService
{
	[BeamContextSystem]
	public static class EditorLocalContentRegistration
	{
		[RegisterBeamableDependencies(-999, RegistrationOrigin.RUNTIME)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.RemoveIfExists<ILocalContentSource>();
			builder.AddSingleton<ILocalContentSource>(provider =>
				provider.GetService<ContentParameterProvider>().EnableLocalContentInEditor
					? EditorLocalContentSource.ForContext(ContentConfiguration.Instance, EditorWorkspace())
					: new DisabledLocalContentSource());
		}

		private static async Promise<string> EditorWorkspace()
		{
			var editor = BeamEditorContext.Default;
			// Runtime registration can run before the editor has loaded its CLI configuration.
			if (editor.BeamCli.latestConfig == null) await editor.OnReady;
			return editor.BeamCli.latestConfig?.configPath;
		}
	}
}
