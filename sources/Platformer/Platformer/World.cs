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


namespace Platformer {
    public class GameWorld : Microsoft.Xna.Framework.DrawableGameComponent {
        Matrix view;
        float cameraShiftX = 0, cameraShiftY = 0;
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
                public Double milliseconds;
            }

            public SpriteEffects effects = SpriteEffects.None;
            public int h, w;
            Texture2D texture;
            string currentAnimation;
            int currentFrameIndex;
            Double msFrameLeft = 0;
            float myDepth;

            Dictionary < string, List < Frame > > animations = new Dictionary<string, List<Frame>> ();

            public Object ( World world, int width, int height, Vector2 position, Texture2D text, float depth, bool isStatic ) :
                base ( world ) {
                myDepth = depth;
                w = width;
                h = height;
                texture = text;
                Vertices rectangleVertices = PolygonTools.CreateRectangle ( Converter.ToSimulate ( width / 2 ),
                                                                            Converter.ToSimulate ( height / 2 ) );
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
                this.Restitution = 0.01f;
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
                //if ( !animations.ContainsKey ( name ) )
                //    throw new Exception ( "Cannot play uninitialised animation!" );
                currentAnimation = name;
                currentFrameIndex = 0;
                msFrameLeft = animations[ currentAnimation ][ currentFrameIndex ].milliseconds;
            }

