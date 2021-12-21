using System.Drawing;

namespace RayTracingGPU
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var raytracing = new RayTracing(new Size(1280, 720));
            raytracing.Run();
        }
    }
}
