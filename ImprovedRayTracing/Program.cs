using System.Drawing;

namespace ImprovedRayTracing
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
