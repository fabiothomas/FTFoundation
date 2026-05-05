#nullable enable
using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    public class PrefsSaveable<T> : Saveable<T>
    {
        public PrefsSaveable(string id, T defaultValue)
            : base(id, defaultValue)
        {
            if (PlayerPrefs.HasKey(Id))
            {
                IsDirty = true;
                Restore();
            }
        }

        public override void Save()
        {
            if (Value is int iValue) PlayerPrefs.SetInt(Id, iValue);
            else if (Value is float fValue) PlayerPrefs.SetFloat(Id, fValue);
            else if (Value is string sValue) PlayerPrefs.SetString(Id, sValue);
            else if (Value is bool bValue) PlayerPrefs.SetInt(Id, bValue ? 1 : 0);
            IsDirty = false;
        }

        public override void Restore()
        {
            if (!IsDirty) return;
            if (Value is int) Value = (T)(object)PlayerPrefs.GetInt(Id);
            else if (Value is float) Value = (T)(object)PlayerPrefs.GetFloat(Id);
            else if (Value is string) Value = (T)(object)PlayerPrefs.GetString(Id);
            else if (Value is bool) Value = (T)(object)(PlayerPrefs.GetInt(Id) == 1);
            IsDirty = false;
            InvokeBindings();
        }
    }
}