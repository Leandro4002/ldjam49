using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Utilities;
using Genbox.VelcroPhysics.Factories;
using Genbox.VelcroPhysics.Collision.Filtering;
using MonoGameUtilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace ldjam49Namespace {
    public class Ghost {
        public bool isActive;
        public float speed = 40, radius = 19, blueEnemyInvaderSpeed = 50;
        public Ldjam49.Direction direction, target;
        public Body body;
        public int blockingTileX, blockingTileY;
        public Texture2D ghostTexture;
        public Delay changeDirectionDelay;
        public Random random;
        public AnimatedSprite goLeft, goRight, goDown, goUp;
        public static AnimatedSprite octorockAnim;
        public IA ia;
        public bool nextDirectionChangeIsRandom;
        public static bool changeBlueEnemy, changeOrangeEnemy, changePinkEnemy;
        public EnemyType enemyType;
        public int currentTileX, currentTileY;
        public static Delay orangeJumpDelay, octorockChangePhase;
        public static float octorockSpeed, octorockPhaseDuration = 3;
        public static bool octorockIsWalking, octorockCanShoot;

        public Ghost (EnemyType enemyType, int xTile, int yTile, float timeBetweenDirectionChange) {
            body = BodyFactory.CreateCircle(Ldjam49.world, radius, 1f);
            body.Position = new Vector2(Ldjam49.TILE_SIZE * xTile, Ldjam49.TILE_SIZE * yTile);
            body.BodyType = BodyType.Dynamic;
            body.GravityScale = 0;
            body.FixedRotation = true;
            body.Restitution = 0.4f;
            changeDirectionDelay = new Delay(timeBetweenDirectionChange, true);
            random = new Random();

            this.enemyType = enemyType;
            goLeft = new AnimatedSprite(Ldjam49.animations["ghost-" + enemyType.ToString() + "-Left_f2w38h38c1r2"], 5).animParam(isLooping: true);
            goRight = new AnimatedSprite(Ldjam49.animations["ghost-" + enemyType.ToString() + "-Right_f2w38h38c1r2"], 5).animParam(isLooping: true);
            goUp = new AnimatedSprite(Ldjam49.animations["ghost-" + enemyType.ToString() + "-Up_f2w38h38c1r2"], 5).animParam(isLooping: true);
            goDown = new AnimatedSprite(Ldjam49.animations["ghost-" + enemyType.ToString() + "-Down_f2w38h38c1r2"], 5).animParam(isLooping: true);
            octorockAnim = new AnimatedSprite(Ldjam49.animations["octorock_f4w38h38c4r1"], 5).animParam(isLooping: true);
            goDown.origin = new Vector2(radius);
            goLeft.origin = new Vector2(radius);
            goRight.origin = new Vector2(radius);
            goUp.origin = new Vector2(radius);
            octorockAnim.origin = new Vector2(radius);
            octorockAnim.scale = new Vector2(1.2f);

            octorockChangePhase = new Delay(octorockPhaseDuration);
            octorockSpeed = 50000; //mario speed is 4 000 000
            octorockIsWalking = true;
            octorockCanShoot = true;

            goUp.currentFrame = random.Next(0, 1);
            goLeft.currentFrame = random.Next(0, 1);
            goRight.currentFrame = random.Next(0, 1);
            goDown.currentFrame = random.Next(0, 1);

            isActive = true;

            body.CollidesWith = Category.Cat1;
            body.CollisionCategories = Category.Cat3;

            ghostTexture = Ldjam49.textures["ghost_" + enemyType.ToString()];
        }

        public static void TransformRedEnemy() {
            for (int i = 0; i < Ldjam49.ghosts.Length; ++i) {
                if (Ldjam49.ghosts[i].enemyType == EnemyType.red) {
                    Ldjam49.ghosts[i].isActive = true;
                    Ldjam49.ghosts[i].body.Position = new Vector2(Ldjam49.ghosts[i].currentTileX * Ldjam49.TILE_SIZE, Ldjam49.ghosts[i].currentTileY * Ldjam49.TILE_SIZE);
                    Ldjam49.ghosts[i].body.BodyType = BodyType.Static;
                    Ldjam49.ghosts[i].body.GravityScale = 0;
                    Ldjam49.ghosts[i].body.FixedRotation = true;
                    Ldjam49.ghosts[i].body.Rotation = 0;
                    Ldjam49.ghosts[i].body.Restitution = 0.4f;
                    Ldjam49.ghosts[i].changeDirectionDelay.Reset();
                    Ldjam49.ghosts[i].body.CollidesWith = Category.None;
                    Ldjam49.ghosts[i].body.CollisionCategories = Category.Cat3;
                    break;
                }
            }
        }

        public static void TransformPinkEnemy() {
            changePinkEnemy = true;
            for (int i = 0; i < Ldjam49.ghosts.Length; ++i) {
                if (Ldjam49.ghosts[i].enemyType == EnemyType.pink) {
                    Ldjam49.ghosts[i].isActive = true;
                    Ldjam49.ghosts[i].body.Position = new Vector2(Ldjam49.ghosts[i].currentTileX * Ldjam49.TILE_SIZE, Ldjam49.ghosts[i].currentTileY * Ldjam49.TILE_SIZE);
                    Ldjam49.ghosts[i].body.BodyType = BodyType.Dynamic;
                    Ldjam49.ghosts[i].body.FixedRotation = true;
                    Ldjam49.ghosts[i].body.Rotation = 0;
                    Ldjam49.ghosts[i].body.Restitution = 0.1f;
                    Ldjam49.ghosts[i].changeDirectionDelay.Reset();
                    Ldjam49.ghosts[i].body.CollidesWith = Category.All;
                    Ldjam49.ghosts[i].body.CollisionCategories = Category.Cat3;
                    break;
                }
            }
        }

        public static void TransformOrangeEnemy() {
            changeOrangeEnemy = true;
            for (int i = 0; i < Ldjam49.ghosts.Length; ++i) {
                if (Ldjam49.ghosts[i].enemyType == EnemyType.orange) {
                    Ldjam49.ghosts[i].isActive = true;
                    Ldjam49.ghosts[i].body.BodyType = BodyType.Dynamic;
                    Ldjam49.ghosts[i].body.Restitution = 0.9f;
                    Ldjam49.ghosts[i].body.CollidesWith = Category.All;
                    Ldjam49.ghosts[i].body.CollisionCategories = Category.Cat3;
                    orangeJumpDelay = new Delay(1f);
                    break;
                }
            }
        }

        public static void TransformBlueEnemy() {
            changeBlueEnemy = true;
            for (int i = 0; i < Ldjam49.ghosts.Length; ++i) {
                if (Ldjam49.ghosts[i].enemyType == EnemyType.blue) {
                    Ldjam49.ghosts[i].isActive = true;
                    if (Ldjam49.ghosts[i].body.Position.X < Ldjam49.roomWidth/2) {
                        Ldjam49.ghosts[i].direction = Ldjam49.Direction.Right;
                    } else {
                        Ldjam49.ghosts[i].direction = Ldjam49.Direction.Left;
                    }
                    Ldjam49.ghosts[i].body.GravityScale = 0;
                    Ldjam49.ghosts[i].body.BodyType = BodyType.Static;
                    Ldjam49.ghosts[i].body.FixedRotation = true;
                    Ldjam49.ghosts[i].body.Rotation = 0;
                    Ldjam49.ghosts[i].body.CollidesWith = Category.None;
                    break;
                }
            }
        }

        public static void Init() {
            Ldjam49.ghosts = new Ghost[] {
                new Ghost(EnemyType.red, 3, 1, 1) { target = Ldjam49.Direction.Right, direction = Ldjam49.Direction.Right, ia = IA.follow },
                new Ghost(EnemyType.blue, 9, 1, 5) { target = Ldjam49.Direction.Left, direction = Ldjam49.Direction.Left, ia = IA.flee },
                new Ghost(EnemyType.pink, 1, 10, 1) { speed = 45, target = Ldjam49.Direction.Right, direction = Ldjam49.Direction.Right, ia = IA.random },
                new Ghost(EnemyType.orange, 11, 10, 3) { speed = 35, target = Ldjam49.Direction.Left, direction = Ldjam49.Direction.Left, ia = IA.random }, };
        }

        public enum EnemyType {
            red, blue, orange, pink
        }
        public enum IA {
            follow, random, flee
        }

        public void Update(float dt) {
            currentTileX = (int)(body.Position.X + Ldjam49.HALF_TILE.Y) / Ldjam49.TILE_SIZE;
            currentTileY = (int)(body.Position.Y + Ldjam49.HALF_TILE.Y) / Ldjam49.TILE_SIZE;

            if (Ldjam49.isGameOver || Ldjam49.isWinning || !Ldjam49.gameStartsDelay.isTrigger) { return; }

            if (body.Position.X < -Ldjam49.HALF_TILE.X) body.Position = body.Position.ChangeX(Ldjam49.roomWidth - Ldjam49.HALF_TILE.X - radius);
            if (body.Position.X > Ldjam49.roomWidth - Ldjam49.HALF_TILE.X) body.Position = body.Position.ChangeX(-Ldjam49.HALF_TILE.X + radius);
            if (body.Position.Y < -Ldjam49.HALF_TILE.Y) body.Position = body.Position.ChangeY(Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y - radius);
            if (body.Position.Y > Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y) body.Position = body.Position.ChangeY(-Ldjam49.HALF_TILE.Y + radius);

            switch (enemyType) {
                case EnemyType.blue:
                    if (!changeBlueEnemy) break;
                    float movement = speed + blueEnemyInvaderSpeed;
                    if (direction == Ldjam49.Direction.Left) {
                        movement = -movement;
                    }
                    body.Position = new Vector2(body.Position.X + movement * dt, body.Position.Y);
                    float blueInvaderOffset = 50, blueInvaderStep = 20;
                    if (body.Position.X > Ldjam49.roomWidth - blueInvaderOffset - Ldjam49.HALF_TILE.X) {
                        direction = Ldjam49.Direction.Left;
                        body.Position = new Vector2(body.Position.X, body.Position.Y + blueInvaderStep);
                    } else if (body.Position.X < blueInvaderOffset - Ldjam49.HALF_TILE.X) {
                        direction = Ldjam49.Direction.Right;
                        body.Position = new Vector2(body.Position.X, body.Position.Y + blueInvaderStep);
                    }
                    return;
                case EnemyType.orange:
                    if (!changeOrangeEnemy) break;
                    orangeJumpDelay.Update(dt);
                    if (orangeJumpDelay.isTrigger) {
                        orangeJumpDelay.Reset();
                        float forceValue = 100000;
                        Vector2 force = Ldjam49.GetRandomVector(forceValue);
                        force.Y = -Math.Abs(force.Y);
                        body.ApplyLinearImpulse(force);
                        body.ApplyAngularImpulse((float)random.NextDouble() * forceValue * 2 - forceValue);
                    }
                    return;
                case EnemyType.pink:
                    if (!changePinkEnemy) break;
                    octorockAnim.Update(dt);
                    octorockChangePhase.Update(dt);
                    if (octorockChangePhase.isTrigger) {
                        octorockIsWalking = !octorockIsWalking;
                        if (!octorockIsWalking) {
                            octorockCanShoot = true;
                        } else {
                            if (direction == Ldjam49.Direction.Left) direction = Ldjam49.Direction.Right;
                            else if (direction == Ldjam49.Direction.Right) direction = Ldjam49.Direction.Left;
                        }
                        octorockChangePhase.Reset();
                    }

                    if (octorockIsWalking) {
                        body.ApplyLinearImpulse(new Vector2(octorockSpeed * dt, 0) * (direction == Ldjam49.Direction.Left ? -1 : 1));
                        body.Awake = true;
                    } else {
                        if (octorockChangePhase.handler < octorockChangePhase.timer / 2 && octorockCanShoot) { //FEUERN
                            Vector2 bulletPosition = body.Position + new Vector2((direction == Ldjam49.Direction.Right) ? radius + Bullet.radius : - radius - Bullet.radius, 0);
                            Vector2 bulletVelocity = new Vector2((direction == Ldjam49.Direction.Right) ? Bullet.speed: - Bullet.speed, 0);
                            Ldjam49.bullets.Add(new Bullet(bulletPosition, bulletVelocity));
                            octorockCanShoot = false;
                        }
                    }

                    if (direction == Ldjam49.Direction.Left) octorockAnim.spriteEffects = SpriteEffects.None;
                    else if (direction == Ldjam49.Direction.Right) octorockAnim.spriteEffects = SpriteEffects.FlipHorizontally;
                    return;
            }
            if (!isActive) return;
            bool canChangePosition = true;

            goLeft.Update(dt);
            goRight.Update(dt);
            goDown.Update(dt);
            goUp.Update(dt);

            changeDirectionDelay.Update(dt);

            if (changeDirectionDelay.isTrigger) {
                IA iaVal;
                if (nextDirectionChangeIsRandom) {
                    iaVal = IA.random;
                    nextDirectionChangeIsRandom = false;
                } else {
                    iaVal = ia;
                }
                float deltaX, deltaY;
                switch (iaVal) {
                    case IA.follow:
                        deltaX = Player.body.Position.X - body.Position.X;
                        deltaY = Player.body.Position.Y - body.Position.Y;
                        if (Math.Abs(deltaX) < Math.Abs(deltaY)) {
                            if (deltaY < 0) target = Ldjam49.Direction.Up;
                            else target = Ldjam49.Direction.Down;
                        } else {
                            if (deltaX < 0) target = Ldjam49.Direction.Left;
                            else target = Ldjam49.Direction.Right;
                        }
                        break;
                    case IA.flee:
                        deltaX = Player.body.Position.X - body.Position.X;
                        deltaY = Player.body.Position.Y - body.Position.Y;
                        if (Math.Abs(deltaX) < Math.Abs(deltaY)) {
                            if (deltaY < 0) target = Ldjam49.Direction.Down;
                            else target = Ldjam49.Direction.Up;
                        } else {
                            if (deltaX < 0) target = Ldjam49.Direction.Right;
                            else target = Ldjam49.Direction.Left;
                        }
                        break;
                    case IA.random: target = (Ldjam49.Direction)random.Next(0, 3); break;
                }
                changeDirectionDelay.Reset();
            }

            blockingTileX = -1; blockingTileY = -1;
            for (int y = 0; y < Ldjam49.tiles.Length; ++y) {
                if (!canChangePosition) break;
                for (int x = 0; x < Ldjam49.tiles[y].Length; ++x) {
                    if (!canChangePosition) break;
                    if (Ldjam49.tiles[y][x] == 0) continue;
                    float xPos = Ldjam49.TILE_SIZE * x;
                    float yPos = Ldjam49.TILE_SIZE * y;
                    float roomOfManeuver = 0.2f;
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
            }

            Vector2 GhostMovement = Vector2.Zero;
            switch (direction) {
                case Ldjam49.Direction.Down: GhostMovement.Y = speed; break;
                case Ldjam49.Direction.Up: GhostMovement.Y = -speed; break;
                case Ldjam49.Direction.Left: GhostMovement.X = -speed; break;
                case Ldjam49.Direction.Right: GhostMovement.X = speed; break;
            }

            body.Position += GhostMovement * dt;

            float offset = 17f;
            bool isGhostBlocked = false;
            for (int y = 0; y < Ldjam49.tiles.Length; ++y) {
                for (int x = 0; x < Ldjam49.tiles[y].Length; ++x) {
                    if (Ldjam49.tiles[y][x] == 0) continue;
                    float xPos = Ldjam49.TILE_SIZE * x;
                    float yPos = Ldjam49.TILE_SIZE * y;
                    switch (direction) {
                        case Ldjam49.Direction.Down:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2, body.Position.Y - radius / 2 + offset, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isGhostBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Up:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2, body.Position.Y - radius / 2 - offset, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isGhostBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Left:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2 - offset, body.Position.Y - radius / 2, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isGhostBlocked = true;
                            }
                            break;
                        case Ldjam49.Direction.Right:
                            if (Tools.Rect2Rect(body.Position.X - radius / 2 + offset, body.Position.Y - radius / 2, radius, radius, xPos - Ldjam49.TILE_SIZE / 2, yPos - Ldjam49.TILE_SIZE / 2, Ldjam49.TILE_SIZE, Ldjam49.TILE_SIZE)) {
                                isGhostBlocked = true;
                            }
                            break;
                    }
                }
            }
            if (isGhostBlocked) {
                body.Position -= GhostMovement * dt;
                changeDirectionDelay.handler = 0;
                nextDirectionChangeIsRandom = true;
            }
        }

        public void Draw() {
            if (blockingTileX != -1 && blockingTileY != -1) {
                //spriteBatch.Draw(DebugTileTexture, new Vector2(blockingTileX, blockingTileY) - HALF_TILE, null, Color.White *.5f, Ghost.body.Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            switch (enemyType) {
                case EnemyType.blue:
                    if (changeBlueEnemy) {
                        if (Vector2.Distance(Player.body.Position, body.Position) < 100 || Ldjam49.isGameOver) {
                            DrawWithTextureString("invader_blue_excited");
                        } else {
                            DrawWithTextureString("invader_blue_normal");
                        }
                        return;
                    }
                    break;
                case EnemyType.orange:
                    if (changeOrangeEnemy) {
                        DrawWithTextureString("flappy_bird_of_death", 1.3f);
                        return;
                    }
                    break;
                case EnemyType.pink:
                    if (changePinkEnemy) {
                        octorockAnim.Draw(Ldjam49.spriteBatch, body.Position);
                        return;
                    }
                    break;
            }

            if (isActive) {
                switch (direction) {
                    case Ldjam49.Direction.Down: goDown.Draw(Ldjam49.spriteBatch, body.Position); break;
                    case Ldjam49.Direction.Left: goLeft.Draw(Ldjam49.spriteBatch, body.Position); break;
                    case Ldjam49.Direction.Right: goRight.Draw(Ldjam49.spriteBatch, body.Position); break;
                    case Ldjam49.Direction.Up: goUp.Draw(Ldjam49.spriteBatch, body.Position); break;
                }
            } else {
                DrawWithTextureString("ghost_scared");
            }
        }

        public void DrawWithTextureString(string textureString, float scale = 1) {
            Ldjam49.spriteBatch.Draw(Ldjam49.textures[textureString], body.Position, null, Color.White, body.Rotation, new Vector2(radius), scale, SpriteEffects.None, 0f);
        }

        public static void LoadContent() {

        }
    }
}
