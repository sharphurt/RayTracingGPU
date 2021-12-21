using System.IO;
using System.Text;
using SFML.Graphics;

namespace RayTracingGPU
{
    public static class DataShader
    {
        public static string CreateDataShader(Scene scene, string shaderTemplatePath)
        {
            var shaderStart = CreateShaderStart(scene, shaderTemplatePath);
            return CreateInitializeFunc(scene);
        }

        private static string CreateShaderStart(Scene scene, string shaderTemplatePath)
        {
            var shaderTemplate = File.ReadAllText(shaderTemplatePath);
            return shaderTemplate.Replace("%box_count%", scene.Boxes.Count.ToString());
        }

        private static string CreateInitializeFunc(Scene scene)
        {
            var builder = new StringBuilder("void InitializeScene() {");
            for (int i = 0; i < scene.Boxes.Count; i++)
            {
                builder.Append(scene.Boxes[i].ToString(i));
            }

            builder.Append("}");
            return builder.ToString();
        }
    }
}