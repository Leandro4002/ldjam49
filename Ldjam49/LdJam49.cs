using ChaiFoxes.FMODAudio;
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
        public static bool showFps, IsExiting, isPhysicsActivated, isGameOver, isWinning;
        public static float roomOfManeuver = 0.3f; // The bigger the more time the player has to change direction before it is not possible again
        public static float ballRadius = 5.5f;
        public const  int TILE_SIZE = 50;
        public static float ambientOpacity, ambientOpacityVariation = 0.02f;
        public static Random random;
        public static readonly Vector2 HALF_TILE = new Vector2(TILE_SIZE / 2, TILE_SIZE / 2);
        public static int[][] tiles;
        public const  int targetScore = 70; //There is 70 in total
        public static Body[][] tilesBody;
        public static Texture2D DebugTileTexture, PlayerTexture, borderTexture, ballTexture, simpleBallTexture;
        public static World world;
        public static Dictionary<string, Texture2D> textures, animations;
        public static KeyboardState kState, oldKState;
        public static Matrix centerImageMatrix, halfTileMatrix;
        public static Vector2 centerDisplacement;
        public static Ghost[] ghosts;
        public static BlendState multiplyBlendState;
        public static Delay thunderDelay, thunderDuration, gameStartsDelay;
        public static float gameStartsWaitTime = 6;
        public static float thunderWaitTime, thunderPerturbation = 10;
        public static Dictionary<string, Sound> sounds;
        public static Sound mainMusicSound, rainSound, wakaSound, startSound;
        public static Channel mainMusicChannel, rainChannel, wakaChannel;
        public static ImGuiRenderer imGuiRenderer;
        public static bool isDebugWindowOpen, showDebugModeLevel;
        public static ImGuiIOPtr io;
        public static ImGuiStylePtr style;
        public static ImFontPtr scoreFont, endFont;
        public static Action[] events;
        public static Action defaultEvent;
        public static int eventCount;
        public static List<Bullet> bullets;
        public static string getBallSoundEffect;

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
            spriteBatch = new SpriteBatch(GraphicsDevice);

            defaultEvent = () => {
                ShakeEverything(1 + eventCount / 10);
                thunderWaitTime = 5;
            };
            
            events = new Action[] {
                () => {
                    ActivatePhysics();
                    ShakeEverything();
                    mainMusicChannel = mainMusicSound.Play();
                    Player.runAnim.currentFrame = 4;
                    Player.runAnim.CalculateCurrentFramePosition();
                    Player.runAnim.isActive = false;
                    wakaChannel.Stop();//TODO + currentframeX and currentFrameY
                    thunderWaitTime = 5;
                },
                () => {
                    ShakeEverything();                    
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    Player.TransformToMario();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    Ghost.TransformBlueEnemy();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    Ghost.TransformOrangeEnemy();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    SwitchToRingSound();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    Ghost.TransformPinkEnemy();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    SwitchToPokeball();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    showDebugModeLevel = true;
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    Ghost.TransformRedEnemy();
                },
                () => {
                    ShakeEverything();
                    PlayGlitchSound();
                    thunderWaitTime = 5;

                    isDebugWindowOpen = true;
                },
            };

            imGuiRenderer = new ImGuiRenderer(this);
            io = ImGui.GetIO();
            scoreFont = io.Fonts.AddFontFromFileTTF("Content/fonts/Pixel-Digivolve.ttf", 20);
            endFont = io.Fonts.AddFontFromFileTTF("Content/fonts/Pixel-Digivolve.ttf", 50);
            imGuiRenderer.RebuildFontAtlas();
            style = ImGui.GetStyle();
            style.WindowRounding = 0;
            style.Colors[(int)ImGuiCol.TitleBg] = style.Colors[(int)ImGuiCol.TitleBgActive];

            random = new Random();
            multiplyBlendState = new BlendState() {
                ColorBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.DestinationColor,
                ColorDestinationBlend = Blend.Zero,
            };
            showFps = true;
            centerDisplacement = new Vector2(610, 240);
            centerImageMatrix = Matrix.CreateTranslation(new Vector3(centerDisplacement.X, centerDisplacement.Y, 0)) * Matrix.CreateScale(.8f);
            halfTileMatrix = Matrix.CreateTranslation(new Vector3(HALF_TILE.X, HALF_TILE.Y, 0));

            /*   AUDIO   */
            FMODManager.Init(FMODMode.CoreAndStudio, "Content/audio");
            sounds = LoadAudioInPath("Content/audio");
            rainSound = CoreSystem.LoadStreamedSound("rain.mp3");
            rainSound.Volume = 0.1f;
            rainSound.Looping = true;
            mainMusicSound = CoreSystem.LoadStreamedSound("doom-music-loopable.mp3");
            mainMusicSound.Volume = 0.5f;
            mainMusicSound.Looping = true;
            startSound = CoreSystem.LoadStreamedSound("pacman-start.mp3");
            startSound.Volume = 1f;
            wakaSound = CoreSystem.LoadStreamedSound("waka.mp3");
            wakaSound.Volume = 0.2f;
            wakaSound.Looping = true;

            InitGame();

            base.Initialize();
        }

        public static void InitGame() {
            bullets = new List<Bullet>();
            getBallSoundEffect = "powerup";
            rainChannel = rainSound.Play();
            startSound.Play();
            ambientOpacity = 0.5f;
            thunderWaitTime = 20;
            showDebugModeLevel = false;
            isDebugWindowOpen = false;
            Ghost.changeBlueEnemy = false;
            Ghost.changeOrangeEnemy = false;
            Ghost.changePinkEnemy = false;
            eventCount = 0;
            thunderDelay = new Delay(thunderWaitTime);
            thunderDuration = new Delay(2, false);
            gameStartsDelay = new Delay(gameStartsWaitTime);
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
            world = new World(Vector2.Zero);
            world.Gravity = new Vector2(0, 50);
            tilesBody = new Body[tiles.Length][];
            for (int y = 0; y < tiles.Length; ++y) {
                tilesBody[y] = new Body[tiles[y].Length];
                for (int x = 0; x < tiles[y].Length; ++x) {
                    if (tiles[y][x] != 0) {
                        float width = 40, height = 40;
                        if (Contains(new int[] { 9, 16, 18 }, tiles[y][x])) height -= 10;
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
                    }
                    if (y == 5 && x == 6) {
                        tilesBody[y][x].BodyType = BodyType.Kinematic;
                        tilesBody[y][x].Position = new Vector2(-9999);
                        tilesBody[y][x].GravityScale = 0;
                        continue;
                    }
                }
            }
        }

        public static void Reset() {
            isWinning = false; isGameOver = false;
            ballTexture = simpleBallTexture;
            InitGame();
            Player.Init();
            Ghost.Init();
            mainMusicChannel.Stop();
            wakaChannel.Stop();
            isPhysicsActivated = false;
        }

        public static int roomWidth => tiles[0].Length* TILE_SIZE;
        public static int roomHeight => tiles.Length* TILE_SIZE;

        protected override void LoadContent() {
            base.LoadContent();

            textures = Content.LoadPath<Texture2D>("Content/textures");
            animations = Content.LoadPath<Texture2D>("Content/animations");

            Player.Init();
            Ghost.Init();

            gameRender = new RenderTarget2D(GraphicsDevice, roomWidth, roomHeight);
            lightRender = new RenderTarget2D(GraphicsDevice, roomWidth, roomHeight);
            DebugTileTexture = Tools.CreateRectTexture(GraphicsDevice, 50, 50, Color.DarkGreen);
            simpleBallTexture = Tools.CreateCircleTexture(GraphicsDevice, (int)ballRadius, Color.Yellow, 2);
            ballTexture = simpleBallTexture;
            borderTexture = Tools.CreateRectTexture(GraphicsDevice, tiles[0].Length * TILE_SIZE, tiles.Length * TILE_SIZE, Color.Black, 5);
        }

        protected override void UnloadContent() {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            kState = Keyboard.GetState();

            if (kState.IsKeyDown(Keys.R) && !oldKState.IsKeyDown(Keys.R)) Reset();
            if (kState.IsKeyDown(Keys.Escape)) Exit();

            //DEBUG CONTROLS
            if (kState.IsKeyDown(Keys.N) && !oldKState.IsKeyDown(Keys.N)) {
                if (!gameStartsDelay.isTrigger) gameStartsDelay.handler = 0;
                else thunderDelay.handler = 0;
            }
            if (kState.IsKeyDown(Keys.P)) ActivatePhysics();
            if (kState.IsKeyDown(Keys.D1)) Ghost.TransformBlueEnemy();
            if (kState.IsKeyDown(Keys.D2)) Ghost.TransformRedEnemy();
            if (kState.IsKeyDown(Keys.D3)) Ghost.TransformOrangeEnemy();
            if (kState.IsKeyDown(Keys.D4)) Ghost.TransformPinkEnemy();
            if (kState.IsKeyDown(Keys.E) && !oldKState.IsKeyDown(Keys.E)) ShakeEverything();
            if (kState.IsKeyDown(Keys.M)) {
                Player.TransformToMario();
            }

            gameStartsDelay.Update(dt);
            if (gameStartsDelay.isTrigger) {
                if (!isWinning && !isGameOver && !wakaChannel.IsPlaying && !Player.isMario) {
                    wakaChannel = wakaSound.Play();
                }
            }

            if (!isGameOver && !isWinning) {
                List<Bullet> bulletsToRemove = new List<Bullet>();
                for (int i = 0; i < bullets.Count; ++i) {
                    bullets[i].Update(dt);

                    for (int y = 0; y < tiles.Length; ++y) {
                        for (int x = 0; x < tiles[y].Length; ++x) {
                            if (tiles[y][x] == 0 || tilesBody[y][x].BodyType != BodyType.Static) continue;
                            if (Tools.Circle2Rect(bullets[i].position.X, bullets[i].position.Y, Bullet.radius, x * TILE_SIZE - HALF_TILE.X, y * TILE_SIZE - HALF_TILE.X, TILE_SIZE, TILE_SIZE)) {
                                bulletsToRemove.Add(bullets[i]);
                            }
                        }
                    }
                }

                foreach (Bullet bullet in bulletsToRemove) {
                    bullets.Remove(bullet);
                }

                thunderDelay.Update(dt);
                thunderDuration.Update(dt);

                if (thunderDelay.isTrigger) {
                    if (eventCount <= events.Length - 1) {
                        events[eventCount].Invoke();
                    } else {
                        defaultEvent.Invoke();
                    }
                    thunderDelay.timer = thunderWaitTime;
                    thunderDelay.Reset();
                    thunderDuration.Reset();
                    Debug.WriteLine("THUNDER " + eventCount);
                    ++eventCount;

                    string audioFile = "thunder" + random.Next(1, 3);
                    sounds[audioFile].Volume = 0.5f;
                    sounds[audioFile].Play();
                }

                ambientOpacity = MathHelper.Clamp(ambientOpacity + (float)(random.NextDouble() - 0.5) * ambientOpacityVariation * (!thunderDuration.isTrigger ? thunderPerturbation : 1), 0, 1);
            }

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

            // variable time step but never less then 30 Hz
            if (!isWinning && !isGameOver) {
                world.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 30f));
            }

            oldKState = kState;
            base.Update(gameTime);
        }

        void ShakeEverything(float multiplicator = 1) {
            if (!isPhysicsActivated) return;

            Vector2 force;
            float forceValue;

            /* Don't shake the player cause it is fucking anoying
            force;
            forceValue = 50000 * multiplicator;
            force = GetRandomVector(forceValue);
            force.Y = -Math.Abs(force.Y);
            if (Player.isMario) {
                Player.marioFakeInertia.X += force.X/300;
                Player.marioFakeInertia.Y += MathHelper.Clamp(force.Y/80, -float.MaxValue, -500);
            } else {
                Player.body.ApplyLinearImpulse(force);
            }
            Player.body.ApplyAngularImpulse((float)random.NextDouble() * forceValue*2 - forceValue);
            */

            for (int i = 0; i < ghosts.Length; ++i) {
                forceValue = 50000 * multiplicator;
                force = GetRandomVector(forceValue);
                force.Y = -Math.Abs(force.Y);
                ghosts[i].body.ApplyLinearImpulse(force);
                ghosts[i].body.ApplyAngularImpulse((float)random.NextDouble() * forceValue*2 - forceValue);
            }

            for (int y = 0; y < tilesBody.Length; ++y) {
                for (int x = 0; x < tilesBody[y].Length; ++x) {
                    if (tilesBody[y][x] == null || tilesBody[y][x].FixedRotation == true) continue;
                    forceValue = 50000 * multiplicator;
                    force = GetRandomVector(forceValue);
                    force.Y = -Math.Abs(force.Y);
                    tilesBody[y][x].ApplyLinearImpulse(force);
                }
            }
        }

        void SwitchToPokeball() {
            ballTexture = textures["pokeball"];
        }
        void SwitchToRingSound() {
            getBallSoundEffect = "sonic-ring";
        }

        void ActivatePhysics() {
            if (isPhysicsActivated) return;
            isPhysicsActivated = true;

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
                        spriteBatch.Draw(ballTexture, tilesBody[y][x].Position, null, Color.White, tilesBody[y][x].Rotation, new Vector2(ballTexture.Width/2), 1f, SpriteEffects.None, 0f);
                    } else {
                        if (showDebugModeLevel) spriteBatch.Draw(DebugTileTexture, new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(textures["tile_" + tiles[y][x]], new Vector2(x * TILE_SIZE, y * TILE_SIZE), null, Color.White, 0, HALF_TILE, 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            Player.Draw();
            foreach (Ghost ghost in ghosts) {
                ghost.Draw();
            }

            foreach (Bullet bullet in bullets) {
                bullet.Draw();
            }
            spriteBatch.Draw(borderTexture, - new Vector2(TILE_SIZE / 2), Color.White);
            spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.TransparentBlack);

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

            foreach (Bullet bullet in bullets) {
              spriteBatch.Draw(textures["bullet_light"], bullet.position + HALF_TILE, null, Color.White, 0, new Vector2(textures["bullet_light"].Width) / 2, 1f, SpriteEffects.None, 0f);
            }
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, multiplyBlendState, transformMatrix: centerImageMatrix);
            spriteBatch.Draw(gameRender, Vector2.Zero, Color.White);
            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.Draw(textures["background-screen"], Vector2.Zero, Color.White);
            spriteBatch.Draw(textures["vignette"], Vector2.Zero, Color.Black * ambientOpacity);
            if (isWinning || isGameOver) {
                spriteBatch.Draw(textures["screen-vignette"], Vector2.Zero, Color.White);
            }
            spriteBatch.End();

            imGuiRenderer.BeforeLayout(gameTime);

            if (isDebugWindowOpen) {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(370, centerDisplacement.Y * .8f));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(113, roomHeight * .8f));
                ImGui.Begin("DEBUG", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

                ImGui.Text("RED: " + ghosts[0].direction.ToString().ToUpper());
                ImGui.Text("BLUE: " + ghosts[1].direction.ToString().ToUpper());
                ImGui.Text("PINK: " + ghosts[2].direction.ToString().ToUpper());
                ImGui.Text("ORANGE: " + ghosts[3].direction.ToString().ToUpper());
                ImGui.Text("PLAYER: " + Player.direction.ToString().ToUpper());
                ImGui.Separator();
                ImGui.Text("isPhysicsActivated"); ImGui.Text(isPhysicsActivated.ToString().ToUpper());
                ImGui.Text("ambientOpacity"); ImGui.Text(ambientOpacity.ToString());
                ImGui.Text("FPS"); ImGui.Text(((int)io.Framerate).ToString());
                ImGui.Text("marioIsOnAir"); ImGui.Text(Player.marioIsOnAir.ToString().ToUpper());
                ImGui.Text("marioFakeInertia"); ImGui.Text(Player.marioFakeInertia.ToString().ToUpper());
                ImGui.Text("CURRENT TILE"); ImGui.Text(Player.currentTileX + " , " + Player.currentTileY);
                ImGui.Separator();
                ImGui.Text("red.isActive"); ImGui.Text(ghosts[0].isActive.ToString().ToUpper());
                ImGui.Text("blue.isActive"); ImGui.Text(ghosts[1].isActive.ToString().ToUpper());
                ImGui.Text("pink.isActive"); ImGui.Text(ghosts[2].isActive.ToString().ToUpper());
                ImGui.Text("orange.isActive"); ImGui.Text(ghosts[3].isActive.ToString().ToUpper());
                
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

            if (isWinning || isGameOver) {
                ImGui.PushFont(endFont);
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(600, 300));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(500));
                ImGui.Begin("winOrLoose", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar);
                ImGui.SetWindowFontScale(1.3f);
                if (isWinning) {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(99/256f, 165/256f, 86/256f, 1));
                    ImGui.Text("YOU WIN !");
                } else if (isGameOver) {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(165 / 256f, 28 / 256f, 26 / 256f, 1));
                    ImGui.Text("YOU LOOSE...");
                }
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 1, 1, 1));
                ImGui.SetWindowFontScale(0.5f);
                ImGui.Text("YOUR SCORE: " + string.Format("{0:000}", Player.score));
                ImGui.Text("TARGET: " + string.Format("{0:000}", targetScore));
                ImGui.Text("\n");
                ImGui.SetWindowFontScale(0.3f);
                ImGui.Text("Press R to reset");

                ImGui.End();
            }

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

        public static void PlayGlitchSound() {
            string audioFile = "glitch" + random.Next(1, 5);
            sounds[audioFile].Volume = 1f;
            sounds[audioFile].Play();
        }

        public static Vector2 GetRandomVector(float force) {
            return new Vector2((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1)) * force;
        }

        protected override void Dispose(bool disposing) {

            base.Dispose(disposing);
        }
    }
}