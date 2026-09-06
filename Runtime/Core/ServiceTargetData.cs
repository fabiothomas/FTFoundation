using System;
using System.Diagnostics.CodeAnalysis;

namespace FTFoundation.Core
{
    internal record ServiceTargetData : IServiceTargetData
    {
        public string Name { get; }
        public ServiceTargetDataType DataType { get; }
        public Type? ObjectType { get; }
        public object? Reference { get; }

        public ServiceTargetData(string name, ServiceTargetDataType dataType, Type? objectType, object? reference)
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

        public static ServiceTargetData FoundationServiceTargetData()
        {
            return new("Foundation", ServiceTargetDataType.FT_FOUNDATION, null, null);
        }

        public bool TryGetReference<T>([NotNullWhen(true)] out T? reference)
        {
            if (Reference == null || ObjectType == null || !typeof(T).IsAssignableFrom(ObjectType))
            {
                reference = default;
                return false;
            }
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

        public void UseReference<T>(Action<T> func, Action? callback = null)
        {
            if (Reference == null || ObjectType == null || !typeof(T).IsAssignableFrom(ObjectType))
            {
                callback?.Invoke();
                return;
            }
            try
            {
                func((T)Reference);
            }
            catch
            {
                callback?.Invoke();
            }
        }
    }
}