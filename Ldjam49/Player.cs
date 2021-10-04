using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Utilities;
using Genbox.VelcroPhysics.Factories;
using Genbox.VelcroPhysics.Collision.Filtering;
using Genbox.VelcroPhysics.Collision.ContactSystem;
using Genbox.VelcroPhysics.Collision.Handlers;
using MonoGameUtilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace ldjam49Namespace {
    public static class Player {
        public static float speed = 50, radius = 15, marioSpeed = 80000;
        public static Ldjam49.Direction direction, target;
        public static Body body;
        public static int score; //TODO an "unstability" makes the imGui debug window appear
        public static int blockingTileX, blockingTileY;
        public static AnimatedSprite runAnim, dieAnim, marioRunAnim;
        public static bool isMario, marioIsOnAir;
        public static bool? marioIsFalling;
        public static float lastYPos, fallingThreshold = 1f;
        public static int marioIsRunning;
        public static Vector2 marioFakeInertia;
        public static float marioInertiaLoss = 500, jumpForce = 800, fakeMarioGravityForce = 1800;
        public static int currentTileX, currentTileY;

        public static void Init() {
            direction = Ldjam49.Direction.Right;
            score = 0;

            body = BodyFactory.CreateCircle(Ldjam49.world, radius, 1f);
            body.Position = new Vector2(Ldjam49.TILE_SIZE * 6, Ldjam49.TILE_SIZE * 5);
            body.BodyType = BodyType.Dynamic;
            body.GravityScale = 0;
            body.FixedRotation = true;
            body.Restitution = 0.4f;
            body.CollisionCategories = Category.Cat2;
            body.CollidesWith = Category.Cat1;
            body.OnCollision = (Fixture a, Fixture b, Contact c) => {
                if (b.Body.BodyType == BodyType.Static) {
                    marioFakeInertia.Y = 0;
                    if (b.Body.Position.Y > a.Body.Position.Y) {
                        marioIsOnAir = false;
                    }
                }
            };

            runAnim = new AnimatedSprite(Ldjam49.animations["pacRun_f14w30h30c8r2"], 30).animParam(isLooping: true);
            runAnim.currentFrame = 3;
            runAnim.CalculateCurrentFramePosition();
            runAnim.origin = new Vector2(radius);
            dieAnim = new AnimatedSprite(Ldjam49.animations["pacCollapse_f17w30h30c1r17"], 6).animParam(isActive: false);
            dieAnim.origin = new Vector2(radius);

            isMario = false;
            marioRunAnim = new AnimatedSprite(Ldjam49.animations["mario-run_f3w27h27c3r1"], 10).animParam(isLooping: true);
            marioRunAnim.origin = new Vector2(radius) + Vector2.UnitY * 2;
            marioRunAnim.scale = new Vector2(1.5f);
        }

        public static void Update(float dt) {
            currentTileX = (int)(body.Position.X + Ldjam49.HALF_TILE.Y) / Ldjam49.TILE_SIZE;
            currentTileY = (int)(body.Position.Y + Ldjam49.HALF_TILE.Y) / Ldjam49.TILE_SIZE;

            if (Ldjam49.gameStartsDelay.isTrigger) {
                runAnim.Update(dt);
            }

            dieAnim.Update(dt);

            if (!Ldjam49.isWinning && !Ldjam49.isGameOver && Ldjam49.gameStartsDelay.isTrigger) {
                for (int y = 0; y < Ldjam49.tilesBody.Length; ++y) {
                    for (int x = 0; x < Ldjam49.tilesBody[y].Length; ++x) {
                        if (Ldjam49.tilesBody[y][x] == null || Ldjam49.tilesBody[y][x].FixedRotation == true) { continue; }
                        if (Tools.Circle2Circle(body.Position.X, body.Position.Y, (isMario ? radius * 1.2f : radius), Ldjam49.tilesBody[y][x].Position.X, Ldjam49.tilesBody[y][x].Position.Y, Ldjam49.ballRadius)) {
                            Ldjam49.tilesBody[y][x].BodyType = BodyType.Kinematic;
                            Ldjam49.tilesBody[y][x].Position = new Vector2(-9999);
                            Ldjam49.tilesBody[y][x].GravityScale = 0;
                            ++score;
                            Ldjam49.sounds[Ldjam49.getBallSoundEffect].Play();

                            if (score >= Ldjam49.targetScore) {
                                Win();
                            }
                        }
                    }
                }

                foreach (Ghost ghost in Ldjam49.ghosts) {
                    if (ghost.isActive
                        || (ghost.enemyType == Ghost.EnemyType.blue && Ghost.changeBlueEnemy)
                        || (ghost.enemyType == Ghost.EnemyType.orange && Ghost.changeOrangeEnemy)) {
                        if (Tools.Circle2Circle(body.Position.X, body.Position.Y, radius, ghost.body.Position.X, ghost.body.Position.Y, ghost.radius)) {
                            GameOver();
                        }
                    }
                }

                foreach (Bullet bullet in Ldjam49.bullets) {
                    if (Tools.Circle2Circle(body.Position.X, body.Position.Y, radius, bullet.position.X, bullet.position.Y, Bullet.radius)) {
                        GameOver();
                    }
                }

                if (Ldjam49.isPhysicsActivated) {
                    if (isMario) {
                        UpdateMovementForMarioLike(dt);
                    }
                } else {
                    UpdateMovementForPacmanLike(dt);
                }
            }

            if (!Ldjam49.isGameOver) {
                if (body.Position.X < -Ldjam49.HALF_TILE.X) body.Position = body.Position.ChangeX(Ldjam49.roomWidth - Ldjam49.HALF_TILE.X - radius);
                if (body.Position.X > Ldjam49.roomWidth - Ldjam49.HALF_TILE.X) body.Position = body.Position.ChangeX(-Ldjam49.HALF_TILE.X + radius);
                if (body.Position.Y < -Ldjam49.HALF_TILE.Y) body.Position = body.Position.ChangeY(Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y - radius);
                if (body.Position.Y > Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y) { body.Position = body.Position.ChangeY(-Ldjam49.HALF_TILE.Y + radius); }
            } else if (isMario) {
                marioFakeInertia.Y += fakeMarioGravityForce/2 * dt;
                body.Position += marioFakeInertia * dt;
            }
        }

        public static void UpdateMovementForMarioLike(float dt) {
            float playerMovement = 0;
            if (Ldjam49.kState.IsKeyDown(Keys.Left) || Ldjam49.kState.IsKeyDown(Keys.A)) playerMovement -= speed;
            if (Ldjam49.kState.IsKeyDown(Keys.Right) || Ldjam49.kState.IsKeyDown(Keys.D)) playerMovement += speed;
            if (!marioIsOnAir && ((Ldjam49.kState.IsKeyDown(Keys.Space) && !Ldjam49.oldKState.IsKeyDown(Keys.Space))
                || Ldjam49.kState.IsKeyDown(Keys.W) && !Ldjam49.oldKState.IsKeyDown(Keys.W))) { marioFakeInertia.Y -= jumpForce; }

            body.ApplyLinearImpulse(new Vector2(playerMovement * marioSpeed * dt, 0));
            if (playerMovement == 0) {
                marioIsRunning = 0;
                marioRunAnim.currentFrame = 0;
                //marioRunAnim.timer = 0; //not sure about that
                marioRunAnim.CalculateCurrentFramePosition();
            } else {
                if (playerMovement > 0) {marioIsRunning = 1; marioRunAnim.spriteEffects = SpriteEffects.FlipHorizontally; }
                else { marioIsRunning = -1; marioRunAnim.spriteEffects = SpriteEffects.None; }
                marioRunAnim.Update(dt);
            }

            if (marioIsOnAir) marioFakeInertia.Y += fakeMarioGravityForce * dt;
            body.Position += marioFakeInertia * dt;
            body.Awake = true;
            marioFakeInertia.X = Tools.BringToZero(marioFakeInertia.X, marioInertiaLoss * dt);
            marioFakeInertia.Y = Tools.BringToZero(marioFakeInertia.Y, marioInertiaLoss * dt);

            float deltaY = body.Position.Y - lastYPos;
            if (Math.Abs(deltaY) < fallingThreshold) {
                marioIsFalling = null;
            } else if (lastYPos > body.Position.Y) {
                marioIsFalling = false;
                marioIsOnAir = true;
            } else {
                marioIsFalling = true;
                marioIsOnAir = true;
            }

            if (body.Position.Y == lastYPos) marioIsOnAir = false;

            lastYPos = body.Position.Y;
        }

        public static void UpdateMovementForPacmanLike(float dt) {
            target = direction;
            if (Ldjam49.kState.IsKeyDown(Keys.Left) || Ldjam49.kState.IsKeyDown(Keys.A)) target = Ldjam49.Direction.Left;
            if (Ldjam49.kState.IsKeyDown(Keys.Right) || Ldjam49.kState.IsKeyDown(Keys.D)) target = Ldjam49.Direction.Right;
            if (Ldjam49.kState.IsKeyDown(Keys.Up) || Ldjam49.kState.IsKeyDown(Keys.W)) target = Ldjam49.Direction.Up;
            if (Ldjam49.kState.IsKeyDown(Keys.Down) || Ldjam49.kState.IsKeyDown(Keys.S)) target = Ldjam49.Direction.Down;
            bool canChangePosition = true;

            blockingTileX = -1; blockingTileY = -1;
            for (int y = 0; y < Ldjam49.tiles.Length; ++y) {
                if (!canChangePosition) break;
                for (int x = 0; x < Ldjam49.tiles[y].Length; ++x) {
                    if (!canChangePosition) break;
                    if (Ldjam49.tiles[y][x] == 0) continue;
                    float xPos = Ldjam49.TILE_SIZE * x;
                    float yPos = Ldjam49.TILE_SIZE * y;
                    float roomOfManeuver = 0.3f;
                    switch (target) {
                        case Ldjam49.Direction.Up:
                            if (yPos < body.Position.Y && yPos >= body.Position.Y - Ldjam49.TILE_SIZE && Math.Abs(xPos - body.Position.X) <= Ldjam49.TILE_SIZE / 2f + Ldjam49.TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileY = (int)yPos; blockingTileX = (int)xPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Down:
                            if (yPos > body.Position.Y && yPos <= body.Position.Y + Ldjam49.TILE_SIZE && Math.Abs(xPos - body.Position.X) <= Ldjam49.TILE_SIZE / 2f + Ldjam49.TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileY = (int)yPos; blockingTileX = (int)xPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Left:
                            if (xPos < body.Position.X && xPos >= body.Position.X - Ldjam49.TILE_SIZE && Math.Abs(yPos - body.Position.Y) <= Ldjam49.TILE_SIZE / 2f + Ldjam49.TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Right:
                            if (xPos > body.Position.X && xPos <= body.Position.X + Ldjam49.TILE_SIZE && Math.Abs(yPos - body.Position.Y) <= Ldjam49.TILE_SIZE / 2f + Ldjam49.TILE_SIZE / (2f + roomOfManeuver)) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                    }
                }
            }

            if (canChangePosition && target != direction) {
                if ((target == Ldjam49.Direction.Up || target == Ldjam49.Direction.Down) && (direction == Ldjam49.Direction.Left || direction == Ldjam49.Direction.Right)) {
                    body.Position = new Vector2((int)(body.Position.X / Ldjam49.TILE_SIZE + 1) * Ldjam49.TILE_SIZE, body.Position.Y);

                    if ((direction == Ldjam49.Direction.Left && target == Ldjam49.Direction.Down) || (direction == Ldjam49.Direction.Left && target == Ldjam49.Direction.Up)) {
                        body.Position = new Vector2(body.Position.X - Ldjam49.TILE_SIZE, body.Position.Y);
                    }
                }
                if ((target == Ldjam49.Direction.Left || target == Ldjam49.Direction.Right) && (direction == Ldjam49.Direction.Up || direction == Ldjam49.Direction.Down)) {
                    body.Position = new Vector2(body.Position.X, (int)(body.Position.Y / Ldjam49.TILE_SIZE + 1) * Ldjam49.TILE_SIZE);

                    if ((direction == Ldjam49.Direction.Up && target == Ldjam49.Direction.Right) || (direction == Ldjam49.Direction.Up && target == Ldjam49.Direction.Left)) {
                        body.Position = new Vector2(body.Position.X, body.Position.Y - Ldjam49.TILE_SIZE);
                    }
                }
                direction = target;
                body.Rotation = (float)((int)direction * Math.PI / 2);
            }

            Vector2 playerMovement = Vector2.Zero;
            switch (direction) {
                case Ldjam49.Direction.Down: playerMovement.Y = speed; break;
                case Ldjam49.Direction.Up: playerMovement.Y = -speed; break;
                case Ldjam49.Direction.Left: playerMovement.X = -speed; break;
                case Ldjam49.Direction.Right: playerMovement.X = speed; break;
            }

            body.Position += playerMovement * dt;

            float offset = 18;
            bool isPlayerBlocked = false;
            for (int y = 0; y < Ldjam49.tiles.Length; ++y) {
                for (int x = 0; x < Ldjam49.tiles[y].Length; ++x) {
                    if (Ldjam49.tiles[y][x] == 0) continue;
                    float xPos = Ldjam49.TILE_SIZE * x;
                    float yPos = Ldjam49.TILE_SIZE * y;
                    switch (direction) {
                        case Ldjam49.Direction.Down:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2, body.Position.Y - radius / 2 + offset, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Up:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2, body.Position.Y - radius / 2 - offset, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Left:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2 - offset, body.Position.Y - radius / 2, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Right:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2 + offset, body.Position.Y - radius / 2, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isPlayerBlocked = true;
                            }
                            break;
                    }
                }
            }

            if (isPlayerBlocked) {
                body.Position -= playerMovement * dt;
            }
        }

        public static void TransformToMario() {
            isMario = true;
            body.FixedRotation = true;
            body.Restitution = 0.1f;
            body.GravityScale = 80;
            body.Position = new Vector2(currentTileX * Ldjam49.TILE_SIZE, currentTileY * Ldjam49.TILE_SIZE);
        }

        public static void Draw() {
            if (isMario) {
                if (Ldjam49.isGameOver) {
                    Ldjam49.spriteBatch.Draw(Ldjam49.textures["mario-dead"], body.Position, null, Color.White, 0, new Vector2(radius), marioRunAnim.scale.X, marioRunAnim.spriteEffects, 0f);
                } else {
                    if (marioIsOnAir) {
                        Ldjam49.spriteBatch.Draw(Ldjam49.textures["mario-jump"], body.Position, null, Color.White, 0, new Vector2(radius), marioRunAnim.scale.X, marioRunAnim.spriteEffects, 0f);
                    } else {
                        marioRunAnim.Draw(Ldjam49.spriteBatch, body.Position);
                    }
                }
            } else {
                if (Ldjam49.isGameOver) {
                    dieAnim.Draw(Ldjam49.spriteBatch, body.Position);
                } else {
                    runAnim.rotation = body.Rotation;
                    runAnim.Draw(Ldjam49.spriteBatch, body.Position);
                }
            }
        }

        public static void GameOver() {
            Ldjam49.isGameOver = true;
            Ldjam49.mainMusicChannel.Stop();
            Ldjam49.wakaChannel.Stop();
            Ldjam49.sounds["pacman-die"].Volume = 1.5f;
            Ldjam49.sounds["pacman-die"].Play();
            Ldjam49.glitchChannel.Volume = 0f;
            dieAnim.isActive = true;
            Ldjam49.ambientOpacity = 1;
            marioFakeInertia.Y = -jumpForce/2f;

            if (isMario) {
                body.CollidesWith = Category.None;
            }
        }

        public static void Win() {
            Ldjam49.isWinning = true;
            Ldjam49.mainMusicChannel.Stop();
            Ldjam49.wakaChannel.Stop();
            Ldjam49.glitchChannel.Volume = 0f;
            Ldjam49.sounds["FF7-victory"].Play();
            Ldjam49.ambientOpacity = 0;
        }

        public static void LoadContent() {

        }
    }
}
