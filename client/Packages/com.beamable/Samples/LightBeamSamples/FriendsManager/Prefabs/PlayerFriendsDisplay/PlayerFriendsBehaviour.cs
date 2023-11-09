using Beamable;
using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FriendsDisplayModel
{
	public PlayerSocial social;
	public long playerId;
}

public class PlayerFriendsBehaviour : MonoBehaviour, ILightComponent<FriendsDisplayModel>
{
	[Header("Scene References")]
	public Transform displayListContainer;
	public TextMeshProUGUI playerIdLabel;
	public Button copyButton;
	public TMP_InputField friendIdInput;
	public Button addFriendButton;
	public Button showFriendsButton;
	public Button showReceivedInvitesButton;
	public Button showSentInvitesButton;
	public Button showBlockedButton;

	private PlayerSocial _social;
	private LightBeam _context;
	
	public async Promise OnInstantiated(LightBeam beam, FriendsDisplayModel model)
	{
		playerIdLabel.text = $"Player Id: {model.playerId}";

		_social = model.social;
		_context = beam;
		
		copyButton.HandleClicked(() =>
		{
			#if UNITY_EDITOR
			EditorGUIUtility.systemCopyBuffer = model.playerId.ToString();
			#endif
		});
		
		addFriendButton.HandleClicked( async () =>
		{
			var friendId = friendIdInput.text;

			if (string.IsNullOrEmpty(friendId))
			{
				Debug.Log("[FRIENDS] Friend id is either null or empty.");
			}

			await _social.Invite(long.Parse(friendId)).Error((e) =>
			{
				Debug.Log("[FRIENDS] An exception occurred while trying to add player: ");
				Debug.LogException(e);
			});
			Debug.Log("[FRIENDS] Finished adding friend");
		});
		
		showFriendsButton.HandleClicked(async () =>
		{
			await UpdateFriendsList();
		});
		
		showReceivedInvitesButton.HandleClicked(async () =>
		{
			await UpdateReceivedInviteList();
		});
		
		showSentInvitesButton.HandleClicked(async () =>
		{
			await UpdateSentInviteList();
		});
		
		showBlockedButton.HandleClicked(async () =>
		{
			await UpdateBlockedList();
		});

		_social.Friends.OnUpdated += () =>
		{
			
			var _ = UpdateFriendsList();
		};

		_social.ReceivedInvites.OnUpdated += () =>
		{
			var _ = UpdateReceivedInviteList();
		};
		
		_social.Blocked.OnUpdated += () =>
		{
			var _ = UpdateBlockedList();
		};
		
		_social.SentInvites.OnUpdated += () =>
		{
			var _ = UpdateSentInviteList();
		};

		await UpdateFriendsList();
	}

	private async Promise UpdateSentInviteList()
	{
		displayListContainer.Clear();
		
		var promises = new List<Promise<SentInviteDisplayBehaviour>>();

		foreach (SentFriendInvite invite in _social.SentInvites)
		{
			var p =  _context.Instantiate<SentInviteDisplayBehaviour, SentFriendInvite>(displayListContainer, invite);
			promises.Add(p);
		}
		var sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise UpdateBlockedList()
	{
		displayListContainer.Clear();
		
		var promises = new List<Promise<BlockedPlayersDisplayBehaviour>>();

		foreach (BlockedPlayer blockedPlayer in _social.Blocked)
		{
			var p =  _context.Instantiate<BlockedPlayersDisplayBehaviour, BlockedPlayer>(displayListContainer, blockedPlayer);
			promises.Add(p);
		}
		var sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise UpdateFriendsList()
	{
		displayListContainer.Clear();
		
		var promises = new List<Promise<FriendDisplayBehaviour>>();

		foreach (PlayerFriend friend in _social.Friends)
		{
			var p =  _context.Instantiate<FriendDisplayBehaviour, PlayerFriend>(displayListContainer, friend);
			promises.Add(p);
		}
		var sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise UpdateReceivedInviteList()
	{
		displayListContainer.Clear();
		
		var promises = new List<Promise<ReceivedInviteDisplayBehaviour>>();

		foreach (ReceivedFriendInvite invite in _social.ReceivedInvites)
		{
			var p = _context.Instantiate<ReceivedInviteDisplayBehaviour, ReceivedFriendInvite>(displayListContainer, invite);
			promises.Add(p);
		}
		var sequence = Promise.Sequence(promises);
		await sequence;
	}
}
