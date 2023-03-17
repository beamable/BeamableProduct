using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using System;
using UnityEngine;

namespace Beamable.Editor
{
	public class EditorPlatformRequesterErrorHandler : IPlatformRequesterErrorHandler
	{
		private readonly BeamEditorContext _ctx;

		public EditorPlatformRequesterErrorHandler(BeamEditorContext ctx)
		{
			_ctx = ctx;
		}

		public Promise<T> HandleError<T>(Exception ex, string contentType, byte[] body, SDKRequesterOptions<T> opts)
		{
			if (ex is PlatformRequesterException platformError && platformError.Error.error == "ProjectIsArchived")
			{
				Debug.LogError("The realm you are using has been archived. The Beamable SDK is automatically signing you out. Please sign back in a select a non-archived realm.");
				_ctx.Logout(clearRealmPid: true);
			}
			throw ex;
		}
	}
}
