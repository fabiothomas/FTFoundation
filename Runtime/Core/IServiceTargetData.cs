using System;

namespace Scripts.Foundation
{
    public interface IServiceTargetData
    {
        public string Name { get; }
        public ServiceTargetDataType DataType { get; }
        public Type ObjectType { get; }
        public object Reference { get; }
    }

    public enum ServiceTargetDataType
    {
        NONE = 0,
        MONOBEHAVIOUR = 1,
        SYSTEM = 2,
        SINGLETON = 3,
        SCOPED = 4
    }
}