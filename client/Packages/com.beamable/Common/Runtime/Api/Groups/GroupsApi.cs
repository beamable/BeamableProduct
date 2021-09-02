using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;

namespace Beamable.Common.Api.Groups
{
    public class GroupsApi : IGroupsApi
    {
        public IUserContext Ctx { get; }
        public IBeamableRequester Requester { get; }

        public GroupsApi(IUserContext ctx, IBeamableRequester requester)
        {
            Ctx = ctx;
            Requester = requester;
        }

        public Promise<GroupUser> GetUser(long gamerTag)
        {
            return Requester.Request<GroupUser>(
               Method.GET,
               String.Format("/object/group-users/{0}", gamerTag)
            );
        }

        public Promise<Group> GetGroup(long groupId)
        {
            return Requester.Request<Group>(
               Method.GET,
               String.Format("/object/groups/{0}", groupId)
            );
        }

        public Promise<EmptyResponse> DisbandGroup(long group)
        {
            return Requester.Request<EmptyResponse>(
               Method.DELETE,
               String.Format("/object/groups/{0}", group)
            );
        }

        public Promise<GroupMembershipResponse> LeaveGroup(long group)
        {
            return Requester.Request<GroupMembershipResponse>(
               Method.DELETE,
               String.Format("/object/group-users/{0}/join", Ctx.UserId),
               new GroupMembershipRequest(group)
            );
        }

        public Promise<GroupMembershipResponse> JoinGroup(long group)
        {
            return Requester.Request<GroupMembershipResponse>(
               Method.POST,
               String.Format("/object/group-users/{0}/join", Ctx.UserId),
               new GroupMembershipRequest(group)
            );
        }

        public Promise<EmptyResponse> Petition(long group)
        {
            return Requester.Request<EmptyResponse>(
               Method.POST,
               String.Format("/object/groups/{0}/petition", group),
               ""
            );
        }

        public Promise<GroupSearchResponse> GetRecommendations()
        {
            return Requester.Request<GroupSearchResponse>(
               Method.GET,
               String.Format("/object/group-users/{0}/recommended", Ctx.UserId)
            );
        }

        public Promise<GroupSearchResponse> Search(
           string name = null,
           List<string> enrollmentTypes = null,
           bool? hasSlots = null,
           long? scoreMin = null,
           long? scoreMax = null,
           string sortField = null,
           int? sortValue = null,
           int? offset = null,
           int? limit = null
        )
        {
            string args = "";

            if (!string.IsNullOrEmpty(name)) { args = AddQuery(args, "name", name); }
            if (offset.HasValue) { args = AddQuery(args, "offset", offset.ToString()); }
            if (limit.HasValue) { args = AddQuery(args, "limit", limit.ToString()); }
            if (enrollmentTypes != null) { args = AddQuery(args, "enrollmentTypes", string.Join(",", enrollmentTypes.ToArray())); }
            if (hasSlots.HasValue) { args = AddQuery(args, "hasSlots", hasSlots.Value.ToString()); }
            if (scoreMin.HasValue) { args = AddQuery(args, "scoreMin", scoreMin.Value.ToString()); }
            if (scoreMax.HasValue) { args = AddQuery(args, "scoreMax", scoreMax.Value.ToString()); }
            if (!string.IsNullOrEmpty(sortField)) { args = AddQuery(args, "sortField", sortField); }
            if (sortValue.HasValue) { args = AddQuery(args, "sortValue", sortValue.Value.ToString()); }

            return Requester.Request<GroupSearchResponse>(
               Method.GET,
               String.Format("/object/group-users/{0}/search?{1}", Ctx.UserId, args)
            );
        }

