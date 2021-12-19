using System;
using System.Timers;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using GPURayTracing;
using RayTracingGPU.Camera;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;
using SFML.Window;
using static SFML.Window.Keyboard;
using Color = SFML.Graphics.Color;


namespace RayTracingGPU
{
    public class RayTracing
    {
        public Vector2u WindowSize { get; }
        public float MouseSensivity { get; } = 0.001f;
        public float Speed { get; } = 0.05f;
        public bool ShowCursor { get; set; }

        public Vector3f Position;

        private FirstPersonCamera _camera = new FirstPersonCamera();

        private Vector2f _mousePosition;

        private RenderWindow _window;

        private Texture _texture;

        private RenderTexture _outputTexture;
        private Sprite _outputTextureSprite;
        private Sprite _outputTextureSpriteFlipped;

        private Text _info;

        private Shader _shader;

        private bool _pause;
        private bool _frameMode;
        private bool _captureFrame;

        private int _accumulatedFrames { get; set; } = 1;
        private Clock _clock { get; set; }
        private float _fps;

        private Random _random = new Random();

        public RayTracing(Size size)
        {
            WindowSize = new Vector2u((uint) size.Width, (uint) size.Height);
            Initialize();
        }

        private void Initialize()
        {
            _window = new RenderWindow(new VideoMode(WindowSize.X, WindowSize.Y), "Ray tracing");
            _window.SetFramerateLimit(60);
            _window.SetMouseCursorVisible(ShowCursor);

            _outputTexture = new RenderTexture(WindowSize.X, WindowSize.Y);

            _outputTextureSprite = new Sprite(_outputTexture.Texture);
            _outputTextureSpriteFlipped = CreateFlipped(_outputTextureSprite);

            using (var image = new Image("Assets/Textures/chess.png"))
            {
                _texture = new Texture(image);
            }

            _clock = new Clock();
            _info = new Text
            {
                CharacterSize = 18,
                Font = new Font("Assets/Fonts/gilroy-regular.ttf"),
                FillColor = Color.White,
                Position = new Vector2f(10, 10)
            };

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(LoadFile.Load("Shaders/raytracing.glsl"))))
            {
                _shader = new Shader(null, null, stream);
                _shader.SetUniform("u_resolution", new Vec2(WindowSize.X, WindowSize.Y));
            }

