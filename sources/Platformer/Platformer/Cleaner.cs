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


namespace Platformer
{
    public class Cleaner : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public Cleaner ( Game game )
            : base ( game ) {
        }
        public override void Initialize () {

            base.Initialize ();
        }
        public override void Update ( GameTime gameTime ) {

            base.Update ( gameTime );
        }

        public override void Draw ( GameTime gameTime ) {
            //Game.GraphicsDevice.Clear ( Color.CornflowerBlue );
            base.Draw ( gameTime );
        }
    }
}
