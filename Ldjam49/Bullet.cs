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
    public class Bullet {
        public static float speed = 100;
        public AnimatedSprite anim;
        public Vector2 position, velocity;
        public static float radius = 8;

        public Bullet(Vector2 pos, Vector2 vel) {
            position = pos;
            velocity = vel;

            //radius = anim.frameWth / 2; WILL ALWAYS BE = 8    HOPEFULLY...

            anim = new AnimatedSprite(Ldjam49.animations["fireball_f4w16h16c4r1"], 7).animParam(isLooping: true);
            anim.origin = new Vector2(radius);
        }

        public void Update(float dt) {
            position += velocity * dt;
            anim.Update(dt);

            if (position.X < -Ldjam49.HALF_TILE.X) position = position.ChangeX(Ldjam49.roomWidth - Ldjam49.HALF_TILE.X - radius);
            if (position.X > Ldjam49.roomWidth - Ldjam49.HALF_TILE.X) position = position.ChangeX(-Ldjam49.HALF_TILE.X + radius);
            if (position.Y < -Ldjam49.HALF_TILE.Y) position = position.ChangeY(Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y - radius);
            if (position.Y > Ldjam49.roomHeight - Ldjam49.HALF_TILE.Y) position = position.ChangeY(-Ldjam49.HALF_TILE.Y + radius);
        }

        public void Draw() {
            anim.Draw(Ldjam49.spriteBatch, position);
        }
    }
}
