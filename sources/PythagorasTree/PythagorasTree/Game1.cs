using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace PythagorasTree {
    public class Game1 : Microsoft.Xna.Framework.Game {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D square;

        public Game1 () {
            graphics = new GraphicsDeviceManager ( this );
            Content.RootDirectory = "Content";
        }

        protected override void Initialize () {

            base.Initialize ();
        }
        protected override void LoadContent () {
            spriteBatch = new SpriteBatch ( GraphicsDevice );

            square = new Texture2D ( GraphicsDevice, 1, 1 );
            Color[] colors = new Color[ 1 ];
            colors[ 0 ] = Color.White;
            square.SetData<Color> ( colors );
        }

        protected override void UnloadContent () {
        }

        protected override void Update ( GameTime gameTime ) {
            if ( GamePad.GetState ( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed )
                this.Exit ();

            base.Update ( gameTime );
        }

        float windAngle = (float) Math.PI / 4;

        void drawTreeRecurion ( Vector2 pos, float size, Color color, int ttl, float angle ) {
            spriteBatch.Draw ( square, new Rectangle ( (int) pos.X, (int) pos.Y, (int) size, (int) size ), null, (ttl == 1 ? Color.DarkGreen : color), angle, new Vector2 ( 0.5f, 0.5f ), SpriteEffects.None, 0 );
            if ( --ttl > 0 ) {
                float len = (float) Math.Sqrt ( ( size/2 )*( size/2 ) + ( 3*size/4 )*( 3*size/4 ) );
                //Vector2 shift1 = new Vector2 ( len * (float) Math.Cos ( Math.PI / 3 - angle ), len * (float) Math.Sin ( Math.PI / 3 - angle ) );
                Vector2 shift1 = new Vector2 ( len * (float) Math.Cos ( Math.PI / 3 - angle ), len * (float) Math.Sin ( Math.PI / 3 - angle ) );
                Vector2 shift2 = new Vector2 ( len * (float) Math.Cos ( 2 * Math.PI / 3 - angle ), len * (float) Math.Sin ( 2*Math.PI / 3 - angle ) );
                //pos -= shift;
                //size *=  (float) Math.Cos ( Math.PI / 4 );
                //angle += (float) Math.PI / 4;
                color.R -= 10;
                drawTreeRecurion ( pos - shift1, size * (float) Math.Cos ( Math.PI / 4 ), color, ttl, angle + (float) Math.PI / 4 );
                drawTreeRecurion ( pos - shift2, size * (float) Math.Cos ( Math.PI / 4 ), color, ttl, angle - (float) Math.PI / 4 );
            }
        }

        protected override void Draw ( GameTime gameTime ) {
            GraphicsDevice.Clear ( Color.CornflowerBlue );

            spriteBatch.Begin ();
            drawTreeRecurion ( new Vector2 ( 350, 300 ), 100, Color.Yellow, 8, 0 );
            spriteBatch.End ();

            base.Draw ( gameTime );
        }
    }
}
