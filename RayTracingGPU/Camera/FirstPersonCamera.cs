using System;
using System.Numerics;
using SFML.System;

namespace RayTracingGPU.Camera
{
    public class FirstPersonCamera
    {
        public Vector3f Position { get; set; } = new Vector3f(-1, 1.5f, -2);
        
        private Vector3f Orientation = new Vector3f((float) Math.PI, 0f, 1f);
        
        public float MoveSpeed { get; set; }
        
        private const float MouseSensitivity = 0.0025f;

        public Vector3f Up => VectorMath.Cross(Direction, VectorMath.Cross(new Vector3f(0, 0, 1), Direction));

        public Vector3f Direction => new Vector3f
        {
            X = (float) (Math.Sin(Orientation.X) * Math.Cos(Orientation.Y)),
            Y = (float) Math.Sin(Orientation.Y),
            Z = (float) (Math.Cos(Orientation.X) * Math.Cos(Orientation.Y))
        };

        public void Move(float x, float y, float z)
        {
            Vector3f offset = new Vector3f();

            Vector3f forward = new Vector3f((float) Math.Sin(Orientation.X), 0, (float) Math.Cos(Orientation.X));
            Vector3f right = new Vector3f(-forward.Z, 0, forward.X);

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