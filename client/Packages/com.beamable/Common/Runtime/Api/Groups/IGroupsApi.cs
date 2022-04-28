using Beamable.Common.Api.Inventory;
using System.Collections.Generic;

namespace Beamable.Common.Api.Groups
{
	public interface IGroupsApi : ISupportsGet<GroupsView>
	{
		/// <summary>
		/// Get the <see cref="GroupUser"/> for a player, which contains all the players groups.
		/// Use the <see cref="GetGroup"/> method to resolve the full group data.
		/// </summary>
		/// <param name="gamerTag">The gamertag of a player</param>
		/// <returns>A <see cref="Promise{T}"/> containing the player's <see cref="GroupUser"/> data</returns>
		Promise<GroupUser> GetUser(long gamerTag);

		/// <summary>
		/// Get the <see cref="Group"/> data for some group.
		/// </summary>
		/// <param name="groupId">The id of a group that the current player is in.
		/// The group id can be found with the <see cref="GetUser"/> method,
		/// or through the <see cref="GetRecommendations"/> and <see cref="Search"/> methods.
		/// </param>
		/// <returns>A <see cref="Promise{T}"/> containing the <see cref="Group"/> data</returns>
		Promise<Group> GetGroup(long groupId);

		/// <summary>
		/// Disbanding a group will delete the group data from Beamable.
		/// This method can only be called by an admin, from a microservice, or by a user that is in a group with the disband ability.
		/// </summary>
		/// <param name="group">The group id to disband.</param>
		/// <returns>A <see cref="Promise{T}"/> representing the network call.</returns>
		Promise<EmptyResponse> DisbandGroup(long group);

		/// <summary>
		/// Remove the current player from a group.
		/// After a player leaves, the group will no longer appear in the <see cref="GetUser"/> method response.
		/// </summary>
		/// <param name="group">The group id to leave</param>
		/// <returns>A <see cref="Promise{T}"/> containing a <see cref="GroupMembershipResponse"/> to check that the Leave operation occurred correctly.</returns>
		Promise<GroupMembershipResponse> LeaveGroup(long group);

		/// <summary>
		/// Add the current player to a group.
		/// A player can only join a group when the group's <see cref="Group.enrollmentType"/> is set to "open".
		/// </summary>
		/// <param name="group">The group id to join</param>
		/// <returns>A <see cref="Promise{T}"/> containing a <see cref="GroupMembershipResponse"/> to check that the Join operation occurred correctly.</returns>
		Promise<GroupMembershipResponse> JoinGroup(long group);

		/// <summary>
		/// Send a message from the current player to the group to ask to join.
		/// This method only works when the group's <see cref="Group.enrollmentType"/> is set to "restricted".
		/// </summary>
		/// <param name="group">The group id to apply to</param>
		/// <returns>A <see cref="Promise{T}"/> representing the network call.</returns>
		Promise<EmptyResponse> Petition(long group);

		/// <summary>
		/// Get a recommended list of <see cref="Group"/>s for the current player to join.
		/// </summary>
		/// <returns>A <see cref="Promise{T}"/> containing a <see cref="GroupSearchResponse"/></returns>
		Promise<GroupSearchResponse> GetRecommendations();

		/// <summary>
		/// Get a list of <see cref="Group"/>s that match some criteria.
		/// </summary>
		/// <param name="name">An optional group name. This text will be a fuzzy text match</param>
		/// <param name="enrollmentTypes"></param>
		/// <param name="hasSlots"></param>
		/// <param name="scoreMin"></param>
		/// <param name="scoreMax"></param>
		/// <param name="sortField"></param>
		/// <param name="sortValue"></param>
		/// <param name="offset"></param>
		/// <param name="limit"></param>
		/// <returns></returns>
		Promise<GroupSearchResponse> Search(
		   string name = null,
		   List<string> enrollmentTypes = null,
		   bool? hasSlots = null,
		   long? scoreMin = null,
		   long? scoreMax = null,
		   string sortField = null,
		   int? sortValue = null,
		   int? offset = null,
		   int? limit = null
		);

		Promise<GroupCreateResponse> CreateGroup(GroupCreateRequest request);
		Promise<AvailabilityResponse> CheckAvailability(string name, string tag);
		Promise<EmptyResponse> SetGroupProps(long groupId, GroupUpdateProperties props);
		Promise<GroupMembershipResponse> Kick(long group, long gamerTag);
		Promise<EmptyResponse> SetRole(long group, long gamerTag, string role);
		Promise<EmptyResponse> MakeDonationRequest(long group, Currency currency);
		Promise<EmptyResponse> Donate(long group, long recipientId, long amount, bool autoClaim = true);
		Promise<EmptyResponse> ClaimDonations(long group);
		string AddQuery(string query, string key, string value);
	}
}
