using System;

namespace Beamable.Modules.Generics
{
    public abstract class Model
    {
        public Action OnInitialized;
        public Action OnChange;
        
        public abstract void Initialize(params object[] initParams);
    }
}