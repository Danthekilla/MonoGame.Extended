﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Content;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Graphics.Batching;

namespace Demo.PrimitiveBatch
{
    public class Game1 : Game
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly GraphicsDeviceManager _graphicsDeviceManager;

        // primitive batch for convex polygons
        private PrimitiveBatch<VertexPositionColor> _primitiveBatchPositionColor;
        // primitive batch for sprites (quads with texture)
        private PrimitiveBatch<VertexPositionColorTexture> _primitiveBatchPositionColorTexture;

        // a material for the polygons
        private PolygonEffectMaterial _polygonMaterial;
        // a material for the sprites 
        // a new material will be required for each texture
        private SpriteEffectMaterial _spriteMaterial; 

        // the polygon
        private FaceVertexPolygonMesh<VertexPositionColor> _polygonMesh;
        // world view projection matrices;
        private Matrix _cartesianProjection2D;
        private Matrix _cartesianCamera2D;
        private Matrix _cartesianWorld;
        private Matrix _spriteBatchProjection;
        private Matrix _spriteBatchCamera;
        private Matrix _spriteBatchWorld;

        // the rotation angle of the sprite
        private float _spriteRotation;

        public Game1()
        {
            _graphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.Position = Point.Zero;
        }

        protected override void LoadContent()
        {
            // get a reference to the graphics device
            var graphicsDevice = GraphicsDevice;

            // viewport: the dimensions and properties of the drawable surface
            var viewport = graphicsDevice.Viewport;

            // world matrix: the coordinate system of the world or universe used to transform primitives from their own Local space to the World space
            // here we scale the x, y and z axes by 100 units
            _cartesianWorld = Matrix.CreateScale(new Vector3(100, 100, 100));

            // view matrix: the camera; use to transform primitives from World space to View (or Camera) space
            // here we don't do anything by using the identity matrix
            _cartesianCamera2D = Matrix.Identity;

            // projection matrix: the mapping from View or Camera space to Projection space so the GPU knows what information from the scene is to be rendered 
            // here we create an orthographic projection; a 3D box in screen space (one side is the screen) where any primitives outside this box is not rendered
            // here the box is setup so the origin (0,0,0) is the centre of the screen's surface
            // here an adjustment by half a pixel is also added because there’s a discrepancy between how the centers of pixels and the centers of texels are computed
            _cartesianProjection2D = Matrix.CreateTranslation(-0.5f, -0.5f, 0) * Matrix.CreateOrthographicOffCenter(viewport.Width * -0.5f, viewport.Width * 0.5f, viewport.Height * -0.5f, viewport.Height * 0.5f, 0, 1);

            // world matrix: the coordinate system of the world or universe used to transform primitives from their own Local space to the World space
            // here we don't do anything by using the identity matrix leaving screen pixel units as world units
            _spriteBatchWorld = Matrix.Identity;

            // view matrix: the camera; use to transform primitives from World space to View (or Camera) space
            // here we don't do anything by using the identity matrix
            _spriteBatchCamera = Matrix.Identity;

            // projection matrix: the mapping from View or Camera space to Projection space so the GPU knows what information from the scene is to be rendered 
            // here we create an orthographic projection; a 3D box in screen space (one side is the screen) where any primitives outside this box is not rendered
            // here the box is set so the origin (0,0,0) is the top-left of the screen's surface
            // the Z axis is also flipped by setting the near plane to 0 and the far plane to -1. (by default -Z is into the screen, +Z is popping out of the screen)
            // here an adjustment by half a pixel is also added because there’s a discrepancy between how the centers of pixels and the centers of texels are computed
            _spriteBatchProjection = Matrix.CreateTranslation(-0.5f, -0.5f, 0) * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1);

            // load the custom effect for the polygons
            var polygonEffect = new PolygonEffect(Content.Load<Effect>("PolygonEffect"));
            // create a material for rendering polygons
            _polygonMaterial = new PolygonEffectMaterial(polygonEffect);

