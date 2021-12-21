using System;
using SFML.System;

namespace RayTracingGPU.Primitives
{
    [Serializable]
    public class Material
    {
        public Vector3f Emmitance { get; }
        public Vector3f Rerlectance { get; }
        public float Opacity { get; }
        public float Roughness { get; }
    }
}