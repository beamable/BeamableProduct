using System;
using System.Globalization;
using Beamable.Common;
using Beamable.Server;

namespace Beamable.SampleService
{
	/// <summary>
	/// A small, self-contained sample microservice that demonstrates the three
	/// things almost every Beamable service does:
	///
	///   1. <see cref="Add"/>     — plain server-side compute (no auth, no I/O).
	///   2. <see cref="WhoAmI"/>  — reading the authenticated caller from <c>Context</c>.
	///   3. <see cref="Visit"/>   — server-authoritative state via the Stats service.
	///
	/// Every method marked <c>[ClientCallable]</c> is reachable from a game client
	/// (here, the React Native app via the Web SDK). See the project README for how
	/// the client invokes these endpoints.
	/// </summary>
	public partial class SampleService : Microservice
	{
		/// <summary>The protected stat we use as a per-player visit counter.</summary>
		private const string VisitsStat = "sample.visits";

		/// <summary>Adds two numbers on the server. The "hello world" of a ClientCallable.</summary>
		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}

		/// <summary>Echoes a friendly, server-built greeting. Shows string args + return values.</summary>
		[ClientCallable]
		public string Greet(string name)
		{
			var who = string.IsNullOrWhiteSpace(name) ? "stranger" : name.Trim();
			return $"Hello, {who}! This greeting came from the SampleService microservice.";
		}

		/// <summary>
		/// Returns the identity of the caller, read from the request <c>Context</c>.
		/// This is the trustworthy, server-verified player id — never trust a player
		/// id sent up from the client.
		/// </summary>
		[ClientCallable]
		public WhoAmIResult WhoAmI()
		{
			return new WhoAmIResult
			{
				userId = Context.UserId,
				cid = Context.Cid,
				pid = Context.Pid,
				isAdmin = Context.IsAdmin,
			};
		}

		/// <summary>
		/// Increments a server-authoritative visit counter for the calling player and
		/// returns the new total. We store it as a *protected* stat: only the server can
		/// write it (so the count can't be forged), while the client can still read it.
		/// </summary>
		[ClientCallable]
		public async Promise<VisitResult> Visit()
		{
			var userId = Context.UserId;

			var current = await Services.Stats.GetProtectedPlayerStat(userId, VisitsStat);
			var count = ParseCount(current) + 1;

			await Services.Stats.SetProtectedPlayerStat(userId, VisitsStat, count.ToString(CultureInfo.InvariantCulture));

			return new VisitResult { userId = userId, visits = count };
		}

		private static int ParseCount(string raw)
		{
			return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
		}
	}

	/// <summary>Caller identity returned by <see cref="SampleService.WhoAmI"/>.</summary>
	[Serializable]
	public class WhoAmIResult
	{
		public long userId;
		public string cid;
		public string pid;
		public bool isAdmin;
	}

	/// <summary>The updated visit count returned by <see cref="SampleService.Visit"/>.</summary>
	[Serializable]
	public class VisitResult
	{
		public long userId;
		public int visits;
	}
}
