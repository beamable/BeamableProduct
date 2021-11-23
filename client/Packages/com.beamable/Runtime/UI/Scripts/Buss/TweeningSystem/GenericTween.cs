using System;
using UnityEngine;

namespace Beamable.UI.Tweening {
    public abstract class GenericTween<T> : BaseTween {
        private readonly T _startValue;
        private readonly T _endValue;
        private readonly Action<T> _updateAction;
        public event Action CompleteEvent;

        public GenericTween(float duration, T startValue, T endValue, Action<T> updateAction) : base(duration) {
            _startValue = startValue;
            _endValue = endValue;
            _updateAction = updateAction;
        }
        
        protected override bool Update(float t) {
            _updateAction(Lerp(_startValue, _endValue, t));
            return true;
        }

        protected abstract T Lerp(T from, T to, float t);

        protected override void OnComplete() {
            CompleteEvent?.Invoke();
        }
    }
}