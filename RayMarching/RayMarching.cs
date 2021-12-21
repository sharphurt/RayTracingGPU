using System.Drawing;
using System.IO;
using System.Text;
using RayMarching.Camera;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using SFML.System;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace RayMarching
{
    public class RayMarching
    {
        private RenderWindow _window;
        private readonly Vector2i WindowSize;

        private Vector2i _lastMousePosition;
        private Vector2i _mousePos;

        private readonly Texture _texture;
        
        private readonly Shader _shader;
        private readonly RenderTexture _outputTexture;
        private readonly Sprite _outputSprite;
        private readonly Sprite _outputFlipped;

        private readonly FirstPersonCamera _camera = new FirstPersonCamera();
        
        public RayMarching(Size size)
        {
            WindowSize = new Vector2i(size.Width, size.Height);
            _window = new RenderWindow(new VideoMode((uint) WindowSize.X, (uint) WindowSize.Y), "Ray marchcing");
            _window.SetFramerateLimit(60);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText("Shaders/raymarching.glsl"))))
            {
                _shader = new Shader(null, null, stream);
            }

            _texture = new Texture("Assets/Textures/chess.png");
            
            _outputTexture = new RenderTexture((uint) WindowSize.X, (uint) WindowSize.Y);
            _outputSprite = new Sprite(_outputTexture.Texture);
            _outputFlipped = new Sprite(_outputSprite) {Scale = new Vector2f(1, -1), Position = new Vector2f(0, WindowSize.Y)};

            _window.Closed += (sender, args) => _window.Close();
        }
        
        
        private void OnMouseMoved()
        {
            var delta = Mouse.GetPosition();
            
            _mousePos += delta - _lastMousePosition;

            _camera.Rotate(_mousePos.X, _mousePos.Y);

            _lastMousePosition = WindowSize / 2;
            Mouse.SetPosition(_lastMousePosition);
        }

        
        private void OnKeyPressed()
        {
            if (IsKeyPressed(Key.Escape))
                _window.Close();
            
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
        }
        
        public void Run()
        {
            while (_window.IsOpen)
            {
                _window.DispatchEvents();
                OnMouseMoved();
                OnKeyPressed();
                
                _shader.SetUniform("uViewportSize", new Vec2(WindowSize.X, WindowSize.Y));
                _shader.SetUniform("uDirection", _camera.Direction);
                _shader.SetUniform("uUp", _camera.Up);
                _shader.SetUniform("uFOV", _camera.FOV);
                _shader.SetUniform("uPosition", _camera.Position);
                _shader.SetUniform("uMainTexture", _texture);
                _outputSprite.Draw(_outputTexture, new RenderStates(_shader));
                _window.Draw(_outputFlipped);
                _window.Display();
            }
        }
    }
}