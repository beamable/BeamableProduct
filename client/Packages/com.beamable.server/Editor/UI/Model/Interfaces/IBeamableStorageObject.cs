using Beamable.Server.Editor;

namespace Beamable.Editor.UI.Model
{
    public interface IBeamableStorageObject : IBeamableService
    {
        StorageObjectDescriptor Descriptor { get; }
        MongoStorageBuilder Builder { get; }
    }
}