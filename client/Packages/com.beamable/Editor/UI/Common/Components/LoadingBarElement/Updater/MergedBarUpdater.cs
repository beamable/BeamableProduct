using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Components {
    public class MergedBarUpdater : LoadingBarUpdater {

        private readonly List<DummyLoadingBar> _loadingBars = new List<DummyLoadingBar>();
        private float _totalWeight = 0f;
        private readonly bool _singleSteps;
        public override string ProcessName { get; }

        public MergedBarUpdater(ILoadingBar loadingBar, string processName, bool singleSteps = false) : base(loadingBar) {
            _singleSteps = singleSteps;
            ProcessName = processName;
        }
        
        public ILoadingBar CreateDummyLoadingBar(float weight = 1f) {
            weight = Mathf.Max(weight, 0f);
            var loadingBar = new DummyLoadingBar(this, weight);
            _loadingBars.Add(loadingBar);
            _totalWeight += weight;
            return loadingBar;
        }
        
        protected override void OnKill() {
            foreach (var loadingBar in _loadingBars) {
                loadingBar.Updater?.Kill();
            }
        }

        private void OnUpdate() {
            float actualProgress; 
            if (_singleSteps) {
                Step = _loadingBars.Count(lb => lb.Updater.Succeeded);
                TotalSteps = _loadingBars.Count;
                actualProgress = Step / (float) TotalSteps;
            }else {
                actualProgress = GetActualProgress();
                Step = _loadingBars.Sum(lb => lb.Updater.Step);
                TotalSteps = _loadingBars.Sum(lb => lb.Updater.TotalSteps);
            }
            Succeeded = _loadingBars.All(lb => lb.Updater.Succeeded);
            var errors = _loadingBars.Count(lb => lb.Updater.GotError);
            GotError = errors > 0;

            if (Succeeded) {
                _loadingBar.UpdateProgress(1f, $"(Success: {ProcessName})");
            }
            else {
                string errorMessage = "";
                if (GotError) {
                    errorMessage = $" Errors: {errors}";
                    actualProgress = 0f;
                }
                _loadingBar.UpdateProgress(actualProgress, $"({ProcessName} {StepText}{errorMessage})", GotError);
            }

            EditorApplication.delayCall += () => {
                if (GotError || _loadingBars.All(lb => lb.Updater.Killed)) {
                    Kill();
                }
            };
        }

        private float GetActualProgress() {
            if (_totalWeight <= 0f || _loadingBars.Count == 0) {
                return 0f;
            }

            return _loadingBars.Sum(lb => lb.Progress * lb.Weight) / _totalWeight;
        }

        private class DummyLoadingBar : ILoadingBar {
            public float Progress { get; set; }
            public string Message { get; set; }
            public bool Failed { get; set; }
            public float Weight { get; }
            public LoadingBarUpdater Updater { get; private set; }
            public MergedBarUpdater Parent { get; }

            public DummyLoadingBar(MergedBarUpdater parent, float weight) {
                Weight = weight;
                Parent = parent;
            }
            
            public void UpdateProgress(float progress, string message = null, bool failed = false) {
                Progress = progress;
                Message = message;
                Failed = failed;
                Parent.OnUpdate();
            }

            public void SetUpdater(LoadingBarUpdater updater) {
                if (updater == Updater) return;
                Updater?.Kill();
                Updater = updater;
                Parent.OnUpdate();
            }
        }
    }
}