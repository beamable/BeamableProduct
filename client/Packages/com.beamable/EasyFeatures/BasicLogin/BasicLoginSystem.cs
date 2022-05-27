using Beamable.AccountManagement;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLogin
{
	public class BasicLoginSystem : BasicLoginView.ILoginDeps
	{
		public virtual UserView CurrentUser { get; set; }
		public virtual List<UserView> AvailableUsers { get; set; }

		private readonly BeamContext _context;
		private readonly StatsService _statsService;
		private readonly AccountManagementConfiguration _config;

		public BasicLoginSystem(BeamContext context, StatsService statsService, AccountManagementConfiguration config)
		{
			_context = context;
			_statsService = statsService;
			_config = config;
		}

		public async Promise RefreshData()
		{
			var promises = new List<Promise<UserView>>();
			promises.Add(GetCurrentUserView().Then(user => CurrentUser = user));
			AvailableUsers.Clear();
			var deviceUsers = await _context.Api.GetDeviceUsers();
			foreach (var deviceUser in deviceUsers)
			{
				var promise = GetUserView(deviceUser).Then(AvailableUsers.Add);
				promises.Add(promise);
			}
			await Promise.Sequence(promises);

		}

		protected virtual async Promise<UserView> GetCurrentUserView()
		{
			await _context.OnReady;
			User user = _context.AuthorizedUser;
			var view = new UserView
			{
				email = user.email,
				gamerTag = user.id,
				deviceIds = user.deviceIds.ToArray(),
				thirdPartyAssociations = user.thirdPartyAppAssociations.ToArray(),
				scopes = user.scopes.ToArray(),
				accessToken = _context.AccessToken.Token,
				refreshToken = _context.AccessToken.RefreshToken,
			};
			await ApplyStats(view, user.id);
			return view;
		}

		protected virtual async Promise<UserView> GetUserView(UserBundle bundle)
		{
			var view = new UserView
			{
				// pull out the immediately available data.
				email = bundle.User.email,
				gamerTag = bundle.User.id,
				deviceIds = bundle.User.deviceIds.ToArray(),
				thirdPartyAssociations = bundle.User.thirdPartyAppAssociations.ToArray(),
				scopes = bundle.User.scopes.ToArray(),
				accessToken = bundle.Token.access_token,
				refreshToken = bundle.Token.refresh_token
			};

			await ApplyStats(view, bundle.User.id);
			return view;
		}

		protected virtual async Promise ApplyStats(UserView view, long gamerTag)
		{
			var stats = await _statsService.GetStats("client", "public", "player", gamerTag);

			// pull out the alias stat.
			var aliasStatKey = _config.DisplayNameStat.StatKey ?? "alias";
			if (!stats.TryGetValue(aliasStatKey, out view.alias))
			{
				view.alias = _config.DisplayNameStat.DefaultValue ?? "Anonymous";
			}

			// get the avatar stat.
			var avatarStatKey = _config.AvatarStat.StatKey ?? "avatar";
			if (!stats.TryGetValue(avatarStatKey, out view.avatarId))
			{
				view.avatarId = _config.AvatarStat.DefaultValue ?? "1";
			}

			// get the subtext
			if (_config.SubtextStat)
			{
				var subtextStatKey = _config.SubtextStat.StatKey;
				if (!stats.TryGetValue(subtextStatKey, out view.subtext))
				{
					view.subtext = gamerTag.ToString();
				}
			} else {
				view.subtext = gamerTag.ToString();
			}
		}
	}
}
