using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common.Runtime.BeamHints
{
	/// <summary>
	/// Interface for the Global Storage --- only exists to enable mocking for automated testing purposes so it'll acknowledge implementation details of the
	/// <see cref="BeamHintEditorStorage"/> which is our implementation of this interface.
	/// <para/>
	/// Internally, we have one <see cref="IBeamHintStorage"/> for each domain we generate with <see cref="BeamHintDomains.GenerateBeamableDomain"/>.
	/// As the number of generated hints a domain can produce grows, we split these macro-storages into one for each sub-domain generated with
	/// <see cref="BeamHintDomains.GenerateSubDomain"/>. This approach allows us to ensure we can move our internal data around to avoid slow-editor performance.
	/// </summary>
	public interface IBeamHintGlobalStorage : IBeamHintStorage
	{
		/// <summary>
		/// The combined hints of all internal <see cref="IBeamHintStorage"/>s.
		/// </summary>
		IEnumerable<BeamHint> All
		{
			get;
		}

		/// <summary>
		/// The list of all user defined storages.
		/// Our Beamable Assistant UI continuously detects hints added to these storages automatically --- so add your declared <see cref="IBeamHintStorage"/>s here.
		/// We recommend you use our <see cref="BeamHintStorage"/> implementation, but you are free to provide your own and, as long as the semantics remain the same, it
		/// should just work with our UI. 
		/// </summary>
		List<IBeamHintStorage> UserDefinedStorages
		{
			get;
		}

		/// <summary>
		/// The list of all Beamable-defined storages.
		/// </summary>
		IReadOnlyList<IBeamHintStorage> BeamableStorages
		{
			get;
		}

		#region Per-Domain Beamable Storages

		/// <summary>
		/// Contains the <see cref="BeamHint"/>s for the entire <see cref="BeamHintDomains.BEAM_CSHARP_MICROSERVICES"/> domain.
		/// </summary>
		IBeamHintStorage CSharpMSHints
		{
			get;
		}

		/// <summary>
		/// Contains the <see cref="BeamHint"/>s for the entire <see cref="BeamHintDomains.BEAM_CONTENT"/> domain.
		/// </summary>
		IBeamHintStorage ContentHints
		{
			get;
		}

		#endregion
	}

	public class BeamHintEditorStorage : IBeamHintGlobalStorage
	{
		public BeamHintType AcceptingTypes => BeamHintType.All;
		public string AcceptingDomains => BeamHintDomains.ANY;

		public List<IBeamHintStorage> UserDefinedStorages
		{
			get;
		}
		public IReadOnlyList<IBeamHintStorage> BeamableStorages
		{
			get;
		}
		public IEnumerable<BeamHint> All => BeamableStorages.Union(UserDefinedStorages).SelectMany(storage => storage);

		#region Per-Domain Beamable Storages

		public IBeamHintStorage CSharpMSHints => _CSharpMSStorage;
		private BeamHintStorage _CSharpMSStorage = new BeamHintStorage(BeamHintType.All, new[] {BeamHintDomains.BEAM_CSHARP_MICROSERVICES});

		public IBeamHintStorage ContentHints => _contentStorage;
		private BeamHintStorage _contentStorage = new BeamHintStorage(BeamHintType.All, new[] {BeamHintDomains.BEAM_CSHARP_MICROSERVICES});

		#endregion

		public BeamHintEditorStorage()
		{
			UserDefinedStorages = new List<IBeamHintStorage>();
			BeamableStorages = new List<IBeamHintStorage>(32); // This number is large enough to fit all our systems.
		}

		#region IEnumerable Implementation

		public IEnumerator<BeamHint> GetEnumerator()
		{
			return All.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
		
		
		public void AddOrReplaceHint(BeamHintType type, string hintDomain, string uniqueId, object hintContextObj = null)
		{
			throw new System.NotImplementedException();
		}

		public void AddOrReplaceHint(BeamHintHeader header, object hintContextObj = null)
		{
			var type = header.Type;
			var domain = header.Domain;
			
			if(BeamHintDomains.IsBeamableDomain(domain))
				// look through our storage
				
			if(BeamHintDomains.IsUserDomain(domain))
				// look through user defined storages
				
			


		}

		public void AddOrReplaceHints(IEnumerable<BeamHintHeader> headers, IEnumerable<object> hintContextObjs)
		{
			throw new System.NotImplementedException();
		}

		public void AddOrReplaceHints(IEnumerable<BeamHint> bakedHints)
		{
			throw new System.NotImplementedException();
		}
		
	}
}
