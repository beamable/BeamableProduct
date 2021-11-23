using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Runtime.BeamHints
{
	public interface IBeamHintProvider
	{
		void SetStorage(params IBeamHintStorage[] storages);
	}

	public interface IBeamHintManager
	{
		void SetStorage(params IBeamHintStorage[] storages);
	}

	/// <summary>
	/// Defines a storage for <see cref="BeamHint"/>s. It is a query-able in-memory database of <see cref="BeamHint"/>s.
	/// Other <see cref="IBeamHintProvider"/> systems add hints to these and <see cref="IBeamHintManager"/> read, filter, clear and pass these along to the UI. 
	/// </summary>
	public interface IBeamHintStorage : IEnumerable<BeamHint>
	{
		/// <summary>
		/// The <see cref="BeamHintType"/>s that this storage will accept.
		/// </summary>
		BeamHintType AcceptingTypes
		{
			get;
		}

		/// <summary>
		/// The '|' separated list of accepting domains.
		/// <see cref="BeamHintDomains.ANY"/>, is returned if it can accept any domain.
		/// <see cref="BeamHintDomains.ANY_BEAMABLE"/>, is returned if it can accept any beamable-owned domain.
		/// <see cref="BeamHintDomains.ANY_USER_DEFINED"/>, is returned if it can accept any user-owned domain.
		/// </summary>
		string AcceptingDomains
		{
			get;
		}

		/// <summary>
		/// Adds a hint to the storage. Verifies that the given types are accepted by this storage.
		/// </summary>
		/// <param name="type">The type of hint that it is.</param>
		/// <param name="originSystem">The system that originated this hint.</param>
		/// <param name="hintDomain">An arbitrary contextual grouping for the hint.</param>
		/// <param name="uniqueId">An id, unique when combined with <paramref name="hintDomain"/>, that identifies the hint.</param>
		/// <param name="hintContextObj">Any arbitrary data that you wish to tie to the hint.</param>
		void AddOrReplaceHint(BeamHintType type, string hintDomain, string uniqueId, object hintContextObj = null);

		/// <summary>
		/// Adds a hint to the storage. Verifies that the given types are accepted by this storage.
		/// </summary>
		/// <param name="header">A pre-built <see cref="BeamHintHeader"/> to add.</param>
		/// <param name="hintContextObj">Any arbitrary data that you wish to tie to the hint.</param>
		void AddOrReplaceHint(BeamHintHeader header, object hintContextObj = null);

		/// <summary>
		/// Takes in two parallel <see cref="IEnumerable{T}"/> (same-length arrays) of <see cref="BeamHintHeader"/>/<see cref="object"/> pairs and add them to the storage.
		/// Validates each header individually against the <see cref="AcceptingTypes"/> and other Accepting filters.
		/// </summary>
		void AddOrReplaceHints(IEnumerable<BeamHintHeader> headers, IEnumerable<object> hintContextObjs);

		/// <summary>
		/// Adds the given <see cref="BeamHint"/>s.
		/// Validates each header individually against the <see cref="AcceptingTypes"/> and other Accepting filters.
		/// </summary>
		void AddOrReplaceHints(IEnumerable<BeamHint> bakedHints);

		void BatchAddOrReplaceHints(BeamHintType type, string domain, IEnumerable<BeamHintHeader> headers, IEnumerable<object> hintContextObjs);
		void BatchAddOrReplaceHints(BeamHintType type, string domain, IEnumerable<BeamHint> bakedHints);

		/// <summary>
		/// Removes the <see cref="BeamHint"/> identified by the <paramref name="header"/>.
		/// </summary>
		void RemoveHint(BeamHintHeader header);

		/// <summary>
		/// Removes the given <paramref name="hint"/> from the storage.
		/// </summary>
		void RemoveHint(BeamHint hint);

		/// <summary>
		/// Removes the <see cref="BeamHint"/>s identified by the given <paramref name="headers"/> from the storage.
		/// </summary>
		void RemoveHints(IEnumerable<BeamHintHeader> headers);

		/// <summary>
		/// Removes the given <paramref name="hints"/> from the storage.
		/// </summary>
		void RemoveHints(IEnumerable<BeamHint> hints);

		/// <summary>
		/// Removes all hints that <see cref="Regex.Match(string)"/> of any of the given <paramref name="hintDomains"/> and <paramref name="hintIds"/>.
		/// </summary>
		int RemoveAllHints(IEnumerable<string> hintDomains, IEnumerable<string> hintIds);

		/// <summary>
		/// Remove all hints of the given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The <see cref="BeamHintType"/>s to remove.</param>
		/// <returns>The amount of <see cref="BeamHint"/>s removed.</returns>
		int RemoveAllHints(BeamHintType type);

		/// <summary>
		/// Removes all hints that <see cref="Regex.Match(string)"/> of the given <paramref name="hintDomainRegex"/> and <paramref name="idRegex"/>.
		/// </summary>
		int RemoveAllHints(string hintDomainRegex = ".*", string idRegex = ".*");
	}

	/// <summary>
	/// Defines a storage for <see cref="BeamHint"/>s. This implementation is header-based, meaning:
	/// <para/>
	/// - It contains a <see cref="HashSet{T}"/> of unique headers for each added hint. Hints are considered existing if they are present in the <see cref="_headers"/> set.
	/// <para/>
	/// - It contains a <see cref="Dictionary{TKey,TValue}"/>, indexed by <see cref="BeamHintHeader"/>, holding each hint's context object. The key exists even if the object is null.
	/// <para/>
	/// - Queries are made over the <see cref="HashSet{T}"/> of headers and returned in <see cref="BeamHint"/> format.
	/// <para/>
	/// On creation, this storage can take a <see cref="BeamHintType"/>, and <see cref="IReadOnlyList{T}"/> of <see cref="string"/>s representing <see cref="BeamHintDomains"/>.
	/// These are used to assert that this <see cref="BeamHintStorage"/> instance only has the correct type of <see cref="BeamHint"/>s added to it.
	/// This allows us to sub-divide the querying area as the system evolves and we bump into performance issues.
	/// </summary>
	public class BeamHintStorage : IBeamHintStorage
	{
		public BeamHintType AcceptingTypes => _acceptingTypes;

		public string AcceptingDomains
		{
			get
			{
				var str = _acceptingDomainsRegex.ToString();
				switch (str)
				{
					case ".*":
						return BeamHintDomains.ANY;
					case "USER_.*":
						return BeamHintDomains.ANY_USER_DEFINED;
					case "BEAM_.*":
						return BeamHintDomains.ANY_BEAMABLE;
					default:
						return str;
				}
			}
		}

		private readonly BeamHintType _acceptingTypes;
		private readonly Regex _acceptingDomainsRegex;

		private HashSet<BeamHintHeader> _headers;
		private Dictionary<BeamHintHeader, object> _hintContextObjects;

		/// <summary>
		/// The given <see cref="BeamHintType"/>, <see cref="BeamHintOriginSystem"/> and <see cref="IReadOnlyList{T}"/> of <see cref="string"/>s are used to assert that this
		/// <see cref="BeamHintStorage"/> instance only has the correct type of <see cref="BeamHint"/>s added to it. This allows us to sub-divide the querying area as the system evolves and we bump into
		/// performance issues.
		/// </summary>
		/// <param name="acceptingTypes">Bitfield of types this storage will accept.</param>
		/// <param name="acceptingDomains">List of accepting domains this storage will accept.</param>
		public BeamHintStorage(BeamHintType acceptingTypes, IEnumerable<string> acceptingDomains)
		{
			_acceptingTypes = acceptingTypes;

			var regexPattern = string.Join("|", acceptingDomains.Select(acceptingDomain =>
			{
				if (acceptingDomain.Equals(BeamHintDomains.ANY)) return ".*";
				if (acceptingDomain.Equals(BeamHintDomains.ANY_USER_DEFINED)) return "USER_.*";
				if (acceptingDomain.Equals(BeamHintDomains.ANY_BEAMABLE)) return "BEAM_.*";
				return acceptingDomain;
			}));
			_acceptingDomainsRegex = new Regex(regexPattern);

			_headers = new HashSet<BeamHintHeader>();
			_hintContextObjects = new Dictionary<BeamHintHeader, object>();
		}

		/// <summary>
		/// Checks a <see cref="BeamHintHeader"/> against the configured accepting <see cref="_acceptingTypes"/>, <see cref="_acceptingSystems"/> and <see cref="_acceptingDomainsRegex"/>. 
		/// </summary>
		private bool CheckAcceptsHint(BeamHintHeader header)
		{
			var matchType = (header.Type & _acceptingTypes) != 0;
			var matchDomain = _acceptingDomainsRegex.IsMatch(header.Domain);

			return matchType && matchDomain;
		}

		#region IEnumerable Implementation

		/// <summary>
		/// Allows iteration over storage with for-each and use of LINQ methods directly over BeamHints.
		/// </summary>
		public IEnumerator<BeamHint> GetEnumerator()
		{
			return _headers.Select(h => new BeamHint(h, _hintContextObjects[h])).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public void AddOrReplaceHint(BeamHintType type, string hintDomain, string uniqueId, object hintContextObj = null)
		{
			var header = new BeamHintHeader(type, hintDomain, uniqueId);
			AddOrReplaceHint(header, hintContextObj);
		}

		public void AddOrReplaceHint(BeamHintHeader header, object hintContextObj = null)
		{
			// Guard to handle guarantee that hints are being added to the valid storages.
			System.Diagnostics.Debug.Assert(CheckAcceptsHint(header),
			                                $"This storage does not accept the given header.\nHeader = {header}\nAccepting Types={_acceptingTypes}\nAccepting Domains{_acceptingDomainsRegex}");

			if (!_headers.Contains(header))
				_headers.Add(header);

			if (_hintContextObjects.ContainsKey(header))
				_hintContextObjects[header] = hintContextObj;
			else
				_hintContextObjects.Add(header, hintContextObj);
		}

		public void AddOrReplaceHints(IEnumerable<BeamHintHeader> headers, IEnumerable<object> hintContextObjs)
		{
			var contextObjs = hintContextObjs.ToList();
			var beamHint_headers = headers.ToList();

			System.Diagnostics.Debug.Assert(beamHint_headers.Count == contextObjs.Count, "These must be parallel arrays of the same length.");

			var zipped = beamHint_headers.Zip(contextObjs, (header, obj) => new {Header = header, ContextObject = obj});
			foreach (var hint in zipped)
			{
				AddOrReplaceHint(hint.Header, hint.ContextObject);
			}
		}

		public void AddOrReplaceHints(IEnumerable<BeamHint> bakedHints)
		{
			foreach (var hint in bakedHints)
			{
				AddOrReplaceHint(hint.Header, hint.ContextObject);
			}
		}

		public void RemoveHint(BeamHintHeader header)
		{
			RemoveHints(new[] {header});
		}

		public void RemoveHint(BeamHint hint)
		{
			RemoveHints(new[] {hint});
		}

		public void RemoveHints(IEnumerable<BeamHintHeader> headers)
		{
			foreach (var toRemove in headers)
			{
				_headers.Remove(toRemove);
			}
		}

		public void RemoveHints(IEnumerable<BeamHint> hints)
		{
			RemoveHints(hints.Select(h => h.Header));
		}

		public int RemoveAllHints(IEnumerable<string> hintDomains, IEnumerable<string> hintIds)
		{
			var hintDomainRegexStr = string.Join("|", hintDomains);
			var hintIdRegexStr = string.Join("|", hintIds);

			return RemoveAllHints(hintDomainRegexStr, hintIdRegexStr);
		}

		public int RemoveAllHints(BeamHintType type)
		{
			var removedCount = _headers.RemoveWhere((header => (header.Type & type) != 0));
			return removedCount;
		}

		public int RemoveAllHints(string hintDomainRegex = ".*", string idRegex = ".*")
		{
			if (hintDomainRegex == ".*" && idRegex == ".*")
			{
				_headers.Clear();
			}

			var domainReg = new Regex(hintDomainRegex);
			var idReg = new Regex(idRegex);

			var removedCount = _headers.RemoveWhere(header => (domainReg.Match(header.Domain).Success && idReg.Match(header.Id).Success));
			return removedCount;
		}
	}
}
