using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Utilities;
using Genbox.VelcroPhysics.Factories;
using MonoGameUtilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace ldjam49Namespace {
    public class Ldjam49 : Game {
        public readonly GraphicsDeviceManager graphicsDevice;
        public static SpriteBatch spriteBatch;
        public static bool showFps, IsExiting, isPhysicsActivated;
        public static float roomOfManeuver = 0.3f; // The bigger the more time the player has to change direction before it is not possible again
        public const int TILE_SIZE = 50;
        public static readonly Vector2 HALF_TILE = new Vector2(TILE_SIZE / 2, TILE_SIZE / 2);
        public static int[][] tiles;
        public static Body[][] tilesBody;
        public static Texture2D DebugTileTexture, PlayerTexture;
        public static World world;
        public static Dictionary<string, Texture2D> textures;
        public static KeyboardState kState, oldKState;
        public static Matrix transformMatrix;
        public static Ghost[] ghosts;

        public enum Direction {
            Right, Down, Left, Up
        }

        public Ldjam49() {
            Window.Title = "LdJam49";
            graphicsDevice = new GraphicsDeviceManager(this);
            ConvertUnits.SetDisplayUnitToSimUnitRatio(1);
            IsFixedTimeStep = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize() {
            graphicsDevice.PreferMultiSampling = true;
            graphicsDevice.PreferredBackBufferWidth = 1600;
            graphicsDevice.PreferredBackBufferHeight = 900;
            graphicsDevice.IsFullScreen = false;
            graphicsDevice.ApplyChanges();

            spriteBatch = new SpriteBatch(GraphicsDevice);

            tiles = new int[][] {
                new int[] {1, 6, 27, 3, 0, 19, 0, 19, 0, 4, 28, 6, 2},
                new int[] {4, 8, 3, 0, 0, 0, 0, 0, 0, 0, 4, 8, 3},
                new int[] {0, 0, 0, 0, 16, 9, 9, 9, 18, 0, 0, 0, 0},
                new int[] {16, 9, 21, 0, 0, 0, 0, 0, 0, 0, 20, 9, 9},
                new int[] {0, 0, 12, 18, 0, 16, 9, 18, 0, 16, 13, 0, 0},
                new int[] {2, 0, 10, 0, 0, 0, 0, 0, 0, 0, 10, 0, 1},
                new int[] {3, 0, 19, 0, 1, 6, 6, 6, 2, 0, 19, 0, 4},
                new int[] {0, 0, 0, 0, 4, 8, 8, 8, 3, 0, 0, 0, 0},
                new int[] {2, 0, 17, 0, 0, 0, 0, 0, 0, 0, 17, 0, 1},
                new int[] {7, 0, 19, 0, 16, 21, 0, 20, 18, 0, 19, 0, 5},
                new int[] {7, 0, 0, 0, 0, 10, 0, 10, 0, 0, 0, 0, 5},
                new int[] {26, 6, 6, 6, 0, 10, 0, 10, 0, 1, 6, 6, 25}
            };

            transformMatrix = Matrix.CreateTranslation(new Vector3(0, 0, 0)) * Matrix.CreateScale(1);

            showFps = true;

            world = new World(Vector2.Zero);
            world.Gravity = new Vector2(0, 50);

            tilesBody = new Body[tiles.Length][];

            for (int y = 0; y < tiles.Length; ++y) {
                tilesBody[y] = new Body[tiles[y].Length];
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] != 0) {
                        tilesBody[y][x] = BodyFactory.CreateRectangle(world, 40f, 40f, 1f);
                        tilesBody[y][x].BodyType = BodyType.Static;
                        tilesBody[y][x].Position = new Vector2(TILE_SIZE * x, TILE_SIZE * y);
                    } else {
                        tilesBody[y][x] = null; //Maybe useless
                    }
                }
            }

            Player.Init();

            base.Initialize();
        }

        protected override void LoadContent() {
            base.LoadContent();

            DebugTileTexture = Tools.CreateRectTexture(GraphicsDevice, 50, 50, Color.Lime);

            textures = Content.LoadPath<Texture2D>("Content/textures");

            ghosts = new Ghost[] {
                new Ghost(Ghost.EnemyType.red) { speed = 25 },
                new Ghost(Ghost.EnemyType.blue),
                new Ghost(Ghost.EnemyType.pink) { speed = 15 },
                new Ghost(Ghost.EnemyType.orange) };
        }

        protected override void UnloadContent() {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            kState = Keyboard.GetState();

            Player.Update(dt);

            foreach (Ghost ghost in ghosts) {
                ghost.Update(dt);
            }

            oldKState = kState;

            // variable time step but never less then 30 Hz
            world.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);

            Player.Draw();
            foreach (Ghost ghost in ghosts) {
                ghost.Draw();
            }

            for (int y = 0; y < tiles.Length; ++y) {
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] == 0) continue;
                    spriteBatch.Draw(textures["tile_" + tiles[y][x]], new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(DebugTileTexture, new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White * .5f, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.End();
            
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing) {

            base.Dispose(disposing);
        }
    }
}