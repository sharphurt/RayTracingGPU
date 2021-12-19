using System.Drawing;

namespace RayTracingGPU
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var raytracing = new RayTracing(new Size(1920, 1080));
            raytracing.Run();
        }
    }
}
