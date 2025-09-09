using System.Collections.Generic;
using Beamable.Server.Editor;
using beamable.tooling.common.Microservice;

namespace Beamable.Server;

public class FederationMetadata
{
    public List<FederationComponent> Components { get; set; }
}