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


namespace Platformer {
    public class GameConsole : Microsoft.Xna.Framework.DrawableGameComponent {
        public delegate string DebugDrawer ();

        List < DebugDrawer > debugDrawers = new List<DebugDrawer> ();
        SpriteBatch spriteBatch;
        Texture2D pix;
        SpriteFont font;

        public GameConsole ( Game game ) : base ( game ) {
        }

        public override void Initialize () {
            spriteBatch = new SpriteBatch ( Game.GraphicsDevice );

            base.Initialize ();
        }

        protected override void LoadContent () {
            font = Game.Content.Load<SpriteFont> ( "DebugFont" );

            pix = new Texture2D ( Game.GraphicsDevice, 1, 1 );

            Color[] col = new Color[ 1 ];
            col[ 0 ] = Color.Black;

            pix.SetData<Color> ( col );

            base.LoadContent ();
        }

        public override void Update ( GameTime gameTime ) { base.Update ( gameTime ); }

        public void AddDrawer ( DebugDrawer d ) {
            if ( d != null ) {
                debugDrawers.Add ( d );
            }
        }

        public override void Draw ( GameTime gameTime ) {
            string res = "";

            foreach ( DebugDrawer d in debugDrawers ) {
                res += d () + "\n";
            }

            spriteBatch.Begin ();
            spriteBatch.Draw ( pix, new Rectangle ( 20, 20, 70, 30 ), new Color ( 255, 255, 255, 125 ) );
            spriteBatch.DrawString ( font, res, new Vector2 ( 23, 23 ), Color.White );
            spriteBatch.End ();

            base.Draw ( gameTime );
        }
    }
}