            _window.GainedFocus += (sender, args) => _accumulatedFrames = 1;
            _window.MouseButtonPressed += OnMouseButtonPressed;
        }

        private void OnMouseButtonPressed(object? sender, MouseButtonEventArgs e)
        {
            if (ShowCursor)
                _accumulatedFrames = 1;

            _window.SetMouseCursorVisible(false);
            ShowCursor = false;
        }

        private void OnMouseMoved()
        {
            if (ShowCursor || !_window.HasFocus())
                return;


            var mx = Mouse.GetPosition().X - WindowSize.X / 2f;
            var my = Mouse.GetPosition().Y - WindowSize.Y / 2f;
            _mousePosition.X += mx;
            _mousePosition.Y += my;
            Mouse.SetPosition(new Vector2i((int) (WindowSize.X / 2), (int) (WindowSize.Y / 2)));
            
            if (Math.Abs(mx) > 1e-9 || Math.Abs(my) > 1e-9)
                _accumulatedFrames = 1;
        }

        private void OnKeyPressed()
        {
            if (ShowCursor || !_window.HasFocus())
                return;

            if (IsKeyPressed(Key.Escape))
                _window.Close();

            if (IsKeyPressed(Key.F12))
                TakeScreenshot();

            if (IsKeyPressed(Key.F5))
                _pause = !_pause;

            if (IsKeyPressed(Key.F) && Keyboard.IsKeyPressed(Key.LControl))
                _frameMode = !_frameMode;

            if (_frameMode && !_captureFrame && IsKeyPressed(Key.Enter))
                _captureFrame = true;

            if (_pause || _frameMode)
                return;

            var wasdUD = new[]
            {
                IsKeyPressed(Key.W), IsKeyPressed(Key.A), IsKeyPressed(Key.S),
                IsKeyPressed(Key.D), IsKeyPressed(Key.Space), IsKeyPressed(Key.LShift)
            };

            /*
            if (wasdUD[0])
                _camera.Move(1, 0, 0);
            if (wasdUD[2])
                _camera.Move(-1, 0, 0);
            if (wasdUD[1])
                _camera.Move(0, -1, 0);
            if (wasdUD[3])
                _camera.Move(0, 1, 0);
                */

            var direction = new Vector3f();

            float mx = (_mousePosition.X) * MouseSensivity;
            float my = (_mousePosition.Y) * MouseSensivity;

            if (wasdUD[0])
                direction = new Vector3f(1, 0, 0);
            if (wasdUD[2])
                direction = new Vector3f(-1, 0, 0);
            if (wasdUD[1])
                direction = new Vector3f(0, -1, 0);
            if (wasdUD[3])
                direction = new Vector3f(0, 1, 0);

            var dirTemp = new Vector3f(
                direction.Z * MathF.Sin(-my) + direction.X * MathF.Cos(-my),
                direction.Y,
                direction.Z * MathF.Cos(-my) - direction.X * MathF.Sin(-my));

            direction.X = dirTemp.X * MathF.Cos(mx) - dirTemp.Y * MathF.Sin(mx);
            direction.Y = dirTemp.X * MathF.Sin(mx) + dirTemp.Y * MathF.Cos(mx);
            direction.Z = dirTemp.Z;

            Position += direction * Speed;

            
            if (wasdUD[4])
                Position += new Vector3f(0, 0, -1) * Speed;
            if (wasdUD[5])
                Position += new Vector3f(0, 0, 1) * Speed;
                

            if (wasdUD.Any(v => v))
            {
                _accumulatedFrames = 1;
            }
        }

        public void Run()
        {
            while (_window.IsOpen)
            {
                OnKeyPressed();
                OnMouseMoved();

                /*
                float mx = (_mousePosition.X / WindowSize.X - 0.5f) * MouseSensivity;
                float my = (_mousePosition.Y / WindowSize.Y - 0.5f) * MouseSensivity;
                */

                if (!_pause || _captureFrame)
                {
                    _shader.SetUniform("u_pos", Position);
                    _shader.SetUniform("u_direction", _camera.Direction);
                    _shader.SetUniform("u_up", _camera.Up);
                    _shader.SetUniform("u_mouse", _mousePosition * MouseSensivity);
                    _shader.SetUniform("u_sample_part", 1.0f / _accumulatedFrames);
                    _shader.SetUniform("u_chess_texture", _texture);
                    var r1 = _random.NextDouble();
                    var r2 = _random.NextDouble();
                    _shader.SetUniform("u_seed1", new Vector2f((float) r1, (float) r1 + 999));
                    _shader.SetUniform("u_seed2", new Vector2f((float) r2, (float) r2 + 999));

                    _outputTexture.Draw(_outputTextureSpriteFlipped, new RenderStates(_shader));

                    _accumulatedFrames++;
                }

                if (_captureFrame)
                    _captureFrame = false;

                float currentTime = _clock.ElapsedTime.AsSeconds();
                float fps = 1f / (currentTime);

                _clock.Restart();
                _info.DisplayedString =
                    $"FPS {fps:00.00}\nAccumulated frames: {_accumulatedFrames}\n{Position}\n\nPause - {_pause}\nFrame mode - {_frameMode}";

                _window.Draw(_outputTextureSprite);
                _window.Draw(_info);
                _window.Display();
            }
        }

        private void TakeScreenshot()
        {
            var img = _outputTextureSprite.Texture.CopyToImage();
            var path = $@"D:\Screenshots\Screenshot_{DateTime.Now:dd_MM_yyyy_hh_mm_ss}.png";
            var isSuccessful = img.SaveToFile(path);
            if (isSuccessful)
                Console.WriteLine($"Saved screeenshot {path}");
        }

        private Sprite CreateFlipped(Sprite sprite) => new Sprite(sprite.Texture)
            {Scale = new Vector2f(1, -1), Position = new Vector2f(0, WindowSize.Y)};
    }
}