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
        public SpriteBatch spriteBatch;
        public bool showFps, IsExiting;
        public float roomOfManeuver = 0.3f; // The bigger the more time the player has to change direction before it is not possible again
        public Body playerBody;
        public const int TILE_SIZE = 50;
        public readonly Vector2 HALF_TILE = new Vector2(TILE_SIZE / 2, TILE_SIZE / 2);
        public int[][] tiles;
        public Body[][] tilesBody;
        public float playerSpeed, playerRadius = 15;
        public Texture2D DebugTileTexture, PlayerTexture;
        public World World;
        public Dictionary<string, Texture2D> textures;
        public Direction playerDirection, targetDirection;
        public KeyboardState kState, oldKState;
        public Matrix transformMatrix;
        public int blockingTileX, blockingTileY;

        public enum Direction {
            Right, Down, Left, Up
        }

        public Ldjam49() {
            Window.Title = "LdJam49";
            graphicsDevice = new GraphicsDeviceManager(this);
            ConvertUnits.SetDisplayUnitToSimUnitRatio(1);
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

            tiles = new int[][] {
                new int[] {1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1},
                new int[] {1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1},
                new int[] {0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0},
                new int[] {1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1},
                new int[] {0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 0},
                new int[] {1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1},
                new int[] {1, 0, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0, 1},
                new int[] {0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0},
                new int[] {1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1},
                new int[] {1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1},
                new int[] {1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1},
                new int[] {1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1}
            };

            transformMatrix = Matrix.CreateTranslation(new Vector3(0, 0, 0)) * Matrix.CreateScale(1);

            playerDirection = Direction.Right;
            playerSpeed = 50;
            showFps = true;

            World = new World(Vector2.Zero);
            World.Gravity = new Vector2(0, 50);

            tilesBody = new Body[tiles.Length][];

            for (int y = 0; y < tiles.Length; ++y) {
                tilesBody[y] = new Body[tiles[y].Length];
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] == 1) {
                        tilesBody[y][x] = BodyFactory.CreateRectangle(World, 40f, 40f, 1f);
                        tilesBody[y][x].BodyType = BodyType.Static;
                        tilesBody[y][x].Position = new Vector2(TILE_SIZE * x, TILE_SIZE * y);
                    } else {
                        tilesBody[y][x] = null; //Maybe useless
                    }
                }
            }

            playerBody = BodyFactory.CreateCircle(World, playerRadius, 1f);
            playerBody.Position = new Vector2(TILE_SIZE * 6, TILE_SIZE * 5);
            playerBody.BodyType = BodyType.Dynamic;
            playerBody.GravityScale = 0;
            playerBody.FixedRotation = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            base.LoadContent();

            DebugTileTexture = Tools.CreateRectTexture(GraphicsDevice, 50, 50, Color.Lime);

            textures = Content.LoadPath<Texture2D>("Content/textures");
        }

        protected override void UnloadContent() {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            kState = Keyboard.GetState();

            targetDirection = playerDirection;
            if (kState.IsKeyDown(Keys.Left) || kState.IsKeyDown(Keys.A)) targetDirection = Direction.Left;
            if (kState.IsKeyDown(Keys.Right) || kState.IsKeyDown(Keys.D)) targetDirection = Direction.Right;
            if (kState.IsKeyDown(Keys.Up) || kState.IsKeyDown(Keys.W)) targetDirection = Direction.Up;
            if (kState.IsKeyDown(Keys.Down) || kState.IsKeyDown(Keys.S)) targetDirection = Direction.Down;
            bool canChangePosition = true;

            blockingTileX = -1; blockingTileY = -1;
            for (int y = 0; y < tiles.Length; ++y) {
                if (!canChangePosition) break;
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (!canChangePosition) break;
                    if (tiles[y][x] != 1) continue;
                    float xPos = TILE_SIZE * x;
                    float yPos = TILE_SIZE * y;
                    float someVal = 1.1f;
                    switch (targetDirection) {
                        case Direction.Down:
                            if (yPos > playerBody.Position.Y + playerRadius && yPos < playerBody.Position.Y + playerRadius + 1.5f * TILE_SIZE && Math.Abs(xPos - playerBody.Position.X) < TILE_SIZE / someVal) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Direction.Up:
                            if (yPos < playerBody.Position.Y - playerRadius && yPos > playerBody.Position.Y - playerRadius - 1.5f * TILE_SIZE && Math.Abs(xPos - playerBody.Position.X) < TILE_SIZE / someVal) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Direction.Left:
                            if (xPos < playerBody.Position.X && xPos >= playerBody.Position.X - TILE_SIZE && Math.Abs(yPos - playerBody.Position.Y) <= TILE_SIZE / 2f + TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Direction.Right:
                            if (xPos > playerBody.Position.X && xPos <= playerBody.Position.X + TILE_SIZE && Math.Abs(yPos - playerBody.Position.Y) <= TILE_SIZE / 2f + TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                    }
                }
            }
            
            if (canChangePosition && targetDirection != playerDirection) {
                if ((targetDirection == Direction.Up || targetDirection == Direction.Down) && (playerDirection == Direction.Left || playerDirection == Direction.Right)) {
                    playerBody.Position = new Vector2((int)(playerBody.Position.X / TILE_SIZE + 1) * TILE_SIZE, playerBody.Position.Y);

                    if ((playerDirection == Direction.Left && targetDirection == Direction.Down) || (playerDirection == Direction.Left && targetDirection == Direction.Up)) {
                        playerBody.Position = new Vector2(playerBody.Position.X - TILE_SIZE, playerBody.Position.Y);
                    }
                }
                if ((targetDirection == Direction.Left || targetDirection == Direction.Right) && (playerDirection == Direction.Up || playerDirection == Direction.Down)) {
                    playerBody.Position = new Vector2(playerBody.Position.X, (int)(playerBody.Position.Y / TILE_SIZE + 1) * TILE_SIZE);

                    if ((playerDirection == Direction.Up && targetDirection == Direction.Right) || (playerDirection == Direction.Up && targetDirection == Direction.Left)) {
                        playerBody.Position = new Vector2(playerBody.Position.X, playerBody.Position.Y - TILE_SIZE);
                    }
                }
                playerDirection = targetDirection;
                playerBody.Rotation = (float)((int)playerDirection * Math.PI / 2);
                Debug.WriteLine("DIRECTION CHANGED");
            }

            oldKState = kState;

            Vector2 playerMovement = Vector2.Zero;
            switch (playerDirection) {
                case Direction.Down: playerMovement.Y = playerSpeed; break;
                case Direction.Up: playerMovement.Y = -playerSpeed; break;
                case Direction.Left: playerMovement.X = -playerSpeed; break;
                case Direction.Right: playerMovement.X = playerSpeed; break;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space)) { playerBody.Position += playerMovement * dt; playerBody.Awake = true; }

            float offset = 18;
            bool isPlayerBlocked = false;
            for (int y = 0; y < tiles.Length; ++y) {
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] != 1) continue;
                    float xPos = TILE_SIZE * x;
                    float yPos = TILE_SIZE * y;
                    switch (playerDirection) {
                        case Direction.Down:
                            if (Tools.Rect2Rect(playerBody.Position.X - playerRadius/2, playerBody.Position.Y - playerRadius / 2 + offset, playerRadius, playerRadius, xPos - TILE_SIZE / 2, yPos - TILE_SIZE / 2, TILE_SIZE, TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Direction.Up:
                            if (Tools.Rect2Rect(playerBody.Position.X - playerRadius / 2, playerBody.Position.Y - playerRadius / 2 - offset, playerRadius, playerRadius, xPos - TILE_SIZE / 2, yPos - TILE_SIZE / 2, TILE_SIZE, TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Direction.Left:
                            if (Tools.Rect2Rect(playerBody.Position.X - playerRadius / 2 - offset, playerBody.Position.Y - playerRadius / 2, playerRadius, playerRadius, xPos - TILE_SIZE / 2, yPos - TILE_SIZE / 2, TILE_SIZE, TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Direction.Right:
                            if (Tools.Rect2Rect(playerBody.Position.X - playerRadius / 2 + offset, playerBody.Position.Y - playerRadius / 2, playerRadius, playerRadius, xPos - TILE_SIZE / 2, yPos - TILE_SIZE / 2, TILE_SIZE, TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                    }
                }
            }
            if (isPlayerBlocked) {
                playerBody.Position -= playerMovement * dt;
            }

            if (kState.IsKeyDown(Keys.P)) {
                playerBody.GravityScale = 1;
                playerBody.FixedRotation = false;
            }

            // variable time step but never less then 30 Hz
            World.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);

            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null) continue;
                    spriteBatch.Draw(textures["tile"], new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                }
            }
            if (blockingTileX != -1 && blockingTileY != -1) {
                //spriteBatch.Draw(DebugTileTexture, new Vector2(blockingTileX, blockingTileY) - HALF_TILE, null, Color.White *.5f, playerBody.Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(textures["pacman"], playerBody.Position, null, Color.White, playerBody.Rotation, new Vector2(playerRadius), 1f, SpriteEffects.None, 0f);
            
            spriteBatch.End();
            
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing) {

            base.Dispose(disposing);
        }
    }
}