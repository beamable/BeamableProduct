using Beamable.Server.Editor;

namespace Beamable.Editor.UI.Model
{
    public class MongoStorageBuilder : ServiceBuilderBase
    {
        public void ForwardEventsTo(MongoStorageBuilder oldBuilder)
        {
            if (oldBuilder == null) return;
            OnIsRunningChanged += oldBuilder.OnIsRunningChanged;
        }

    }
}