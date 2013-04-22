using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace Platformer {
    public class KeyboardManager : Microsoft.Xna.Framework.GameComponent {
        public delegate void KeyPressed ( bool isRealesed );
        Dictionary < Keys, KeyPressed > keyDict = new Dictionary < Keys, KeyPressed > ();
        HashSet < Keys > keysHash = new HashSet<Keys> ();

        public KeyboardManager ( Game game ) : base ( game ) {
            foreach ( Keys k in Enum.GetValues ( typeof ( Keys ) ) ) {
                keyDict.Add ( k, null );
            }
        }

        public override void Initialize () {

            base.Initialize ();
        }

        public override void Update ( GameTime gameTime ) {
            KeyboardState kbState = Keyboard.GetState ();
            try {
                foreach ( var k in keyDict ) {
                    if ( !keysHash.Contains ( k.Key ) && kbState.IsKeyDown ( k.Key ) ) {
                        if ( k.Value != null ) {
                            k.Value ( false );
                        }
                        keysHash.Add ( k.Key );
                    } else if ( keysHash.Contains ( k.Key ) && kbState.IsKeyUp ( k.Key ) ) {
                        if ( k.Value != null ) {
                            k.Value ( true );
                        }
                        keysHash.Remove ( k.Key );
                    }
                }
            } catch ( Exception ignore ) {
                // KeyPressed event could change a KeyDict that throws an exception.
                // In order to avoid this we just ignore this exception.
            }

            base.Update ( gameTime );
        }

        public bool IsKeyDown ( Keys k ) {
            return keysHash.Contains ( k );
        }

        public void UnbindAll () {
            foreach ( Keys k in Enum.GetValues ( typeof ( Keys ) ) ) {
                keyDict[ k ] = null;
            }
        }

        public KeyPressed this[ Keys k ] {
            get {
                return keyDict[ k ];
            } set {
                keyDict[ k ] = value;
            }
        }
    }
}
