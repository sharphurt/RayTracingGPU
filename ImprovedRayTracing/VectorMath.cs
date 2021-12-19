using System;
using SFML.System;

namespace RayTracingGPU
{
    public static class VectorMath
    {
        public static Vector3f Normalize(Vector3f vector)
        {
            var mag = Magnitude(vector);
            return vector / mag;
        }

        public static float Magnitude(Vector3f vector)
        {
            var square = new Vector3f(vector.X * vector.X, vector.Y * vector.Y, vector.Z * vector.Z);
            return MathF.Sqrt(square.X + square.Y + square.Z);
        }

        public static Vector3f Cross(Vector3f left, Vector3f right)
        {
            return new Vector3f(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }
    }
}