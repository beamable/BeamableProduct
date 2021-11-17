using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Mail
{
	public abstract class AbsMailApi : IMailApi
	{
		protected IBeamableRequester Requester
		{
			get;
		}

		protected IUserContext Ctx
		{
			get;
		}

		public const string SERVICE_NAME = "mail";

		protected AbsMailApi(IBeamableRequester requester, IUserContext ctx)
		{
			Requester = requester;
			Ctx = ctx;
		}

		public Promise<SearchMailResponse> SearchMail(SearchMailRequest request)
		{
			var url = $"/object/mail/{Ctx.UserId}/search";

			using (var pooledBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				var dict = request.Serialize();
				var json = Json.Serialize(dict, pooledBuilder.Builder);
				return Requester.Request<SearchMailResponse>(Method.POST, url, json);
			}
		}

		public Promise<ListMailResponse> GetMail(string category, long startId = 0, long limit = 100)
		{
			const string key = "search";
			var req = new SearchMailRequest(
				new SearchMailRequestClause
				{
					name = key,
					categories = new[] { category },
					states = new[] { "Read", "Unread" },
					limit = limit,
					start = startId > 0 ? (long?)(startId) : null
				}
			);
			return SearchMail(req).Map(res =>
			{
				var content = res.results.Find(set => set.name == key)?.content;
				return new ListMailResponse { result = content };
			});
		}

		/// <summary>
		/// Must be sent from an admin user or a microservice.
		/// </summary>
		/// <param name="request">Structure holding 1 or more messages to send and to whom.</param>
		/// <returns></returns>
		public Promise<EmptyResponse> SendMail(MailSendRequest request)
		{
			return Requester.Request<EmptyResponse>(
				Method.POST,
				$"/basic/mail/bulk",
				request
			);
		}

		public Promise<EmptyResponse> Update(MailUpdateRequest updates)
		{
			return Requester.Request<EmptyResponse>(
				Method.PUT,
				$"/object/mail/{Ctx.UserId}/bulk",
				updates
			);
		}

		/// <summary>
		/// Accept all the attachments from a set of mail messages.
		/// </summary>
		/// <param name="manyRequest">Request structure containing numeric message IDs.</param>
		public Promise<EmptyResponse> AcceptMany(MailAcceptManyRequest manyRequest)
		{
			return Requester.Request<EmptyResponse>(
				Method.PUT,
				$"/object/mail/{Ctx.UserId}/accept/many",
				manyRequest
			);
		}

		public abstract Promise<MailQueryResponse> GetCurrent(string scope = "");
	}

	[Serializable]
	public class MailQueryResponse
	{
		public int unreadCount;
	}

	[Serializable]
	public class ListMailResponse
	{
		public List<MailMessage> result;
	}

	[Serializable]
	public class SearchMailRequest
	{
		public SearchMailRequestClause[] clauses;

		public SearchMailRequest(params SearchMailRequestClause[] clauses)
		{
			this.clauses = clauses;
		}

		public ArrayDict Serialize()
		{
			var serializedClauses = new ArrayDict[clauses.Length];
			for (var i = 0; i < serializedClauses.Length; i++)
			{
				serializedClauses[i] = clauses[i].Serialize();
			}

			return new ArrayDict { { nameof(clauses), serializedClauses } };
		}
	}

	[Serializable]
	public class SearchMailRequestClause
	{
		public string name;
		public bool onlyCount;
		public string[] categories;
		public string[] states;
		public long? forSender;
		public long? limit;
		public long? start;

		public ArrayDict Serialize()
		{
			var dict = new ArrayDict();

			dict.Add(nameof(name), name);
			dict.Add(nameof(onlyCount), onlyCount);

			if (categories != null)
			{
				dict.Add(nameof(categories), categories);
			}

			if (states != null)
			{
				dict.Add(nameof(states), states);
			}

			if (limit.HasValue)
			{
				dict.Add(nameof(limit), limit.Value);
			}

			if (forSender.HasValue)
			{
				dict.Add(nameof(forSender), forSender.Value);
			}

			if (start.HasValue)
			{
				dict.Add(nameof(start), start.Value);
			}

			return dict;
		}
	}

	[Serializable]
	public class SearchMailResponse
	{
		public List<SearchMailResponseClause> results;
	}

	[Serializable]
	public class SearchMailResponseClause
	{
		public int count;
		public string name;
		public List<MailMessage> content;
	}

	[Serializable]
	public class MailMessage
	{
		public long id;
		public long sent;
		public long claimedTimeMs;
		public long receiverGamerTag;
		public long senderGamerTag;
		public string category;
		public string subject;
		public string body;
		public string state;
		public string expires;
		public MailRewards rewards;

		public MailState MailState => (MailState)Enum.Parse(typeof(MailState), state);
	}

	[Serializable]
	public class MailCounts
	{
		public long sent;
		public MailStateCounts received;
	}

	[Serializable]
	public class MailStateCounts
	{
		public long all;
		public long unread;
		public long read;
		public long deleted;
	}

	[Serializable]
	public class MailGetCountsResponse
	{
		public MailCounts total;
	}

	[Serializable]
	public class MailSendRequest
	{
		public List<MailSendEntry> sendMailRequests = new List<MailSendEntry>();

		public MailSendRequest Add(MailSendEntry entry)
		{
			sendMailRequests.Add(entry);
			return this;
		}
	}

	[Serializable]
	public class MailSendEntry
	{
		public long senderGamerTag;
		public long receiverGamerTag;
		public string category;
		public string subject;
		public string body;
		public string expires;
		public MailRewards rewards;
	}

	[Serializable]
	public class MailRewards
	{
		public List<CurrencyChange> currencies;
		public List<ItemCreateRequest> items;
		public bool applyVipBonus = true;
	}

	[Serializable]
	public class MailUpdate
	{
		public long mailId;
		public string state;
		public string expires;
		public bool acceptAttachments;

		public MailUpdate(long mailId, MailState state, bool acceptAttachments, string expires)
		{
			this.mailId = mailId;
			this.state = state.ToString();
			this.acceptAttachments = acceptAttachments;
			this.expires = expires;
		}
	}

	[Serializable]
	public class MailUpdateEntry
	{
		public long id;
		public MailUpdate update;
	}

	[Serializable]
	public class MailUpdateRequest
	{
		public List<MailUpdateEntry> updateMailRequests = new List<MailUpdateEntry>();

		public MailUpdateRequest Add(long id, MailState state, bool acceptAttachments, string expires)
		{
			var entry = new MailUpdateEntry { id = id, update = new MailUpdate(id, state, acceptAttachments, expires) };
			updateMailRequests.Add(entry);
			return this;
		}
	}

	[Serializable]
	public class MailReceivedRequest
	{
		public string[] categories;
		public string[] states;
		public long limit;
	}

	[Serializable]
	public class MailCountRequest
	{
		public string[] categories;
	}

	[Serializable]
	public class MailAcceptManyRequest
	{
		public long[] mailIds;
	}

	public enum MailState
	{
		Read,
		Unread,
		Claimed,
		Deleted
	}
}
