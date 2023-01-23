using Beamable.Common.Api.Auth;
using Beamable.Common.Player;

namespace Beamable.Player
{
	[System.Serializable]
	public class ObservableUser : Observable<User>
	{
		public static implicit operator ObservableUser(User data) => new ObservableUser { Value = data };

	}
}
