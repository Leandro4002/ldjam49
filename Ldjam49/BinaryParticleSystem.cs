using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameUtilities;

namespace ldjam49Namespace {
    public class BinaryParticleSystem : ParticleSystem {
        public BinaryParticleSystem(SpriteBatch spriteBatch, Texture2D texture, int particlesFps) : base(spriteBatch, texture, particlesFps) {
            
        }

        protected override void InitializeConstants() {
            minScale = 0.5f; maxScale = 0.5f;
            minLifeDuration = 0.5f; maxLifeDuration = 1.5f;
            minAcceleration = 0; maxAcceleration = 0;
            minInitialSpeed = -40; maxInitialSpeed = 40;
            minRotationSpeed = 0; maxRotationSpeed = 0;
            minInitialRotation = 0; maxInitialRotation = 0;
            minNumParticles = 10; maxNumParticles = 10;
            TotalNumberOfParticles = 50;
            emitAngle = 0.3f;
            emitDirection = 0.785f;
            ParticlesColor = new Color(255, 255, 255, 255);
            fadeOutMethod = ParticleSystem.FadeOutMethod.Linear;
            fadePoints = new Vector2[] { new Vector2(0, 1), new Vector2(0.82f, 1), new Vector2(1, 0.01f) };
            deathTrigger = ParticleSystem.DeathTrigger.onEndOfLifeDuration;
        }

        protected override void InitializeParticle(Particle p, Vector2 pos) {
            base.InitializeParticle(p, pos);
            p.currentFrame = random.Next() % 2 == 0 ? 1 : 0;
            p.CalculateCurrentFramePosition();
            p.position.X += random.Next(-30, 30);
        }
    }
}