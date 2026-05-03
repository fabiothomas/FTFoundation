using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    public struct Position
    {
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        public static Position Default => new()
        {
            position = Vector3.zero,
            rotation = Quaternion.identity,
            scale = Vector3.one,
        };

        public static Position Get(Vector3 position)
        {
            return new()
            {
                position = position,
                rotation = Quaternion.identity,
                scale = Vector3.one,
            };
        }

        public static Position Get(Vector3 position, Vector3 scale)
        {
            return new()
            {
                position = position,
                rotation = Quaternion.identity,
                scale = scale,
            };
        }

        public static Position Get(Vector3 position, Quaternion rotation)
        {
            return new()
            {
                position = position,
                rotation = rotation,
                scale = Vector3.one,
            };
        }

        public static Position Get(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return new()
            {
                position = position,
                rotation = rotation,
                scale = scale,
            };
        }

        public readonly void SetTransform(Transform transform)
        {
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }
    }
}