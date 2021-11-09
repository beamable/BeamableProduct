using System;
using UnityEngine;

namespace Beamable.UI.SDF.Styles {
    [Serializable]
    public class SerializableValueObject : ISerializationCallbackReceiver {
        private object value;
        [SerializeField]
        private string type;
        [SerializeField]
        private string json;

        public void Set(object value) {
            this.value = value;
        }

        public object Get() {
            return value;
        }

        public T Get<T>() {
            return (T) value;
        }

        public void OnBeforeSerialize() {
            if (value == null) {
                type = json = string.Empty;
            }
            else {
                type = value.GetType().AssemblyQualifiedName;
                json = JsonUtility.ToJson(value);
            }
        }

        public void OnAfterDeserialize() {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(json)) {
                value = null;
            }
            var sysType = Type.GetType(type);
            try {
                value = JsonUtility.FromJson(json, sysType);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}