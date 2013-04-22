using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using FarseerPhysics;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Controllers;

using Utils;

namespace Platformer {
    public class Game1 : Microsoft.Xna.Framework.Game {
        #region NewTypes
        enum GameState { menu, setup, game, editor, pifagorTreeDemo };

        public delegate void Event ();
        public class Menu {
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
        static public KeyboardManager kbManager;
        static public GameConsole console;

        PifagorasTree pfTree;
        GameWorld game;

        GameState gState;
        Menu menu;
        Menu setup;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        float fpsCounter = 0;
        #endregion


        #region InitializesAndDeinitializes

        public Game1 () {
            kbManager = new KeyboardManager ( this );
            console = new GameConsole ( this );
            menu = new Menu ();
            menu.Add ( "1) Start Game", () => changeState ( GameState.game ) );
            menu.Add ( "2) Pifagor's Tree Demo", () => changeState ( GameState.pifagorTreeDemo ) );
            menu.Add ( "3) Setup", () => changeState ( GameState.setup ) );
            menu.Add ( "4) Exit Game", () => this.Exit () );

            setup = new Menu ();
            setup.Add ( "1) Toggle Fullscreen", () => graphics.ToggleFullScreen () );
            setup.Add ( "2) Back", () => changeState ( GameState.menu ) );

            graphics = new GraphicsDeviceManager ( this );
            
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            graphics.ApplyChanges();


            game = new GameWorld ( this );

            Content.RootDirectory = "Content";

            pfTree = new PifagorasTree ( this );
        }

        void changeState ( GameState state ) {
            kbManager.UnbindAll ();

            game.Enabled = false;

            gState = state;
            switch ( state ) {
                case GameState.setup:
                    kbManager[ Keys.Down ] = ( bool b ) => { if (!b) setup.down (); };
                    kbManager[ Keys.Up ] = ( bool b ) => { if (!b) setup.up (); };
                    kbManager[ Keys.Enter ] = ( bool b ) => { if (!b) setup.enter (); };
                    break;
                case GameState.menu:
                    kbManager[ Keys.Down ] = ( bool b ) => { if (!b) menu.down (); };
                    kbManager[ Keys.Up ] = ( bool b ) => { if (!b) menu.up (); };
                    kbManager[ Keys.Enter ] = ( bool b ) => { if (!b) menu.enter (); };
                    break;
                case GameState.editor:
                    break;
                case GameState.game:
                    game.Enabled = true;
                    break;
            }
        }

        protected override void Initialize () {
            Components.Add ( game );
            Services.AddService ( game.GetType(), game );

            Components.Add ( kbManager );
            Services.AddService ( kbManager.GetType (), kbManager );

            Components.Add ( console );
            Services.AddService ( console.GetType (), console );

            console.AddDrawer ( fps );

            pfTree.Initialize ();

            changeState ( GameState.menu );

            game.DrawOrder = 2;
            console.DrawOrder = 100500;

            base.Initialize ();
        }

        protected override void LoadContent () {
            spriteBatch = new SpriteBatch ( GraphicsDevice );

            font = Content.Load<SpriteFont> ( "Font" );

            pfTree.publicLoadContent ();
        }

        protected override void UnloadContent () {
        }

        #endregion


        #region UpdateAndDraw

        protected override void Update ( GameTime gameTime ) {
            base.Update ( gameTime );
        }

        protected override void Draw ( GameTime gameTime ) {
            GraphicsDevice.Clear ( Color.CornflowerBlue );

            fpsCounter = 1000 / gameTime.ElapsedGameTime.Milliseconds;

            switch ( gState ) {
                case GameState.setup:
                    setup.drawMenu ( spriteBatch, font );
                    break;
                case GameState.menu:
                    menu.drawMenu ( spriteBatch, font );
                    break;
                case GameState.pifagorTreeDemo:
                    pfTree.Draw ( gameTime );
                    break;
            }

            base.Draw ( gameTime );
        }

        public string fps () {
            return "fps: " + fpsCounter;
        }

        #endregion
    }
}
