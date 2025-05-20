using Beamable.Common;

namespace Beamable.Server;

/// <summary>
/// this class is not meant to be used. It's sole purpose is to stand in
/// when something in the outer class needs to access a method with nameof() 
/// </summary>
class DummyThirdParty : IFederationId
{
	public string UniqueName => "__temp__";
}
