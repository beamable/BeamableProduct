using Beamable.Serialization.SmallerJSON;

namespace Beamable.Common.Semantics
{
	public interface IBeamSemanticType : IRawJsonProvider
	{
		string SemanticName { get; }
	}
	
    public interface IBeamSemanticType<T> : IBeamSemanticType
    {
    }
}