        public Promise<GroupCreateResponse> CreateGroup(GroupCreateRequest request)
        {
            using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
            {
                var dict = new ArrayDict();
                if (!string.IsNullOrEmpty(request.tag))
                {
                    dict.Add("tag", request.tag);
                }

                dict.Add("enrollmentType", request.enrollmentType);
                dict.Add("requirement", request.requirement);
                dict.Add("maxSize", request.maxSize);
                dict.Add("name", request.name);

                var json = Json.Serialize(dict, pooledBuilder.Builder);
                return Requester.Request<GroupCreateResponse>(Method.POST, $"/object/group-users/{Ctx.UserId}/group", json);
            }
        }

        public Promise<AvailabilityResponse> CheckAvailability(string name, string tag)
        {
            string query = "";
            if (name != null)
            {
                query += "name=" + name;
            }
            if (tag != null)
            {
                if (name != null) { query += "&"; }
                query += "tag=" + tag;
            }
            return Requester.Request<AvailabilityResponse>(
               Method.GET,
               String.Format("/object/group-users/{0}/availability?{1}", Ctx.UserId, query)
            );
        }

        public Promise<EmptyResponse> SetGroupProps(long groupId, GroupUpdateProperties props)
        {
            using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
            {
                var dict = new ArrayDict();

                if (!string.IsNullOrEmpty(props.name))
                {
                    dict.Add("name", props.name);
                }

                if (!string.IsNullOrEmpty(props.tag))
                {
                    dict.Add("tag", props.tag);
                }

                if(!string.IsNullOrEmpty(props.enrollmentType))
                {
                    dict.Add("enrollmentType", props.enrollmentType);
                }

                if (props.motd != null)
                {
                    dict.Add("motd", props.motd);
                }

                if (props.slogan != null)
                {
                    dict.Add("slogan", props.slogan);
                }

                if (props.clientData != null)
                {
                    dict.Add("clientData", props.clientData);
                }

                if (props.requirement.HasValue)
                {
                    dict.Add("requirement", props.requirement.Value);
                }

                var json = Json.Serialize(dict, pooledBuilder.Builder);
                return Requester.Request<EmptyResponse>(
                   Method.PUT,
                   $"/object/groups/{groupId}",
                   json
                );
            }
        }

        public Promise<GroupMembershipResponse> Kick(long group, long gamerTag)
        {
            return Requester.Request<GroupMembershipResponse>(
               Method.DELETE,
               String.Format("/object/groups/{0}/member", group),
               new KickRequest(gamerTag)
            );
        }

        public Promise<EmptyResponse> SetRole(long group, long gamerTag, string role)
        {
            return Requester.Request<EmptyResponse>(
               Method.PUT,
               String.Format("/object/groups/{0}/role", group),
               new RoleChangeRequest(gamerTag, role)
            );
        }

        public Promise<EmptyResponse> MakeDonationRequest(long group, Currency currency)
        {
            return Requester.Request<EmptyResponse>(
               Method.POST,
               $"/object/groups/{group}/donations",
               new CreateDonationRequest(currency)
            );
        }

        public Promise<EmptyResponse> Donate(long group, long recipientId, long amount, bool autoClaim = true)
        {
            return Requester.Request<EmptyResponse>(
               Method.PUT,
               $"/object/groups/{group}/donations",
               new MakeDonationRequest(recipientId, amount, autoClaim)
            );
        }

        public Promise<EmptyResponse> ClaimDonations(long group)
        {
            return Requester.Request<EmptyResponse>(
               Method.PUT,
               $"/object/groups/{group}/donations/claim"
            );
        }

        public string AddQuery(string query, string key, string value)
        {
            if (query.Length == 0)
            {
                return key + "=" + value;
            }
            else
            {
                return query + "&" + key + "=" + value;
            }
        }

        public virtual Promise<GroupsView> GetCurrent(string scope = "")
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class GroupUser
    {
        public long gamerTag;
        public GroupMemberships member;
        public long updated;
    }

    [Serializable]
    public class GroupMemberships
    {
        public List<GroupMembership> guild;
    }

    [Serializable]
    public class GroupMembership
    {
        public long id;
        public List<long> subGroups;
        public long joined;
    }

    [Serializable]
    public class GroupsView
    {
        public List<GroupView> Groups;

