using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture;
using Beamable.Server;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Server.Clients
{
	public static class AuthExtensions
	{
		public static Promise<AttachExternalIdentityResponse> AttachIdentity<T>(this ISupportsFederatedLogin<T> client, string token, ChallengeSolution solution=null)
			where T : IThirdPartyCloudIdentity
		{
			var ctx = BeamContext.Default; // TODO: get context from client
			var identity = ctx.ServiceProvider.GetService<T>();
			return ctx.Api.AuthService.AttachIdentity(token, client.ServiceName, identity.UniqueName, solution);
		}

		public static Promise<DetachExternalIdentityResponse> DetachIdentity<T>(this ISupportsFederatedLogin<T> client, string userId)
			where T : IThirdPartyCloudIdentity
		{
			var ctx = BeamContext.Default; // TODO: get context from client
			var identity = ctx.ServiceProvider.GetService<T>();
			return ctx.Api.AuthService.DetachIdentity(client.ServiceName, userId, identity.UniqueName);
		}
		
		public static Promise<ExternalAuthenticationResponse> AuthorizeExternalIdentity<T>(this ISupportsFederatedLogin<T> client, string token, ChallengeSolution solution=null)
			where T : IThirdPartyCloudIdentity
		{
			var ctx = BeamContext.Default; // TODO: get context from client
			var identity = ctx.ServiceProvider.GetService<T>();
			return ctx.Api.AuthService.AuthorizeExternalIdentity(token, client.ServiceName, identity.UniqueName,
			                                                     solution);
		}
	}
}
