using SFML.System;

namespace RayMarching.Camera
{
    public interface ICamera
    {
        Vector3f Position { get; set; }
        Vector3f Forward { get; set; }
        Vector3f Right { get; set; }
        Vector3f Direction { get; set; }
        
        Vector3f Up { get; }

        void Move(float x, float y, float z);
        void Rotate(float x, float y);

    }
}