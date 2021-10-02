using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Utilities;
using Genbox.VelcroPhysics.Factories;
using MonoGameUtilities;

namespace ldjam49Namespace {
    public class Ldjam49 : Game {
        public readonly GraphicsDeviceManager graphicsDevice;
        public SpriteBatch spriteBatch;
        public bool showFps, IsExiting;
        public Body rectangle, ground;
        public Texture2D rectangleTexture, groundTexture;
        public World World;

        public Ldjam49() {
            Window.Title = "LdJam49";
            graphicsDevice = new GraphicsDeviceManager(this);
            ConvertUnits.SetDisplayUnitToSimUnitRatio(24f);
            IsFixedTimeStep = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            graphicsDevice.PreferMultiSampling = true;
            graphicsDevice.PreferredBackBufferWidth = 1600;
            graphicsDevice.PreferredBackBufferHeight = 900;
            graphicsDevice.IsFullScreen = false;
            graphicsDevice.ApplyChanges();

            spriteBatch = new SpriteBatch(GraphicsDevice);

            showFps = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            base.LoadContent();

            World = new World(Vector2.Zero);
            World.Gravity = new Vector2(0, 10);

            rectangle = BodyFactory.CreateRectangle(World, 5f, 5f, 1f);
            rectangle.Position = new Vector2(20, 0);
            rectangle.Rotation = (float)(Math.PI / 5);
            rectangle.BodyType = BodyType.Dynamic;

            ground = BodyFactory.CreateRectangle(World, 30f, 2f, 1f);
            ground.Position = new Vector2(33, 10);
            ground.BodyType = BodyType.Static;

            rectangleTexture = Tools.CreateRectTexture(GraphicsDevice, (int)ConvertUnits.ToDisplayUnits(5f), (int)ConvertUnits.ToDisplayUnits(5f), Color.Red);
            groundTexture = Tools.CreateRectTexture(GraphicsDevice, (int)ConvertUnits.ToDisplayUnits(30f), (int)ConvertUnits.ToDisplayUnits(2f), Color.Blue);
        }

        protected override void UnloadContent() {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {

            // variable time step but never less then 30 Hz
            World.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            spriteBatch.Begin();
            spriteBatch.Draw(rectangleTexture, ConvertUnits.ToDisplayUnits(rectangle.Position), null, Color.White, rectangle.Rotation, ConvertUnits.ToDisplayUnits(new Vector2(5, 5) / 2), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(groundTexture, ConvertUnits.ToDisplayUnits(ground.Position), null, Color.White, ground.Rotation, ConvertUnits.ToDisplayUnits(new Vector2(30, 2) / 2), 1f, SpriteEffects.None, 0f);
            spriteBatch.End();
            
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing) {

            base.Dispose(disposing);
        }
    }
}