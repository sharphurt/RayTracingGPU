using System;
using SFML.System;

namespace RayTracingGPU
{
    [Serializable]
    public struct Matrix3
    {
        public Vector3f Row1 { get; }
        public Vector3f Row2 { get; }
        public Vector3f Row3 { get; }
    }
}