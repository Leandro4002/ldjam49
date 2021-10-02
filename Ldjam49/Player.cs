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
    public static class Player {
        public static float speed = 50, radius = 15;
        public static Ldjam49.Direction direction, target;
        public static Body body;
        public static int blockingTileX, blockingTileY;
        public static void Init() {
            direction = Ldjam49.Direction.Right;
            body = BodyFactory.CreateCircle(Ldjam49.world, radius, 1f);
            body.Position = new Vector2(Ldjam49.TILE_SIZE * 6, Ldjam49.TILE_SIZE * 5);
            body.BodyType = BodyType.Dynamic;
            body.GravityScale = 0;
            body.FixedRotation = true;

            body.CollisionCategories = Category.Cat2;
            body.CollidesWith = Category.Cat1;
        }

        public static void Update(float dt) {
            if (Ldjam49.isPhysicsActivated) {

            } else {
                UpdateMovementForPacmanLike(dt);
            }
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
                    float someVal = 1.1f;
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
                Debug.WriteLine("DIRECTION CHANGED");
            }

            Vector2 playerMovement = Vector2.Zero;
            switch (direction) {
                case Ldjam49.Direction.Down: playerMovement.Y = speed; break;
                case Ldjam49.Direction.Up: playerMovement.Y = -speed; break;
                case Ldjam49.Direction.Left: playerMovement.X = -speed; break;
                case Ldjam49.Direction.Right: playerMovement.X = speed; break;
            }

            body.Position += playerMovement * dt;
            body.Awake = true;

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

        public static void Draw() {
            if (blockingTileX != -1 && blockingTileY != -1) {
                //spriteBatch.Draw(DebugTileTexture, new Vector2(blockingTileX, blockingTileY) - HALF_TILE, null, Color.White *.5f, Player.body.Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            Ldjam49.spriteBatch.Draw(Ldjam49.textures["pacman"], body.Position, null, Color.White, body.Rotation, new Vector2(radius), 1f, SpriteEffects.None, 0f);
        }

        public static void LoadContent() {

        }
    }
}
