namespace Beamable.Editor.UI.Components {
    public interface ILoadingBar {
        float Progress { get; set; }
        string Message { get; set; }
        bool Failed { get; set; }
        void UpdateProgress(float progress, string message = null, bool failed = false);
        void SetUpdater(LoadingBarUpdater updater);
    }
}