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
        class GameWorld {
            World world = new World ( new Vector2 ( 0, 9.8f ) );
            Dictionary<string, Object> bodies = new Dictionary<string, Object> ();

            class Converter {
                static float currentPixelsInMeter;

                static public void SetRation ( float pixelsInMeter ) {
                    currentPixelsInMeter = pixelsInMeter;
                }

                static public float ToSimulate ( float pixels ) {
                    return pixels / currentPixelsInMeter;
                }

                static public float ToPixels ( float meters ) {
                    return meters * currentPixelsInMeter;
                }

                static public Vector2 ToPixelsVec ( Vector2 vector ) {
                    return vector * currentPixelsInMeter;
                }

                static public Vector2 ToSimulateVec ( Vector2 vector ) {
                    return vector / currentPixelsInMeter;
                }
            }

            class Object : Body {
                public struct Frame {
                    public Rectangle spriteRect;
                    public int milliseconds;
                }

                public int h, w;
                Texture2D texture;
                string currentAnimation;
                int currentFrameIndex;
                int msFrameLeft = 0;

                Dictionary < string, List < Frame > > animations = new Dictionary<string,List<Frame>> ();

                public Object ( World world, int width, int height, Vector2 position, Texture2D text, bool isStatic ) :
                  base ( world ) {
                    w = width;
                    h = height;
                    texture = text;
                    Vertices rectangleVertices = PolygonTools.CreateRectangle ( Converter.ToSimulate ( width / 2),
                                                                                Converter.ToSimulate ( height / 2));
                    FarseerPhysics.Collision.Shapes.PolygonShape rectangleShape =
                        new FarseerPhysics.Collision.Shapes.PolygonShape ( rectangleVertices, 0.0f );
                    this.CreateFixture ( rectangleShape, null );

                    if ( isStatic ) {
                        this.BodyType = FarseerPhysics.Dynamics.BodyType.Static;
                        this.IsStatic = true;
                    } else {
                        this.BodyType = FarseerPhysics.Dynamics.BodyType.Dynamic;
                    }
                    this.Position =  Converter.ToSimulateVec ( position );
                    this.Restitution = 0.0f;
                    this.Friction = 0.0f;
                }

                public void InitAnimation ( params Frame[] defalutFrames ) {
                    animations[ currentAnimation = "default" ] = defalutFrames.ToList ();
                }

                public void AddAnimation ( string name, params Frame[] frames ) {
                    animations[ name ] = frames.ToList ();
                    currentFrameIndex = 0;
                    msFrameLeft = frames[ currentFrameIndex ].milliseconds;
                }

                public void PlayAnimation ( string name ) {
                    if ( !animations.ContainsKey ( name ) )
                        throw new Exception ( "Cannot play uninitialised animatio!" );
                    currentAnimation = name;
                    currentFrameIndex = 0;
                    msFrameLeft = animations[ currentAnimation ][ ++currentFrameIndex ].milliseconds;
                }

                /// <summary>
                /// Sets the default animation
                /// </summary>
                public void PauseAnimation () {
                    currentAnimation = "default";
                }

                public void Update ( GameTime gameTime ) {
                    if ( msFrameLeft > 0 ) {
                        msFrameLeft -= gameTime.ElapsedGameTime.Milliseconds;
                    } else {
                        while ( msFrameLeft <= 0 ) {
                            if ( ++currentFrameIndex == animations[ currentAnimation ].Count ) {
                                currentFrameIndex = 0;
                            }
                            msFrameLeft = animations[ currentAnimation ][ currentFrameIndex ].milliseconds + msFrameLeft;
                        }
                    }
                }

                public void Draw ( SpriteBatch batch ) {
                    batch.Draw ( texture,
                        new Rectangle ( (int)Converter.ToPixels ( Position.X ),
                                        (int)Converter.ToPixels ( Position.Y ),
                                        w, h ),
                        animations[ currentAnimation ][ currentFrameIndex ].spriteRect,
                        Color.White,
                        0.0f,
                        new Vector2 ( w/2f, h/2f ),
                        SpriteEffects.None,
                        0 );
                }
            }


            public GameWorld () {
            }

            public void LoadContent ( ContentManager content ) {
                Converter.SetRation ( 64.0f );


                Object b = new Object ( world, 40, 60,
                                        new Vector2 ( 30, 100 ),
                                        content.Load<Texture2D> ( "hero_sprite" ),
                                        false );
                b.InitAnimation ( new Object.Frame {
                    spriteRect = new Rectangle ( 3, 17, 45, 62 ),
                    milliseconds = 100
                }, new Object.Frame {
                    spriteRect = new Rectangle ( 58, 17, 45, 62 ),
                    milliseconds = 100
                }, new Object.Frame {
                    spriteRect = new Rectangle ( 116, 17, 45, 62 ),
                    milliseconds = 100
                } );
                bodies.Add ( "hero", b );


                Texture2D tileset = content.Load<Texture2D> ( "tile_set" );
                b = new Object ( world, 128, 128,
                                        new Vector2 ( 64, 600-64 ),
                                        tileset,
                                        true );

                b.InitAnimation ( new Object.Frame {
                    spriteRect = new Rectangle ( 0, 148, 128, 128 ),
                    milliseconds = 100
                } );
                bodies.Add ( "tile", b );

                b = new Object ( world, 128, 128,
                                         new Vector2 ( 64*3, 600-64 ),
                                         tileset,
                                         true );
                b.InitAnimation ( new Object.Frame {
                    spriteRect = new Rectangle ( 0, 148, 128, 128 ),
                    milliseconds = 100
                } );
                bodies.Add ( "tile2", b );

                b = new Object ( world, 128, 128,
                                        new Vector2 ( 64*7, 600-64 ),
                                        tileset,
                                        true );
                b.InitAnimation ( new Object.Frame {
                    spriteRect = new Rectangle ( 0, 148, 128, 128 ),
                    milliseconds = 100
                } );
                bodies.Add ( "tile3", b );
            }

            Vector2 velocity = new Vector2 ( 0 );
            const float walkSpeed = 2f;

            public void Update ( GameTime gt ) {
                velocity = new Vector2 ( bodies[ "hero" ].LinearVelocity.X, 0 );
                bodies [ "hero" ].ApplyLinearImpulse ( -velocity );
                if ( kbManager [ Keys.A, KeyboardManager.PressType.ever ] && kbManager [ Keys.D, KeyboardManager.PressType.ever ] ) {
                    velocity = new Vector2 ( 0 );
                } else if ( kbManager [ Keys.A, KeyboardManager.PressType.ever ] ) {
                    bodies [ "hero" ].ApplyLinearImpulse ( velocity = new Vector2 ( -walkSpeed, 0 ) );
                } else if ( kbManager [ Keys.D, KeyboardManager.PressType.ever ] ) {
                    bodies [ "hero" ].ApplyLinearImpulse ( velocity = new Vector2 ( walkSpeed, 0 ) );
                } else {
                    velocity = new Vector2 ( 0 );
                }

                if ( kbManager[ Keys.W, KeyboardManager.PressType.once ] ) {
                    bodies[ "hero" ].ApplyLinearImpulse ( new Vector2 ( 0, -5f ) );
                }

                bodies[ "hero" ].Update ( gt );

                world.Step ( (float)gt.ElapsedGameTime.Milliseconds / 1000f );
            }

            public void Draw ( SpriteBatch spriteBatch ) {
                foreach ( Object obj in bodies.Values ) {
                    obj.Draw ( spriteBatch );
                }
            }

            public string Debug () {
                return "";
            }
        }
        #endregion


        #region Properties

        PifagorasTree pfTree;
        GameWorld game;

        GameState gState;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        static KeyboardManager kbManager;
        Menu menu;
        Menu setup;

        #endregion


        #region InitializesAndDeinitializes

        public Game1 () {
            kbManager = new KeyboardManager ();
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

            Content.RootDirectory = "Content";

            pfTree = new PifagorasTree ( this );
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

            pfTree.Initialize ();
            game = new GameWorld ();

            base.Initialize ();
        }

        protected override void LoadContent () {
            spriteBatch = new SpriteBatch ( GraphicsDevice );

            font = Content.Load<SpriteFont> ( "Font" );
            game.LoadContent ( Content );

            pfTree.publicLoadContent ();
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
                case GameState.editor:
                case GameState.game: {
                        game.Update ( gameTime );
                        if ( kbManager[ Keys.Tab ] ) {
                            changeState ( ( gState == GameState.editor ? GameState.game : GameState.editor ) );
                        }
                        break;
                    }
                default: {
                        if ( kbManager [ Keys.Escape ] ) {
                            changeState ( GameState.menu );
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
                    spriteBatch.DrawString ( font, gState.ToString () + game.Debug (), new Vector2 ( 30 ), Color.Black );
                    game.Draw ( spriteBatch );
                    spriteBatch.End ();
                    break;
                case GameState.pifagorTreeDemo:
                    pfTree.Draw ( gameTime );
                    break;
            }

            base.Draw ( gameTime );
        }

        #endregion
    }
}
