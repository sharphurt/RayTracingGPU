using System;
using RayTracingGPU;
using SFML.System;

namespace ImprovedRayTracing
{
    public class Camera
    {
        public float FOV { get; set; } = MathF.PI / 2;
        public Vector3f Position = new Vector3f(1, 1, 1);
        public Vector3f Orientation = new Vector3f((float) Math.PI, 0f, -3f);
        public float MoveSpeed { get; set; } = 1f;
        private const float MouseSensitivity = 0.025f;

        public Vector3f LookDirection => new Vector3f
        {
            X = (float) (Math.Sin(Orientation.X) * Math.Cos(Orientation.Y)),
            Y = (float) Math.Sin(Orientation.Y),
            Z = (float) (Math.Cos(Orientation.X) * Math.Cos(Orientation.Y))
        };

        public Vector3f Right => new Vector3f(
            MathF.Sin(Orientation.X - MathF.PI / 2),
            0.0f,
            MathF.Cos(Orientation.X - MathF.PI / 2)
        );

        public Vector3f Up => VectorMath.Cross(LookDirection, Right);

        public void Move(float x, float y, float z)
        {
            var offset = new Vector3f();

            var forward = new Vector3f((float) Math.Sin(Orientation.X), 0, (float) Math.Cos(Orientation.X));
            var right = new Vector3f(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset = VectorMath.Normalize(offset);
            offset *= MoveSpeed;

            Position += offset;
        }

        public void AddRotation(float x, float y)
        {
            x *= MouseSensitivity;
            y *= MouseSensitivity;

            Orientation.X = (Orientation.X + x) % ((float) Math.PI * 2.0f);
            Orientation.Y = Math.Max(Math.Min(Orientation.Y + y, (float) Math.PI / 2.0f - 0.1f),
                (float) -Math.PI / 2.0f + 0.1f);
        }
    }
}