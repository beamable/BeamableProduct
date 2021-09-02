using Beamable.Common;

namespace Beamable.Editor.UI.Components {
    public abstract class LoadingBarUpdater {
        protected ILoadingBar _loadingBar;
        public int Step { get; protected set; }
        public int TotalSteps { get; protected set; }
        public bool Killed { get; private set; }
        public bool GotError { get; protected set; }
        public bool Succeeded { get; protected set; }

        public virtual string StepText => $"{Step}/{TotalSteps}";
        public abstract string ProcessName { get; }

        public LoadingBarUpdater(ILoadingBar loadingBar) {
            _loadingBar = loadingBar;
            _loadingBar.SetUpdater(this);
        }
        
        public void Kill() {
            if (Killed) return;
            Killed = true;
            OnKill();
        }

        protected abstract void OnKill();
    }
}