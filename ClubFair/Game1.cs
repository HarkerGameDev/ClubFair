using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClubFair
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        const int PLAYERS = 5;
        const int SPEED_1 = 520;
        const int SPEED_2 = 450;
        const int RADIUS_1 = 32;
        const int RADIUS_2 = 40;
        const float PEACE_TIME = 3;
        const float MIN_TAG = 2.0f;
        const double GAME_TIME = 20;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D whiteRect;
        SpriteFont font, fontBig;

        bool paused;
        Player[] players;
        Random rand;
        float resetTime;
        int current;
        int deadPlayers;
        double _gameTime;
        int totalGames;

        struct Player
        {
            public Vector2 Position;
            public int Radius;
            public int Speed;
            public Color Color;
            public bool Alive;
            public int Wins;
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();

            players = new Player[PLAYERS];
            rand = new Random();
            int width = graphics.GraphicsDevice.Viewport.Width;
            int height = graphics.GraphicsDevice.Viewport.Height;
            for (int i = 0; i < players.Length; i++)
            {
                players[i].Color = new Color(rand.Next(256), rand.Next(256), rand.Next(256));
                players[i].Speed = SPEED_1;
                players[i].Radius = RADIUS_1;
                players[i].Wins = 0;
            }

            totalGames = 0;
            paused = false;

            Reset();

            base.Initialize();
        }

        private void Reset()
        {
            resetTime = PEACE_TIME;
            current = -1;
            deadPlayers = 0;
            _gameTime = 0;

            int width = graphics.GraphicsDevice.Viewport.Width;
            int height = graphics.GraphicsDevice.Viewport.Height;
            for (int i = 0; i < players.Length; i++)
            {
                players[i].Position = new Vector2(rand.Next(width), rand.Next(height));
                players[i].Alive = true;
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            whiteRect = new Texture2D(GraphicsDevice, 1, 1);
            whiteRect.SetData(new[] { Color.White });

            font = Content.Load<SpriteFont>("Score");
            fontBig = Content.Load<SpriteFont>("ScoreBig");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            whiteRect.Dispose();
            Content.Unload();
        }

        private void moveKeyboardPlayer(float deltaTime)
        {
            KeyboardState state = Keyboard.GetState();
            float speed;
            if (current == 0)
                speed = deltaTime * SPEED_2;
            else
                speed = deltaTime * players[0].Speed;

            if (state.IsKeyDown(Keys.Right) || state.IsKeyDown(Keys.D))
                players[0].Position.X += speed;
            if (state.IsKeyDown(Keys.Left) || state.IsKeyDown(Keys.A))
                players[0].Position.X -= speed;
            if (state.IsKeyDown(Keys.Down) || state.IsKeyDown(Keys.S))
                players[0].Position.Y += speed;
            if (state.IsKeyDown(Keys.Up) || state.IsKeyDown(Keys.W))
                players[0].Position.Y -= speed;
        }
        
        private void moveGamePadPlayers(float deltaTime)
        {
            for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; index++)
            {
                GamePadCapabilities capabilities = GamePad.GetCapabilities(index);
                if (capabilities.IsConnected && capabilities.HasLeftXThumbStick)
                {
                    int i = (int)index + 1;
                    if (i < players.Length)
                    {
                        float speed;
                        if (current == i)
                            speed = deltaTime * SPEED_2;
                        else
                            speed = deltaTime * players[i].Speed;

                        GamePadState state = GamePad.GetState(index, GamePadDeadZone.Circular);
                        players[i].Position.X += state.ThumbSticks.Left.X * speed;
                        players[i].Position.Y -= state.ThumbSticks.Left.Y * speed;
                    }
                }
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                paused = !paused;

                // TODO: Add your update logic here
            if (!paused) {
                _gameTime += deltaTime;

                if (deadPlayers >= players.Length - 1)
                {
                    _gameTime = 0.0;
                    players[current].Wins++;
                    Reset();
                    totalGames++;
                }

                moveKeyboardPlayer(deltaTime);
                moveGamePadPlayers(deltaTime);

                resetTime -= deltaTime;
                if (resetTime < 0)
                {
                    resetTime = (float)rand.NextDouble() + MIN_TAG;
                    int next = rand.Next(players.Length);
                    while (next == current || !players[next].Alive)
                        next = rand.Next(players.Length);
                    current = next;
                }

                int minX = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Width / 2.0);
                int minY = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Height / 2.0);
                int width = graphics.GraphicsDevice.Viewport.Width - minX * 2;
                int height = graphics.GraphicsDevice.Viewport.Height - minY * 2;

                for (int i = 0; i < players.Length; i++)
                {
                    // check boundaries
                    int maxX, maxY;
                    if (i == current)
                    {
                        maxX = minX + width - RADIUS_2 * 2;
                        maxY = minY + height - RADIUS_2 * 2;
                    }
                    else
                    {
                        maxX = minX + width - players[i].Radius * 2;
                        maxY = minY + height - players[i].Radius * 2;
                    }

                    if (players[i].Position.X > maxX)
                        players[i].Position.X = maxX;
                    else if (players[i].Position.X < minX)
                        players[i].Position.X = minX;
                    if (players[i].Position.Y > maxY)
                        players[i].Position.Y = maxY;
                    else if (players[i].Position.Y < minY)
                        players[i].Position.Y = minY;

                    // check collisions to tag player
                    if (i != current && current >= 0 && players[i].Alive)
                    {
                        float dist = (players[current].Position - players[i].Position).Length();
                        if (dist < players[current].Radius + players[i].Radius)
                        {
                            players[i].Alive = false;
                            deadPlayers++;
                        }
                    }
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin();

            int minX = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Width / 2.0);
            int minY = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Height / 2.0);
            int width = graphics.GraphicsDevice.Viewport.Width - minX * 2;
            int height = graphics.GraphicsDevice.Viewport.Height - minY * 2;
            spriteBatch.Draw(whiteRect, new Rectangle(minX, minY, width, height), Color.CornflowerBlue);

            for (int i = 0; i < players.Length; i++)
            {
                if (i == current)
                {
                    Texture2D circle = CreateCircle(RADIUS_2);
                    spriteBatch.Draw(circle, players[i].Position, Color.Red);
                } else if (players[i].Alive) {
                    Texture2D circle = CreateCircle(players[i].Radius);
                    spriteBatch.Draw(circle, players[i].Position, players[i].Color);
                }
                //Console.WriteLine(player.Position);
            }

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.AppendLine("Scores");
            for (int i=0; i<players.Length; i++) {
                text.AppendLine(string.Format("Player {0}: {1}", i+1, players[i].Wins));
            }
            text.AppendLine(string.Format("Total games - {0}", totalGames));
            spriteBatch.DrawString(font, text, new Vector2(10, 10), Color.Green);

            if (current < 0)
                spriteBatch.DrawString(fontBig, "Run from red", new Vector2(600, 400), Color.Red);

            if (paused)
                spriteBatch.DrawString(fontBig, "Paused", new Vector2(600, 100), Color.Yellow);

            spriteBatch.DrawString(fontBig, "GAME DEV CLUB!", new Vector2(500, 100), Color.Red);

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private Texture2D CreateCircle(int radius)
        {
            int outerRadius = radius * 2 + 2; // So circle doesn't go out of bounds
            Texture2D texture = new Texture2D(GraphicsDevice, outerRadius, outerRadius);

            Color[] data = new Color[outerRadius * outerRadius];

            // Colour the entire texture transparent first.
            for (int i = 0; i < data.Length; i++)
                data[i] = Color.Transparent;

            // Work out the minimum step necessary using trigonometry + sine approximation.
            double angleStep = 1f / radius;

            for (double angle = 0; angle < Math.PI; angle += angleStep)
            {
                // Use the parametric definition of a circle: http://en.wikipedia.org/wiki/Circle#Cartesian_coordinates
                int x = (int)Math.Round(radius + radius * Math.Cos(angle));
                int y = (int)Math.Round(radius + radius * Math.Sin(angle));

                for (int i=radius*2 - y; i<=y; i++)
                    data[i * outerRadius + x + 1] = Color.White;
            }

            texture.SetData(data);
            return texture;
        }
    }
}
