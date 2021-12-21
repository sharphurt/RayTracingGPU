using System.Drawing;

namespace RayMarching
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var raytracing = new RayMarching(new Size(1280, 720));
            raytracing.Run();
        }
    }
}