            // load the custom effect for the sprites
            var spriteEffect = new SpriteEffect(Content.Load<Effect>("SpriteEffect"));
            // load the texture for the sprites
            var spriteTexture = Content.Load<Texture2D>("logo-square-128");
            // create a material for rendering sprites
            // each texture will need a seperate material
            _spriteMaterial = new SpriteEffectMaterial(spriteEffect, spriteTexture);

            // create the VertexPositionColor PrimitiveBatch for rendering the polygons
            _primitiveBatchPositionColor = new PrimitiveBatch<VertexPositionColor>(graphicsDevice, Array.Sort);
            // create the VertexPositionColorTexture PrimitiveBatch for rendering the sprites
            _primitiveBatchPositionColorTexture = new PrimitiveBatch<VertexPositionColorTexture>(graphicsDevice, Array.Sort);

            // create our polygon mesh; vertices are in Local space; indices are index references to the vertices to draw 
            // indices have to multiple of 3 for PrimitiveType.TriangleList which says to draw a collection of triangles each with 3 vertices (different triangles can share vertices)
            // TriangleList is the most common way to have vertices layed out in memory for uploading to the GPU for most common scnearios
            var vertices = new[]
            {
                new VertexPositionColor(new Vector3(0, 0, 0), Color.Red),
                new VertexPositionColor(new Vector3(2, 0, 0), Color.Blue),
                new VertexPositionColor(new Vector3(1, 2, 0), Color.Green),
                new VertexPositionColor(new Vector3(3, 2, 0), Color.White)
            };
            var indices = new short[]
            {
                2,
                1,
                0,
                3,
                1,
                2
            };
            _polygonMesh = new FaceVertexPolygonMesh<VertexPositionColor>(PrimitiveType.TriangleList, vertices, indices);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // clear the (pixel) buffers to a specific color
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // set the states for rendering
            // this could be moved outside the render loop if it doesn't change frame per frame 
            // however, it's left here indicating it's possible and common to change the state between frames

            // use alphablend so the transparent part of the texture is blended with the color behind it
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            var polygonEffect = _polygonMaterial.Effect;
            // apply the world view projection matrices for cartesian drawing
            polygonEffect.World = _cartesianWorld;
            polygonEffect.View = _cartesianCamera2D;
            polygonEffect.Projection = _cartesianProjection2D;

            // draw the polygon mesh in the cartesian coordinate system using the VertexPositionColor PrimitiveBatch
            _primitiveBatchPositionColor.Begin(BatchSortMode.Immediate, PrimitiveType.TriangleList);
            _primitiveBatchPositionColor.DrawPolygonMesh(_polygonMaterial, _polygonMesh);
            _primitiveBatchPositionColor.End();

            // apply the world view projection matrices for sprite drawing
            var spriteEffect = _spriteMaterial.Effect;
            spriteEffect.World = _spriteBatchWorld;
            spriteEffect.View = _spriteBatchCamera;
            spriteEffect.Projection = _spriteBatchProjection;

            // draw the sprite in the screen coordinate system using the VertexPositionColorTexture PrimitiveBatch
            _primitiveBatchPositionColorTexture.Begin(BatchSortMode.Immediate, PrimitiveType.TriangleList);
            var viewport = GraphicsDevice.Viewport;
            var spriteColor = Color.White;
            var spriteOrigin = new Vector2(_spriteMaterial.Texture.Width * 0.5f, _spriteMaterial.Texture.Height * 0.5f);
            var spritePosition = new Vector2(viewport.Width * 0.25f, viewport.Height * 0.25f);
            var spriteDepth = 0f;
            _spriteRotation += MathHelper.ToRadians(1);
            _primitiveBatchPositionColorTexture.DrawSprite(_spriteMaterial, _spriteMaterial.Texture, null, new Vector3(spritePosition, spriteDepth), color: spriteColor, rotation: _spriteRotation, origin: spriteOrigin);
            _primitiveBatchPositionColorTexture.End();

            base.Draw(gameTime);
        }
    }
}
