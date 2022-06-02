using Beamable.AccountManagement;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using System.Collections.Generic;

namespace Beamable.EasyFeatures.BasicLogin
{
	public class BasicLoginSystem : ILoginDeps
	{
		public virtual UserView CurrentUser { get; set; }
		public virtual List<UserView> AvailableUsers { get; set; } = new List<UserView>();

		public virtual OptionalUserView AvailableSwitch { get; set; } = new OptionalUserView();

		public virtual LoginFlowState State { get; set; } = LoginFlowState.HOME;

		protected readonly BeamContext _context;
		protected readonly StatsService _statsService;
		protected readonly AccountManagementConfiguration _config;

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
				if (deviceUser.User.id == _context.PlayerId) continue; // skip current user, even if it also a device user.
				var promise = GetUserView(deviceUser).Then(AvailableUsers.Add);
				promises.Add(promise);
			}
			await Promise.Sequence(promises);
		}

		protected virtual Promise<UserView> GetCurrentUserView()
		{
			return UserView.GetCurrentUserView(_statsService, _config, _context);
		}

		protected virtual Promise<UserView> GetUserView(UserBundle bundle)
		{
			return UserView.GetUserView(_statsService, _config, bundle);
		}

		public void OfferUserSelection(UserView nextUser)
		{
			AvailableSwitch.SetValue(nextUser);
			// now what? need to switch view
		}

	}
}
