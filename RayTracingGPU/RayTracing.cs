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
        private Vector2u WindowSize { get; }
        private bool ShowCursor { get; set; }

        private FirstPersonCamera _camera = new FirstPersonCamera();

        private Vector2i _lastMousePosition;
        private Vector2i _mousePos;

        private RenderWindow _window;

        private Texture _texture;

        private RenderTexture _firstTexture;
        private Sprite _firstTextureSprite;
        private Sprite _firstTextureSpriteFlipped;

        private RenderTexture _secondTexture;
        private Sprite _secondTextureSprite;
        private Sprite _secondTextureSpriteFlipped;

        private Text _info;

        private Shader _shader;

        private bool _pause;
        private bool _frameMode;
        private bool _captureFrame;

        private int _accumulatedFrames { get; set; } = 1;
        private Clock _clock { get; set; }

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

            _firstTexture = new RenderTexture(WindowSize.X, WindowSize.Y);
            _firstTextureSprite = new Sprite(_firstTexture.Texture);
            _firstTextureSpriteFlipped = CreateFlipped(_firstTextureSprite);

            _secondTexture = new RenderTexture(WindowSize.X, WindowSize.Y);
            _secondTextureSprite = new Sprite(_secondTexture.Texture);
            _secondTextureSpriteFlipped = CreateFlipped(_secondTextureSprite);

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

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(LoadFile.Load("Shaders/rt.glsl"))))
            {
                _shader = new Shader(null, null, stream);
            }

            _window.GainedFocus += (sender, args) => _accumulatedFrames = 1;
        }

        private void OnMouseMoved()
        {
            if (ShowCursor || !_window.HasFocus())
                return;

            var delta = Mouse.GetPosition();
            if (delta - _lastMousePosition != new Vector2i(0, 0))
                _accumulatedFrames = 1;

            _mousePos += delta - _lastMousePosition;

            _camera.Rotate(_mousePos.X, _mousePos.Y);

            _lastMousePosition = new Vector2i((int) (WindowSize.X / 2), (int) (WindowSize.Y / 2f));
            Mouse.SetPosition(_lastMousePosition);
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
                _camera.Move(0, 1, 0);
            if (wasdUD[2])
                _camera.Move(0, -1, 0);
            if (wasdUD[1])
                _camera.Move(1, 0, 0);
            if (wasdUD[3])
                _camera.Move(-1, 0, 0);
            if (wasdUD[4])
                _camera.Move(0, 0, 1);
            if (wasdUD[5])
                _camera.Move(0, 0, -1);

            if (wasdUD.Any(v => v))
            {
                _accumulatedFrames = 1;
            }
        }

        public void Run()
        {
            while (_window.IsOpen)
            {
                _window.DispatchEvents();
                OnKeyPressed();
                OnMouseMoved();

                if (!_pause || _captureFrame)
                {
                    _shader.SetUniform("uViewportSize", new Vec2(WindowSize.X, WindowSize.Y));
                    _shader.SetUniform("uPosition", _camera.Position);
                    _shader.SetUniform("uDirection", _camera.Direction);
                    _shader.SetUniform("uUp", _camera.Up);
                    _shader.SetUniform("uFOV", _camera.FOV);
                    _shader.SetUniform("uSamples", 4);

                    var r1 = _random.NextDouble();
                    var r2 = _random.NextDouble();

                    _shader.SetUniform("uSeed1", new Vector2f((float) r1, (float) r1 + 999));
                    _shader.SetUniform("uSeed2", new Vector2f((float) r2, (float) r2 + 999));

                    _shader.SetUniform("uSamplePart", 1f / _accumulatedFrames);
                    
                    if (_accumulatedFrames % 2 == 1)
                    {
                        _shader.SetUniform("uSample", _firstTexture.Texture);
                        _secondTexture.Draw(_firstTextureSpriteFlipped, new RenderStates(_shader));
                        _window.Draw(_secondTextureSprite);
                    }
                    else
                    {
                        _shader.SetUniform("uSample", _secondTexture.Texture);
                        _firstTexture.Draw(_secondTextureSpriteFlipped, new RenderStates(_shader));
                        _window.Draw(_firstTextureSprite);
                    }
                    
                    _accumulatedFrames++;
                }


                if (_captureFrame)
                    _captureFrame = false;

                float currentTime = _clock.ElapsedTime.AsSeconds();
                float fps = 1f / (currentTime);

                _clock.Restart();
                _info.DisplayedString =
                    $"FPS {fps:00.00}\nAccumulated frames: {_accumulatedFrames}\n{_camera.Position}\n\nPause - {_pause}\nFrame mode - {_frameMode}";
                
                _window.Draw(_info);
                _window.Display();
            }
        }

        private void TakeScreenshot()
        {
            var img = _secondTexture.Texture.CopyToImage();
            var path = $@"D:\Screenshots\Screenshot_{DateTime.Now:dd_MM_yyyy_hh_mm_ss}.png";

            var isSuccessful = img.SaveToFile(path);
            if (isSuccessful)
                Console.WriteLine($"Saved screeenshot {path}");
        }

        private Sprite CreateFlipped(Sprite sprite) => new Sprite(sprite.Texture)
        {
            Scale = new Vector2f(1, -1), Position = new Vector2f(0, WindowSize.Y)
        };
    }
}