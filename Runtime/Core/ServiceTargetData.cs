using System;
using System.Diagnostics.CodeAnalysis;

namespace Scripts.Foundation
{
    internal record ServiceTargetData : IServiceTargetData
    {
        public string Name { get; }
        public ServiceTargetDataType DataType { get; }
        public Type ObjectType { get; }
        public object Reference { get; }

        public ServiceTargetData(string name, ServiceTargetDataType dataType, Type objectType, object reference)
        {
            Name = name;
            DataType = dataType;
            ObjectType = objectType;
            Reference = reference;
        }

        public bool IsUnknown()
        {
            return DataType == ServiceTargetDataType.NONE;
        }

        public static ServiceTargetData EmptyServiceTargetData()
        {
            return new("Unknown Target", ServiceTargetDataType.NONE, null, null);
        }

        public bool TryGetReference<T>([NotNullWhen(true)] out T reference)
        {
            try
            {
                reference = (T)Reference;
                return true;
            }
            catch
            {
                reference = default;
                return false;
            }
        }

        public void UseReference<T>(Action<T> func, Action callback = null)
        {
            try
            {
                func((T)Reference);
            }
            catch
            {
                callback();
            }
        }
    }
}