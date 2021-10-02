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
        public float speed = 20, radius = 20;
        public Ldjam49.Direction direction, target;
        public Body body;
        public int blockingTileX, blockingTileY;
        public Texture2D ghostTexture;
        public Delay changeDirectionDelay;
        public Random random;
        public Ghost (EnemyType enemyType, int xTile, int yTile) {
            body = BodyFactory.CreateCircle(Ldjam49.world, radius, 1f);
            body.Position = new Vector2(Ldjam49.TILE_SIZE * xTile, Ldjam49.TILE_SIZE * yTile);
            body.BodyType = BodyType.Dynamic;
            body.GravityScale = 0;
            body.FixedRotation = true;
            changeDirectionDelay = new Delay(5f);
            random = new Random();

            target = (Ldjam49.Direction)random.Next(0, 3);

            body.CollidesWith = Category.Cat1;
            body.CollisionCategories = Category.Cat3;

            ghostTexture = Ldjam49.textures["ghost_" + enemyType.ToString()];
        }

        public enum EnemyType {
            red, blue, orange, pink
        }

        public void Update(float dt) {
            if (Ldjam49.isPhysicsActivated) return;
            bool canChangePosition = true;

            if (ghostTexture.Name.Contains("red")) {
                target = direction;
                if (Ldjam49.kState.IsKeyDown(Keys.Left) || Ldjam49.kState.IsKeyDown(Keys.A)) target = Ldjam49.Direction.Left;
                if (Ldjam49.kState.IsKeyDown(Keys.Right) || Ldjam49.kState.IsKeyDown(Keys.D)) target = Ldjam49.Direction.Right;
                if (Ldjam49.kState.IsKeyDown(Keys.Up) || Ldjam49.kState.IsKeyDown(Keys.W)) target = Ldjam49.Direction.Up;
                if (Ldjam49.kState.IsKeyDown(Keys.Down) || Ldjam49.kState.IsKeyDown(Keys.S)) target = Ldjam49.Direction.Down;
            } else {
                changeDirectionDelay.Update(dt);

                if (changeDirectionDelay.isTrigger) {
                    target = (Ldjam49.Direction)random.Next(0, 3);
                    changeDirectionDelay.Reset();
                }
            }

            blockingTileX = -1; blockingTileY = -1;
            for (int y = 0; y < Ldjam49.tiles.Length; ++y) {
                if (!canChangePosition) break;
                for (int x = 0; x < Ldjam49.tiles[y].Length; ++x) {
                    if (!canChangePosition) break;
                    if (Ldjam49.tiles[y][x] == 0) continue;
                    float xPos = Ldjam49.TILE_SIZE * x;
                    float yPos = Ldjam49.TILE_SIZE * y;
                    float someVal = 1.1f;
                    switch (target) {
                        case Ldjam49.Direction.Down:
                            if (yPos > body.Position.Y + radius && yPos < body.Position.Y + radius + 1.5f * Ldjam49.TILE_SIZE && Math.Abs(xPos - body.Position.X) < Ldjam49.TILE_SIZE / someVal) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Up:
                            if (yPos < body.Position.Y - radius && yPos > body.Position.Y - radius - 1.5f * Ldjam49.TILE_SIZE && Math.Abs(xPos - body.Position.X) < Ldjam49.TILE_SIZE / someVal) {
                                blockingTileX = (int)xPos; blockingTileY = (int)yPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Left:
                            if (xPos < body.Position.X - radius && xPos > body.Position.X - radius - 1.5f * Ldjam49.TILE_SIZE && Math.Abs(yPos - body.Position.Y) < Ldjam49.TILE_SIZE / someVal) {
                                blockingTileY = (int)yPos; blockingTileX = (int)xPos;
                                canChangePosition = false;
                            }
                            break;
                        case Ldjam49.Direction.Right:
                            if (xPos > body.Position.X + radius && xPos < body.Position.X + radius + 1.5f * Ldjam49.TILE_SIZE && Math.Abs(yPos - body.Position.Y) < Ldjam49.TILE_SIZE / someVal) {
                                blockingTileY = (int)yPos; blockingTileX = (int)xPos;
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
            body.Awake = true;

            float offset = 15;
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
            }
        }

        public void Draw() {
            if (blockingTileX != -1 && blockingTileY != -1) {
                //spriteBatch.Draw(DebugTileTexture, new Vector2(blockingTileX, blockingTileY) - HALF_TILE, null, Color.White *.5f, Ghost.body.Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            Ldjam49.spriteBatch.Draw(ghostTexture, body.Position, null, Color.White, body.Rotation, new Vector2(radius), 1f, SpriteEffects.None, 0f);
        }

        public static void LoadContent() {

        }
    }
}
