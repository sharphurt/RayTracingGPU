using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using GPURayTracing;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;
using SFML.Window;
using static SFML.Window.Keyboard;
using Color = SFML.Graphics.Color;


namespace ImprovedRayTracing
{
    public class RayTracing
    {
        public Vector2u WindowSize { get; }
        public float MouseSensivity { get; } = 0.001f;
        public float Speed { get; } = 0.1f;
        public bool ShowCursor { get; set; }

        public Vector3f Position { get; set; }

        private Camera _camera;

        private Vector2f _mousePosition;
        private Vector2f _lastMousePosition;

        private RenderWindow _window;

        private Texture _texture;

        private RenderTexture _outputTexture;
        private Sprite _outputTextureSprite;
        private Sprite _outputTextureSpriteFlipped;

        private RenderTexture _resultTexture;
        private Sprite _resultTextureSprite;

        private Text _info;

        private Shader _shader;
        private Shader _postProcessShader;

        private bool _pause;
        private bool _frameMode;
        private bool _captureFrame;

        private int _accumulatedFrames { get; set; } = 1;
        private Clock _clock { get; set; }
        private float _fps;
        private float _time;

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

            _camera = new Camera();

            _outputTexture = new RenderTexture(WindowSize.X, WindowSize.Y);
            _outputTextureSprite = new Sprite(_outputTexture.Texture);
            _outputTextureSpriteFlipped = CreateFlipped(_outputTextureSprite);

            _resultTexture = new RenderTexture(WindowSize.X, WindowSize.Y);
            _resultTextureSprite = new Sprite(_resultTexture.Texture);

            using (var image = new Image("Assets/Textures/chess.jpg"))
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

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(LoadFile.Load("Shaders/raytracing2.glsl"))))
            {
                _shader = new Shader(null, null, stream);
            }

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(LoadFile.Load("Shaders/postprocess.glsl"))))
            {
                _postProcessShader = new Shader(null, null, stream);
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

            var delta = _lastMousePosition - new Vector2f(Mouse.GetPosition().X, Mouse.GetPosition().Y);

            if (delta == new Vector2f(0, 0))
                return;

            _lastMousePosition += delta;
            _camera.AddRotation(delta.X, delta.Y);

            //  if (Focused) Mouse.SetPosition(Width / 2f, Height / 2f);

            _lastMousePosition = new Vector2f(Mouse.GetPosition().X, Mouse.GetPosition().Y);
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

            if (wasdUD[0])
                _camera.Move(0f, 1, 0f);
            if (wasdUD[2])
                _camera.Move(0f, -1, 0f);
            if (wasdUD[1])
                _camera.Move(-1, 0f, 0f);
            if (wasdUD[3])
                _camera.Move(1, 0f, 0f);

            if (wasdUD[4])
                _camera.Move(0f, 0f, 1);
            if (wasdUD[5])
                _camera.Move(0f, 0f, -1);

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

                if (!_pause || _captureFrame)
                {
                    _shader.SetUniform("uPosition", _camera.Position);
                    _shader.SetUniform("uDirection", _camera.LookDirection);
                    _shader.SetUniform("uUp", _camera.Up);
                    _shader.SetUniform("uFOV", _camera.FOV);
                    _shader.SetUniform("uTime", _time);
                    _shader.SetUniform("uViewportSize", new Vector2f(WindowSize.X, WindowSize.Y));

                    _shader.SetUniform("uSamples", 16);

                    _outputTexture.Draw(_outputTextureSpriteFlipped, new RenderStates(_shader));

                    _accumulatedFrames++;

                    
                    _postProcessShader.SetUniform("uImage", _outputTexture.Texture);
                    _postProcessShader.SetUniform("uImageSamples", 16);

                    _resultTexture.Draw(_resultTextureSprite, new RenderStates(_postProcessShader));
                }

                if (_captureFrame)
                    _captureFrame = false;

                float currentTime = _clock.ElapsedTime.AsSeconds();
                float fps = 1f / (currentTime);

                _clock.Restart();
                _info.DisplayedString =
                    $"FPS {fps:00.00}\nAccumulated frames: {_accumulatedFrames}\n{Position}\n\nPause - {_pause}\nFrame mode - {_frameMode}";

                _window.Draw(_resultTextureSprite);
                _window.Draw(_info);
                _window.Display();
                _time += 0.01f;
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