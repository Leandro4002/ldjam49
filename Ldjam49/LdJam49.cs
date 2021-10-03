﻿using ChaiFoxes.FMODAudio;
using Genbox.VelcroPhysics.Collision.Filtering;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Factories;
using Genbox.VelcroPhysics.Utilities;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ldjam49Namespace {
    public class Ldjam49 : Game {
        public readonly GraphicsDeviceManager graphicsDevice;
        public static SpriteBatch spriteBatch;
        public static RenderTarget2D gameRender, lightRender;
        public static bool showFps, IsExiting, isPhysicsActivated, isGameOver, isWinning = true;
        public static float roomOfManeuver = 0.3f; // The bigger the more time the player has to change direction before it is not possible again
        public static float ballRadius = 5.5f;
        public const int TILE_SIZE = 50;
        public static float ambientOpacity = 0.5f, ambientOpacityVariation = 0.02f;
        public static Random random;
        public static readonly Vector2 HALF_TILE = new Vector2(TILE_SIZE / 2, TILE_SIZE / 2);
        public static int[][] tiles;
        public static int targetScore;
        public static Body[][] tilesBody;
        public static Texture2D DebugTileTexture, PlayerTexture, borderTexture, ballTexture;
        public static World world;
        public static Dictionary<string, Texture2D> textures, animations;
        public static KeyboardState kState, oldKState;
        public static Matrix centerImageMatrix, halfTileMatrix;
        public static Vector2 centerDisplacement;
        public static Ghost[] ghosts;
        public static BlendState multiplyBlendState;
        public static Delay thunderDelay, thunderDuration, gameStartsDelay;
        public static float gameStartsWaitTime = 6;
        public static float thunderMinWaitTime = 20, thunderMaxWaitTime = 30, thunderPerturbation = 10;
        public static Dictionary<string, Sound> sounds;
        public static Sound mainMusicSound, rainSound, wakaSound;
        public static Channel mainMusicChannel, rainChannel, wakaChannel;
        public static ImGuiRenderer imGuiRenderer;
        public static bool isDebugWindowOpen = true;
        public static ImGuiIOPtr io;
        public static ImGuiStylePtr style;
        public static ImFontPtr scoreFont;

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

        public static bool Contains(Array a, object val) {
            return Array.IndexOf(a, val) != -1;
        }

        protected override void Initialize() {
            graphicsDevice.PreferMultiSampling = true;
            graphicsDevice.PreferredBackBufferWidth = 1600;
            graphicsDevice.PreferredBackBufferHeight = 900;
            graphicsDevice.IsFullScreen = false;
            graphicsDevice.ApplyChanges();

            imGuiRenderer = new ImGuiRenderer(this);
            io = ImGui.GetIO();
            scoreFont = io.Fonts.AddFontFromFileTTF("Content/fonts/Pixel-Digivolve.ttf", 20);
            imGuiRenderer.RebuildFontAtlas();
            style = ImGui.GetStyle();
            style.WindowRounding = 0;
            style.Colors[(int)ImGuiCol.TitleBg] = style.Colors[(int)ImGuiCol.TitleBgActive];

            FMODManager.Init(FMODMode.CoreAndStudio, "Content/audio");
            sounds = LoadAudioInPath("Content/audio");

            rainSound = CoreSystem.LoadStreamedSound("rain.mp3");
            rainSound.Volume = 0.1f;
            rainChannel = rainSound.Play();
            rainChannel.Looping = true;

            mainMusicSound = CoreSystem.LoadStreamedSound("Monkeys-Spinning-Monkeys.mp3");
            mainMusicSound.Volume = 1f;
            mainMusicSound.Looping = true;

            Sound sound = CoreSystem.LoadStreamedSound("pacman-start.mp3");
            sound.Volume = 1f;
            sound.Play();

            wakaSound = CoreSystem.LoadStreamedSound("waka.mp3");
            wakaSound.Volume = 0.2f;
            wakaSound.Looping = true;

            random = new Random();

            thunderDelay = new Delay((float)random.NextDouble() * (thunderMaxWaitTime - thunderMinWaitTime) + thunderMinWaitTime);
            thunderDuration = new Delay(2, false);
            gameStartsDelay = new Delay(gameStartsWaitTime);

            spriteBatch = new SpriteBatch(GraphicsDevice);

            tiles = new int[][] {
                new int[] {24, 24, 27, 3, 0, 19, 0, 19, 0, 4, 28, 24, 24},
                new int[] {8, 8, 3, 0, 0, 0, 0, 0, 0, 0, 4, 8, 8},
                new int[] {0, 0, 0, 0, 16, 9, 9, 9, 18, 0, 0, 0, 0},
                new int[] {9, 9, 21, 0, 0, 0, 0, 0, 0, 0, 20, 9, 9},
                new int[] {0, 0, 12, 18, 0, 16, 9, 18, 0, 16, 13, 0, 0},
                new int[] {2, 0, 10, 0, 0, 0, 0, 0, 0, 0, 10, 0, 1},
                new int[] {3, 0, 19, 0, 1, 6, 6, 6, 2, 0, 19, 0, 4},
                new int[] {0, 0, 0, 0, 4, 8, 8, 8, 3, 0, 0, 0, 0},
                new int[] {2, 0, 17, 0, 0, 0, 0, 0, 0, 0, 17, 0, 1},
                new int[] {7, 0, 19, 0, 16, 21, 0, 20, 18, 0, 19, 0, 5},
                new int[] {7, 0, 0, 0, 0, 10, 0, 10, 0, 0, 0, 0, 5},
                new int[] {26, 6, 6, 2, 0, 10, 0, 10, 0, 1, 6, 6, 25}
            };

            centerDisplacement = new Vector2(610, 240);
            centerImageMatrix = Matrix.CreateTranslation(new Vector3(centerDisplacement.X, centerDisplacement.Y, 0)) * Matrix.CreateScale(.8f);
            halfTileMatrix = Matrix.CreateTranslation(new Vector3(HALF_TILE.X, HALF_TILE.Y, 0));

            multiplyBlendState = new BlendState() {
                ColorBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.DestinationColor,
                ColorDestinationBlend = Blend.Zero,
            };

            showFps = true;

            world = new World(Vector2.Zero);
            world.Gravity = new Vector2(0, 50);

            tilesBody = new Body[tiles.Length][];

            for (int y = 0; y < tiles.Length; ++y) {
                tilesBody[y] = new Body[tiles[y].Length];
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] != 0) {
                        float width = 40, height = 40;
                        if (Contains(new int[]{ 9, 16, 18}, tiles[y][x])) height -= 10;
                        if (Contains(new int[] { 10, 17, 19 }, tiles[y][x])) width -= 10;
                        tilesBody[y][x] = BodyFactory.CreateRectangle(world, width, height, 1f);
                        tilesBody[y][x].BodyType = BodyType.Static;
                        tilesBody[y][x].Position = new Vector2(TILE_SIZE * x, TILE_SIZE * y);
                        tilesBody[y][x].CollisionCategories = Category.Cat1;
                        tilesBody[y][x].FixedRotation = true;
                    } else {
                        tilesBody[y][x] = BodyFactory.CreateCircle(world, ballRadius, 1);
                        tilesBody[y][x].BodyType = BodyType.Static;
                        tilesBody[y][x].Position = new Vector2(TILE_SIZE * x, TILE_SIZE * y);
                        tilesBody[y][x].CollisionCategories = Category.Cat4;
                        tilesBody[y][x].CollidesWith = Category.Cat2;
                        tilesBody[y][x].Restitution = 0.7f;
                        tilesBody[y][x].Friction = 0.3f;
                        ++targetScore;
                    }
                    if (y == 5 && x == 6) {
                        Ldjam49.tilesBody[y][x].BodyType = BodyType.Kinematic;
                        Ldjam49.tilesBody[y][x].Position = new Vector2(-9999);
                        Ldjam49.tilesBody[y][x].GravityScale = 0;
                        continue;
                    }
                }
            }

            base.Initialize();
        }

        public static int roomWidth => tiles[0].Length* TILE_SIZE;
        public static int roomHeight => tiles.Length* TILE_SIZE;

        protected override void LoadContent() {
            base.LoadContent();

            textures = Content.LoadPath<Texture2D>("Content/textures");
            animations = Content.LoadPath<Texture2D>("Content/animations");

            Player.Init();

            ghosts = new Ghost[] {
                new Ghost(Ghost.EnemyType.red, 3, 1) { speed = 25, target = Direction.Right, direction = Direction.Right },
                new Ghost(Ghost.EnemyType.blue, 9, 1) { target = Direction.Left, direction = Direction.Left },
                new Ghost(Ghost.EnemyType.pink, 1, 10) { speed = 15, target = Direction.Right, direction = Direction.Right },
                new Ghost(Ghost.EnemyType.orange, 11, 10) { target = Direction.Left, direction = Direction.Left }, };

            gameRender = new RenderTarget2D(GraphicsDevice, roomWidth, roomHeight);
            lightRender = new RenderTarget2D(GraphicsDevice, roomWidth, roomHeight);

            DebugTileTexture = Tools.CreateRectTexture(GraphicsDevice, 50, 50, Color.Lime);
            ballTexture = Tools.CreateCircleTexture(GraphicsDevice, (int)ballRadius, Color.Yellow, 2);
            borderTexture = Tools.CreateRectTexture(GraphicsDevice, tiles[0].Length * TILE_SIZE, tiles.Length * TILE_SIZE, Color.Black, 5);
        }

        protected override void UnloadContent() {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            gameStartsDelay.Update(dt);
            if (gameStartsDelay.isTrigger) {
                if (!wakaChannel.IsPlaying) {
                    wakaChannel = wakaSound.Play();
                }
            }

            if (!isGameOver) {
                thunderDelay.Update(dt);
                thunderDuration.Update(dt);

                if (thunderDelay.isTrigger) {
                    thunderDelay.timer = (float)random.NextDouble() * (thunderMaxWaitTime - thunderMinWaitTime) + thunderMinWaitTime;
                    thunderDelay.Reset();
                    thunderDuration.Reset();
                    string audioFile = "thunder" + random.Next(1, 3);
                    sounds[audioFile].Volume = 0.5f;
                    sounds[audioFile].Play();
                    if (!isPhysicsActivated) {
                        ActivatePhysics();
                    } else {
                        ShakeEverything();
                    }
                    Debug.WriteLine("THUNDER");
                }

                ambientOpacity = MathHelper.Clamp(ambientOpacity + (float)(random.NextDouble() - 0.5) * ambientOpacityVariation * (!thunderDuration.isTrigger ? thunderPerturbation : 1), 0, 1);

                kState = Keyboard.GetState();
            }

            if (kState.IsKeyDown(Keys.P)) {
                ActivatePhysics();
            }

            if (kState.IsKeyDown(Keys.R)) {
                //Reset
            }

            if (kState.IsKeyDown(Keys.Escape)) Exit();

            Player.Update(dt);

            foreach (Ghost ghost in ghosts) {
                ghost.Update(dt);
            }

            for (int y = 0; y < tiles.Length; ++y) {
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tilesBody[y][x] == null || tiles[y][x] != 0 || tilesBody[y][x].BodyType == BodyType.Kinematic) continue;

                    if (tilesBody[y][x].Position.X < -HALF_TILE.X) tilesBody[y][x].Position = tilesBody[y][x].Position.ChangeX(roomWidth - HALF_TILE.X - ballRadius);
                    if (tilesBody[y][x].Position.X > roomWidth - HALF_TILE.X) tilesBody[y][x].Position = tilesBody[y][x].Position.ChangeX(-HALF_TILE.X + ballRadius);
                    if (tilesBody[y][x].Position.Y < -HALF_TILE.Y) tilesBody[y][x].Position = tilesBody[y][x].Position.ChangeY(roomHeight - HALF_TILE.Y - ballRadius);
                    if (tilesBody[y][x].Position.Y > roomHeight - HALF_TILE.Y) tilesBody[y][x].Position = tilesBody[y][x].Position.ChangeY(-HALF_TILE.Y + ballRadius);
                }
            }

            oldKState = kState;

            // variable time step but never less then 30 Hz
            world.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f));

            base.Update(gameTime);
        }

        void ShakeEverything() {
            if (!isPhysicsActivated) return;

            Vector2 force;
            force = GetRandomVector(50000);
            force.Y = -Math.Abs(force.Y);
            Player.body.ApplyLinearImpulse(force);
            Player.body.ApplyAngularImpulse((float)random.NextDouble() * 1000000 - 500000);

            for (int i = 0; i < ghosts.Length; ++i) {
                force = GetRandomVector(50000);
                force.Y = -Math.Abs(force.Y);
                ghosts[i].body.ApplyLinearImpulse(force);
                ghosts[i].body.ApplyAngularImpulse((float)random.NextDouble() * 1000000 - 500000);
            }

            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null || tilesBody[y][x].FixedRotation == true) continue;
                    force = GetRandomVector(5000);
                    force.Y = -Math.Abs(force.Y);
                    tilesBody[y][x].ApplyLinearImpulse(force);
                }
            }
        }

        void ActivatePhysics() {
            if (isPhysicsActivated) return;
            isPhysicsActivated = true;
            mainMusicChannel = mainMusicSound.Play();

            Player.body.GravityScale = 1;
            Player.body.FixedRotation = false;
            Player.body.Awake = true;
            Player.body.CollidesWith = Category.All;

            for (int i = 0; i < ghosts.Length; ++i) {
                ghosts[i].body.GravityScale = 1;
                ghosts[i].body.FixedRotation = false;
                ghosts[i].body.Awake = true;
                ghosts[i].body.CollidesWith = Category.All;
                ghosts[i].isActive = false;
            }

            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null || tilesBody[y][x].FixedRotation == true) continue;
                    tilesBody[y][x].BodyType = (tilesBody[y][x].BodyType == BodyType.Kinematic) ? BodyType.Kinematic : BodyType.Dynamic;
                    tilesBody[y][x].CollidesWith = Category.All;
                }
            }

            ShakeEverything();
        }
        
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.SetRenderTarget(gameRender);
            spriteBatch.Begin();
            spriteBatch.Draw(textures["background"], Vector2.Zero, Color.White);
            spriteBatch.End();
            spriteBatch.Begin(transformMatrix: halfTileMatrix);
            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null) continue;
                    if (tiles[y][x] == 0 && tilesBody[y][x].FixedRotation == false) {
                        spriteBatch.Draw(ballTexture, tilesBody[y][x].Position, null, Color.White, 0, new Vector2(ballRadius), 1f, SpriteEffects.None, 0f);
                    } else {
                        spriteBatch.Draw(textures["tile_" + tiles[y][x]], new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            Player.Draw();
            foreach (Ghost ghost in ghosts) {
                ghost.Draw();
            }
            spriteBatch.Draw(borderTexture, - new Vector2(TILE_SIZE / 2), Color.White);
            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.TransparentBlack);

            //Light
            /*GraphicsDevice.SetRenderTarget(lightRender);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(textures["pacman_light"], Player.body.Position + HALF_TILE, null, Color.White, 0, new Vector2(textures["pacman_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
            foreach (Ghost ghost in ghosts) {
                string thing = ghost.ghostTexture.Name.Substring((int)(ghost.ghostTexture.Name.IndexOf("/")) + 1);
                spriteBatch.Draw(textures[thing + "_light"], ghost.body.Position + HALF_TILE, null, Color.White, 0, new Vector2(textures[thing + "_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
            }
            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);*/

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(textures["mask"], Vector2.Zero, Color.White);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, transformMatrix: centerImageMatrix);
            spriteBatch.Draw(textures["pacman_light"], Player.body.Position + HALF_TILE, null, Color.White, 0, new Vector2(textures["pacman_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
            foreach (Ghost ghost in ghosts) {
                string thing = ghost.ghostTexture.Name.Substring((int)(ghost.ghostTexture.Name.IndexOf("/")) + 1);
                spriteBatch.Draw(textures[thing + "_light"], ghost.body.Position + HALF_TILE, null, Color.White, 0, new Vector2(textures[thing + "_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
            }
            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null || tilesBody[y][x].FixedRotation == true || tilesBody[y][x].BodyType == BodyType.Kinematic) continue;
                    spriteBatch.Draw(textures["ball_light"], tilesBody[y][x].Position + HALF_TILE, null, Color.White, 0, new Vector2(textures["ball_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, multiplyBlendState, transformMatrix: centerImageMatrix);
            spriteBatch.Draw(gameRender, Vector2.Zero, Color.White);
            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.Draw(textures["background-screen"], Vector2.Zero, Color.White);
            spriteBatch.Draw(textures["vignette"], Vector2.Zero, Color.Black * ambientOpacity);
            spriteBatch.End();

            imGuiRenderer.BeforeLayout(gameTime);

            if (isDebugWindowOpen) {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(370, centerDisplacement.Y * .8f));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(113, roomHeight * .8f));
                ImGui.Begin("DEBUG", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

                ImGui.Text("Red direction"); ImGui.Text(ghosts[0].direction.ToString().ToUpper());
                ImGui.Text("Blue direction"); ImGui.Text(ghosts[1].direction.ToString().ToUpper());
                ImGui.Text("Pink direction"); ImGui.Text(ghosts[2].direction.ToString().ToUpper());
                ImGui.Text("Orange direction"); ImGui.Text(ghosts[3].direction.ToString().ToUpper());
                ImGui.Text("Player direction: "); ImGui.Text(Player.direction.ToString().ToUpper());
                ImGui.Separator();
                ImGui.Text("isPhysicsActivated"); ImGui.Text(isPhysicsActivated.ToString().ToUpper());
                ImGui.Text("ambientOpacity"); ImGui.Text(ambientOpacity.ToString());
                ImGui.Text("FPS"); ImGui.Text(((int)io.Framerate).ToString());
                
                ImGui.End();
            }

            ImGui.PushFont(scoreFont);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(1010, centerDisplacement.Y * .8f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(113, roomHeight * .8f));
            ImGui.Begin("display", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar);
            
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text("SCORE");
            ImGui.SetWindowFontScale(1);
            ImGui.Text(string.Format("{0:000}", Player.score));
            ImGui.Text("\n");
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text("TARGET");
            ImGui.SetWindowFontScale(1);
            ImGui.Text(string.Format("{0:000}", targetScore));

            ImGui.End();

            imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        Dictionary<string, Sound> LoadAudioInPath(string path) {
            Dictionary<string, Sound> content = new Dictionary<string, Sound>();

            string[] files = Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories);
            foreach (string file in files) {
                string loadPath = file.Replace(@"\", "/").Replace("Content/", "").Replace(".mp3", "");
                string key = loadPath.Replace(path.Replace("Content/", ""), "").Replace("/", "");
                content[key] = CoreSystem.LoadStreamedSound(key + ".mp3");
            }

            return content;

        }

        Vector2 GetRandomVector(float force) {
            return new Vector2((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1)) * force;
        }

        protected override void Dispose(bool disposing) {

            base.Dispose(disposing);
        }
    }
}