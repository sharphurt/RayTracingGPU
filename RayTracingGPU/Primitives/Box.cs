using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;

namespace RayTracingGPU.Primitives
{
    public class Box
    {
        public Vector3f Position { get; }
        public Matrix3 Rotation { get; }
        public Vector3f HalfSize { get; }
        public Material Material { get; }

        public string ToString(int index)
        {
            return
$@"
boxes[{index}].position = vec3({Position.X}, {Position.Y}, {Position.Z});
boxes[{index}].rotation = mat3({Rotation.Row1.X}, {Rotation.Row1.Y}, {Rotation.Row1.Z},
                               {Rotation.Row2.X}, {Rotation.Row2.Y}, {Rotation.Row2.Z},
                               {Rotation.Row3.X}, {Rotation.Row3.Y}, {Rotation.Row3.Z});
boxes[{index}].halfSize = vec3({HalfSize.X}, {HalfSize.Y}, {HalfSize.Z});

boxes[{index}].material.emmitance = vec3({Material.Emmitance.X}, {Material.Emmitance.Y}, {Material.Emmitance.Z});
boxes[{index}].material.reflectance = vec3({Material.Rerlectance.X}, {Material.Rerlectance.Y}, {Material.Rerlectance.Z});
boxes[{index}].material.emmitance = {Material.Opacity};    
boxes[{index}].material.roughness = {Material.Roughness};
";
        }
    }
}