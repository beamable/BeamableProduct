using Beamable;
using Beamable.Runtime.LightBeams;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
	[Header("Scene references")]
	public RectTransform root;
	public CanvasGroup loadingBlocker;

	[Header("Asset references")]
	public FriendsExampleConfig config;

	async void Start()
	{

		var beamContext = BeamContext.Default;
		var lightBeam = await beamContext.CreateLightBeam(root, loadingBlocker, builder =>
		{
			builder.AddSingleton(config);

			builder.AddLightComponent(config.homePage);
			builder.AddLightComponent<PlayerFriendsBehaviour, FriendsDisplayModel>(config.friendsBehaviour);
		});
		
		await lightBeam.Scope.Start<HomePage>();
	}
}
