using System;
using System.Numerics;
using SFML.System;

namespace RayTracingGPU.Camera
{
    public class FirstPersonCamera
    {
        public float FOV => MathF.PI / 2;

        private Vector2f _angle;

        public float MoveSpeed { get; } = 0.1f;

        private const float MouseSensitivity = 0.1f;

        public Vector3f Position { get; set; } = new Vector3f(-1, 1.5f, -2);

        public Vector3f Forward { get; set; } = new Vector3f(0, 0, 1);

        public Vector3f Right { get; set; } = new Vector3f(-1, 0, 0);

        public Vector3f Direction { get; set; } = new Vector3f(0, 0, 1);

        public Vector3f Up => VectorMath.Cross(Direction, Right);
        
        public void Move(float x, float y, float z)
        {
            Vector3f offset = new Vector3f();

            offset += x * Right;
            offset += y * Forward;
            offset.Y += z;

            offset = VectorMath.Normalize(offset);
            offset *= MoveSpeed;

            Position += offset;
        }

        public void Rotate(float x, float y)
        {
            _angle = new Vector2f(x * MathF.PI / 180, y * MathF.PI / 180) * MouseSensitivity;

            _angle.X -= MathF.PI * 2 * MathF.Floor(_angle.X / MathF.PI * 2);
            _angle.Y = -MathF.Max(MathF.Min(_angle.Y, MathF.PI / 2 - 0.001f), -MathF.PI / 2 + 0.001f);

            Direction = new Vector3f(
                MathF.Cos(_angle.Y) * MathF.Sin(_angle.X),
                MathF.Sin(_angle.Y),
                MathF.Cos(_angle.Y) * MathF.Cos(_angle.X)
            );

            Forward = new Vector3f(
                MathF.Sin(_angle.X),
                0.0f,
                MathF.Cos(_angle.X)
            );

            Right = new Vector3f(
                MathF.Sin(_angle.X - MathF.PI / 2),
                0.0f,
                MathF.Cos(_angle.X - MathF.PI / 2)
            );
        }
    }
}