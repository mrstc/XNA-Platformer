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
    public class Game1 : Microsoft.Xna.Framework.Game {
        #region NewTypes
        enum GameState { menu, setup, game, editor };
        delegate void Event ();
        // working; seems like complete... maybe only groups binding add in the future
        class KeyboardManager {
            SortedSet< Keys > pressedKeys = new SortedSet<Keys> ();
            Dictionary < PressType, Dictionary< Keys, Event > > binds = new Dictionary<PressType, Dictionary<Keys, Event>>();

            public enum PressType { once, ever };

            public KeyboardManager () {
                foreach ( PressType t in Enum.GetValues ( typeof ( PressType ) ) ) {
                    binds.Add ( t, new Dictionary<Keys, Event> () );
                }
            }

            public void checkKeys () {
                SortedSet<Keys> pressedKeys_t = new SortedSet<Keys> ( pressedKeys );
                foreach ( Keys k in pressedKeys_t ) {
                    if ( Keyboard.GetState ().IsKeyUp ( k ) ) {
                        pressedKeys.Remove ( k );
                    }
                }
                Event res = null;
                foreach ( var el in binds[ PressType.once ] ) {
                    if ( Keyboard.GetState ().IsKeyDown ( el.Key ) && !pressedKeys.Contains ( el.Key ) ) {
                        pressedKeys.Add ( el.Key );
                        res += el.Value;
                    }
                }
                foreach ( var el in binds[ PressType.ever ] ) {
                    if ( Keyboard.GetState ().IsKeyDown ( el.Key ) ) {
                        pressedKeys.Add ( el.Key );
                        res += el.Value;
                    }
                }
                if ( res != null ) {
                    res ();
                }
            }

            public void bind ( Keys k, PressType t, Event e ) {
                binds[t].Add ( k, e );
            }

            public void unbindKey ( Keys k ) {
                foreach ( PressType t in Enum.GetValues ( typeof ( PressType ) ) ) {
                    if ( binds[t].ContainsKey ( k ) ) {
                        binds[t].Remove ( k );
                    }
                }

            }

            public bool this [ Keys k, PressType t = PressType.once ] {
                get {
                    bool res = false;
                    if ( Keyboard.GetState ().IsKeyDown ( k ) ) {
                        if ( !pressedKeys.Contains ( k ) || t == PressType.ever ) {
                            res = true;
                            pressedKeys.Add ( k );
                        }
                    }
                    return res;
                }
            }
        }
        class Menu {
            Dictionary < String, Event > menu = new Dictionary<string,Event>();
            int curr = 0;
            int count = 0;

            public Menu () {
            }

            public void Add ( String s, Event e ) {
                menu.Add ( s, e );
                count++;
            }

            public void drawMenu ( SpriteBatch spBatch, SpriteFont fnt ) {
                int i = 0;
                spBatch.Begin ();
                foreach ( var el in menu ) {
                    spBatch.DrawString ( fnt, el.Key, new Vector2 ( 50, 50*(i + 1) ), (i == curr ? Color.Black : Color.White) );
                    i++;
                }
                spBatch.End ();
            }

            public void down () {
                curr = (curr + 1) % count;
            }

            public void up () {
                if ( --curr < 0 ) {
                    curr += count;
                }
            }

            public void enter () {
                int i = 0;
                foreach ( var el in menu ) {
                    if ( i == curr ) {
                        el.Value ();
                    }
                    i++;
                }
            }
        }
        #endregion


        #region Properties

        GameState gState;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        KeyboardManager kbManager;
        Menu menu;
        Menu setup;

        #endregion


        #region InitializesAndDeinitializes

        public Game1 () {
            kbManager = new KeyboardManager ();
            menu = new Menu ();
            menu.Add ( "1) Start Game", () => changeState ( GameState.game ) );
            menu.Add ( "2) Setup", () => changeState ( GameState.setup ) );
            menu.Add ( "3) Exit Game", () => this.Exit () );

            setup = new Menu ();
            setup.Add ( "1) Toggle Fullscreen", () => graphics.ToggleFullScreen () );
            setup.Add ( "2) Back", () => changeState ( GameState.menu ) );

            graphics = new GraphicsDeviceManager ( this );
            Content.RootDirectory = "Content";
        }

        void changeState ( GameState state ) {
            kbManager.unbindKey ( Keys.Down );
            kbManager.unbindKey ( Keys.Up );
            kbManager.unbindKey ( Keys.Enter );

            gState = state;
            switch ( state ) {
                case GameState.setup:
                    kbManager.bind ( Keys.Down, KeyboardManager.PressType.once, setup.down );
                    kbManager.bind ( Keys.Up, KeyboardManager.PressType.once, setup.up );
                    kbManager.bind ( Keys.Enter, KeyboardManager.PressType.once, setup.enter );
                    break;
                case GameState.menu:
                    kbManager.bind ( Keys.Down, KeyboardManager.PressType.once, menu.down );
                    kbManager.bind ( Keys.Up, KeyboardManager.PressType.once, menu.up );
                    kbManager.bind ( Keys.Enter, KeyboardManager.PressType.once, menu.enter );
                    break;
                case GameState.editor:
                    break;
                case GameState.game:
                    break;
            }
        }

        protected override void Initialize () {
            changeState ( GameState.menu );
            base.Initialize ();
        }

        protected override void LoadContent () {
            spriteBatch = new SpriteBatch ( GraphicsDevice );

            font = Content.Load<SpriteFont> ( "Font" );
        }

        protected override void UnloadContent () {
        }

        #endregion


        #region UpdateAndDraw

        protected override void Update ( GameTime gameTime ) {
            kbManager.checkKeys ();

            switch ( gState ) {
                case GameState.menu:
                    break;
                default: {
                        if ( kbManager [ Keys.Escape ] ) {
                            changeState ( GameState.menu );
                        }
                        if ( kbManager [ Keys.Tab ] ) {
                            changeState ( (gState == GameState.editor ? GameState.game : GameState.editor) );
                        }
                        break;
                    }
            }


            base.Update ( gameTime );
        }

        protected override void Draw ( GameTime gameTime ) {
            GraphicsDevice.Clear ( Color.CornflowerBlue );

            switch ( gState ) {
                case GameState.setup:
                    setup.drawMenu ( spriteBatch, font );
                    break;
                case GameState.menu:
                    menu.drawMenu ( spriteBatch, font );
                    break;
                case GameState.editor:
                    spriteBatch.Begin ();
                    spriteBatch.DrawString ( font, gState.ToString (), new Vector2 ( 30 ), Color.Black );
                    spriteBatch.End ();
                    break;
                case GameState.game:
                    spriteBatch.Begin ();
                    spriteBatch.DrawString ( font, gState.ToString (), new Vector2 ( 30 ), Color.Black );
                    spriteBatch.End ();
                    break;
            }

            base.Draw ( gameTime );
        }

        #endregion
    }
}