        public void Update(GroupUser user, IEnumerable<Group> groups)
        {
            Groups = groups.Select(group =>
            {
                var membership = user.member.guild.Find(m => m.id == group.id);
                return new GroupView
                {
                    Group = group,
                    Joined = membership.joined
                };
            }).ToList();
        }
    }

    [Serializable]
    public class GroupView
    {
        public Group Group;
        public long Joined;
    }

    [Serializable]
    public class Group
    {
        public long id;
        public string name;
        public string tag;
        public string slogan;
        public string motd;
        public string enrollmentType;
        public long requirement;
        public int maxSize;
        public List<Member> members;
        public List<SubGroup> subGroups;
        public string clientData;

        public long created;
        public int freeSlots;

        public bool canDisband;
        public bool canUpdateEnrollment;
        public bool canUpdateMOTD;
        public bool canUpdateSlogan;
        public List<DonationRequest> donations;
    }

    [Serializable]
    public class Member
    {
        public long gamerTag;
        public string role;

        public bool canKick;
        public bool canPromote;
        public bool canDemote;
    }

    [Serializable]
    public class SubGroup
    {
        public string name;
        public long requirement;
        public List<Member> members;
    }

    [Serializable]
    public class GroupMembershipRequest
    {
        public long group;

        public GroupMembershipRequest(long group)
        {
            this.group = group;
        }
    }

    [Serializable]
    public class GroupMembershipResponse
    {
        public bool member;
    }

    [Serializable]
    public class GroupCreateRequest
    {
        public string name;
        public string tag;
        public string enrollmentType;
        public long requirement;
        public int maxSize;

        public GroupCreateRequest(string name, string tag, string enrollmentType, long requirement, int maxSize)
        {
            this.name = name;
            this.tag = tag;
            this.enrollmentType = enrollmentType;
            this.requirement = requirement;
            this.maxSize = maxSize;
        }
    }

    [Serializable]
    public class GroupCreateResponse
    {
        public GroupMetaData group;
    }

    [Serializable]
    public class GroupMetaData
    {
        public long id;
        public string name;
        public string tag;
    }

    [Serializable]
    public class GroupSearchResponse
    {
        public List<Group> groups;
    }

    [Serializable]
    public class AvailabilityResponse
    {
        public bool name;
        public bool tag;
    }

    [Serializable]
    public class KickRequest
    {
        public long gamerTag;
        public KickRequest(long gamerTag)
        {
            this.gamerTag = gamerTag;
        }
    }

    [Serializable]
    public class RoleChangeRequest
    {
        public long gamerTag;
        public string role;
        public RoleChangeRequest(long gamerTag, string role)
        {
            this.gamerTag = gamerTag;
            this.role = role;
        }
    }

    [Serializable]
    public class GroupUpdateProperties
    {
        public string slogan;
        public string motd;
        public string enrollmentType;
        public string clientData;
        public long? requirement;
        public string name;
        public string tag;
    }

    [Serializable]
    public class CreateDonationRequest
    {
        public string currencyId;
        public long amount;

        public CreateDonationRequest(Currency currency)
        {
            currencyId = currency.id;
            amount = currency.amount;

        }
    }

    [Serializable]
    public class MakeDonationRequest
    {
        public long recipientId;
        public long amount;
        public bool autoClaim;

        public MakeDonationRequest(long recipientId, long amount, bool autoClaim = true)
        {
            this.recipientId = recipientId;
            this.amount = amount;
            this.autoClaim = autoClaim;
        }
    }

    [Serializable]
    public class DonationRequest
    {
        // Id of the player who is requesting the donation.
        public long playerId;
        // Currency type and amount of the donation requested.
        public Currency currency;
        // Time this particular request was made.
        public long timeRequested;
        // List of all the members who have donated toward the request.
        public List<DonationEntry> progress;
    }

    [Serializable]
    public class DonationEntry
    {
        public long playerId;
        public long amount;
        public long time;
    }
}