            public void SetDefaultAnimation () {
                currentAnimation = "default";
                currentFrameIndex = 0;
                msFrameLeft = animations[ currentAnimation ][ currentFrameIndex ].milliseconds;
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
                var rect = animations[ currentAnimation ][ currentFrameIndex ].spriteRect;

                batch.Draw ( texture,
                    Converter.ToPixelsVec ( this.Position ),
                    rect,
                    Color.White,
                    0.0f,
                    new Vector2 ( rect.Width/2f, rect.Height/2f ),
                    1.0f,
                    effects,
                    myDepth );
            }
        }

        Vector2 myCenter;
        SpriteBatch spriteBatch;
        ContentManager content;
        

        public GameWorld ( Game game ) : base ( game ) {
            content = new ContentManager ( game.Services, "Content" );

            view = Matrix.Identity;
        }

        public override void Initialize () {
            spriteBatch = new SpriteBatch ( Game.GraphicsDevice );
            base.Initialize ();
        }

        protected override void LoadContent () {
            Converter.SetRation ( 64.0f );

            myCenter = new Vector2 ( Game.GraphicsDevice.Viewport.Width/2, Game.GraphicsDevice.Viewport.Height/2 );

            XmlDocument map = new XmlDocument ();
            map.Load ( "map1.xml" );
            foreach ( XmlNode node in map.ChildNodes[ 1 ].ChildNodes ) {
                if ( node.Name == "put" ) {
                    XmlDocument sets = new XmlDocument ();
                    sets.Load ( node.Attributes.GetNamedItem ( "from" ).Value + ".xml" );

                    XmlNode textureSets = sets.ChildNodes[ 1 ];
                    Texture2D texture = content.Load<Texture2D> ( textureSets.Attributes.GetNamedItem ( "name" ).Value );

                    foreach ( XmlNode obj in node.ChildNodes ) {


                        string[] pos    = obj.Attributes.GetNamedItem ( "position" ).Value.Split ( ',' );
                        string[] pSize  = obj.Attributes.GetNamedItem ( "physize" ).Value.Split ( ',' );
                        bool isStatic   = obj.Attributes.GetNamedItem ( "static" ).Value == "true";
                        string name     = obj.Attributes.GetNamedItem ( "name" ).Value;
                        string animStart= node.Name;

                        Object b = new Object ( world, int.Parse ( pSize[ 0 ] ), int.Parse ( pSize[ 1 ] ),
                                                new Vector2 ( int.Parse ( pos[ 0 ] ), int.Parse ( pos[ 1 ] ) ),
                                                texture, (name == "hero" ? 0.0f : 1.0f), isStatic );


                        bodies.Add ( name, b );

                        foreach ( XmlNode frameSets in textureSets.ChildNodes ) {
                            List<Object.Frame> frames = new List<Object.Frame> ();
                            foreach ( XmlNode frame in frameSets.ChildNodes ) {

                                int x = 0, y = 0, w = 0, h = 0, c = 0;
                                string tStr = frame.Attributes.GetNamedItem ( "time" ).Value;
                                Double t = (tStr == "Infinity" ? Double.PositiveInfinity : Double.Parse ( tStr ));


                                foreach ( XmlNode size in frame.ChildNodes ) {
                                    if ( size.Name == "size" ) {
                                        w = int.Parse ( size.Attributes.GetNamedItem ( "width" ).Value );
                                        h = int.Parse ( size.Attributes.GetNamedItem ( "height" ).Value );
                                        c++;
                                    } else if ( size.Name == "position" ) {
                                        x = int.Parse ( size.Attributes.GetNamedItem ( "x" ).Value );
                                        y = int.Parse ( size.Attributes.GetNamedItem ( "y" ).Value );
                                        c++;
                                    }
                                }

                                if ( c < 2 ) {
                                    throw new Exception ( "I see u xml is not valid. Check code above to understand xml structure =)" );
                                }
                                frames.Add ( new Object.Frame {
                                    milliseconds = t,
                                    spriteRect = new Rectangle ( x, y, w, h )
                                } );


                            }
                            bodies[ name ].AddAnimation ( frameSets.Name, frames.ToArray () );
                        }
                        bodies[ name ].PlayAnimation ( obj.Name );
                    }
                }
            }
        }

        const float walkSpeed = 2f;
        bool heroMoveFlag = false;

        bool first = true;
        float lastBodyPosX = 0, lastBodyPosY = 0;
        int frameCounter = 0;

        public override void Update ( GameTime gameTime ) {
            if ( Enabled ) {
                if ( first ) {
                    first = !first;
                    lastBodyPosX = bodies[ "hero" ].Position.X;
                    lastBodyPosY = bodies[ "hero" ].Position.Y;
                }


                bodies[ "hero" ].ApplyLinearImpulse ( new Vector2 ( -bodies[ "hero" ].LinearVelocity.X, 0 ) );
                if ( Game1.kbManager[ Keys.A, Game1.KeyboardManager.PressType.ever ] && Game1.kbManager[ Keys.D, Game1.KeyboardManager.PressType.ever ] ) {
                    heroMoveFlag = false;
                    bodies[ "hero" ].SetDefaultAnimation ();
                } else if ( Game1.kbManager[ Keys.A, Game1.KeyboardManager.PressType.ever ] ) {
                    if ( !heroMoveFlag ) {
                        bodies[ "hero" ].PlayAnimation ( "move" );
                        heroMoveFlag = true;
                    }
                    bodies[ "hero" ].ApplyLinearImpulse ( new Vector2 ( -walkSpeed, 0 ) );
                    bodies[ "hero" ].effects = SpriteEffects.FlipHorizontally;
                } else if ( Game1.kbManager[ Keys.D, Game1.KeyboardManager.PressType.ever ] ) {
                    if ( !heroMoveFlag ) {
                        bodies[ "hero" ].PlayAnimation ( "move" );
                        heroMoveFlag = true;
                    }
                    bodies[ "hero" ].ApplyLinearImpulse ( new Vector2 ( walkSpeed, 0 ) );
                    bodies[ "hero" ].effects = SpriteEffects.None;
                } else if ( heroMoveFlag ) {
                    heroMoveFlag = false;
                    bodies[ "hero" ].SetDefaultAnimation ();
                }

                if ( Game1.kbManager[ Keys.W, Game1.KeyboardManager.PressType.once ] ) {
                    bodies[ "hero" ].ApplyLinearImpulse ( new Vector2 ( 0, -5f ) );
                }

                bodies[ "hero" ].Update ( gameTime );


                world.Step ( (float)gameTime.ElapsedGameTime.Milliseconds / 1000f );

                const float maxShift = 2;
                cameraShiftX = Math.Min ( maxShift, Math.Max ( -maxShift, cameraShiftX + bodies[ "hero" ].Position.X - lastBodyPosX ) );
                cameraShiftY = Math.Min ( maxShift, Math.Max ( -maxShift, cameraShiftY + bodies[ "hero" ].Position.Y - lastBodyPosY ) );

                lastBodyPosX = bodies[ "hero" ].Position.X;
                lastBodyPosY = bodies[ "hero" ].Position.Y;

                base.Update ( gameTime );
            }
        }

        public override void Draw ( GameTime gameTime ) {
            if ( Enabled ) {
                view = Matrix.CreateTranslation ( new Vector3 ( myCenter - Converter.ToPixelsVec ( bodies[ "hero" ].Position - new Vector2 ( cameraShiftX, cameraShiftY ) ), 0f ) );
                spriteBatch.Begin ( SpriteSortMode.BackToFront, null, null, null, null, null, view );
                foreach ( Object obj in bodies.Values ) {
                    obj.Draw ( spriteBatch );
                }
                spriteBatch.End ();

                base.Update ( gameTime );
            }
        }

        public string Debug () {
            return "\n" + cameraShiftX + " " + cameraShiftY;
        }
    }
}
