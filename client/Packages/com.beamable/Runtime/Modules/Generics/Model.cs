using System;

namespace Beamable.Modules.Generics
{
    public abstract class Model
    {
        public Action OnRefreshRequested;
        public Action OnRefresh;
        
        public abstract void Initialize(params object[] initParams);
    }
}