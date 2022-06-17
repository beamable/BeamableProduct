using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Editor.Alias;
using Beamable.Editor.UI.Model;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public class PublishService
	{
		private readonly BeamEditorContext _ctx;
		private readonly AliasService _alias;

		public PublishService(MicroservicesDataModel model, BeamEditorContext ctx, AliasService alias)
		{
			_ctx = ctx;
			_alias = alias;
		}
	}
}
