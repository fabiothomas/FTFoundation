using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    public class FileSaveable<T> : Saveable<T>
    {
        private readonly IFileSaveService _saveService;

        private FileSaveable(string id, T defaultValue, IFileSaveService saveService)
            : base(id, defaultValue)
        {
            _saveService = saveService;
        }

        public static FileSaveable<T> Create(string id, T defaultValue, IFileSaveService saveService)
        {
            // check if type is serializable
            if (!typeof(T).IsSerializable)
                throw new System.InvalidOperationException($"Type {typeof(T)} is not serializable and cannot be used with FileSaveable.");

            return new FileSaveable<T>(id, defaultValue, saveService);
        }

        public override void Save()
        {
            _saveService.Set(Id, Serialize(Value));
            IsDirty = false;
        }

        public override void Restore()
        {
            string serialized = _saveService.Get(Id);
            if (serialized == null) return;
            Value = Deserialize(serialized);
            IsDirty = false;
            InvokeBindings();
        }

        private static string Serialize(T value)
        {
            if (value is string s) return s;
            if (value is int i) return i.ToString();
            if (value is float f) return f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (value is bool b) return b ? "1" : "0";
            return JsonUtility.ToJson(value);
        }

        private static T Deserialize(string raw)
        {
            object result;
            if (typeof(T) == typeof(string)) result = raw;
            else if (typeof(T) == typeof(int)) result = int.Parse(raw);
            else if (typeof(T) == typeof(float)) result = float.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            else if (typeof(T) == typeof(bool)) result = raw == "1";
            else result = JsonUtility.FromJson<T>(raw);
            return (T)result;
        }
    }
}