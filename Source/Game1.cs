using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Source
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		// Game related constants
		const int PLAYERS = 5;
		const float PEACE_TIME = 3;
		const double GAME_TIME = 20.0;
		const float MIN_TAG = 2.0f;
		const float MIN_POWER_TIME = 5.0f;
		const float MAX_POWER_TIME = 10.0f;

		// Player related constants
		const int SPEED_1 = 520;
		const float SPEED_2_SCALE = 0.86f;
		const int RADIUS_1 = 32;
		const float RADIUS_2_SCALE = 1.25f;

		// Powerup related constants
		const int POWERUP_LENGTH = 50;
		const float POWERUP_DURATION = 4.0f;
		const float POWERUP_FASTER_ALL = 2.0f;
		const float POWERUP_SLOWER_ALL = 0.5f;

		GraphicsDeviceManager graphics;     // this is always here
		SpriteBatch spriteBatch;            // so is this
		Texture2D whiteRect;                // useful for drawing rectangles
		SpriteFont font, fontBig;           // fonts need to always be generated and loaded using content manager

		bool paused;
		Player[] players;
		Random rand;
		float resetTime;    //Time until current player swaps
		float powerTime;    //Time until new powerup spawns
		int current;
		int deadPlayers;
		double _gameTime;   //Total game time used for scaling level
		int totalGames;
		List<Powerup> powerUps;

		class Player
		{
			public Vector2 Position;
			public float Radius;
			public float Speed;
			public Color Color;
			public bool Alive;
			public int Wins;
		}

		// IMPORTANT! The order of the colors defined here must correspond to the order
		// in which the enum PowerType is defined.
		Color[] _powerUpColors = { Color.Red, Color.Blue, Color.Purple, Color.Cyan };

		enum PowerType  // Don't forget to add a color if you add a new powerup
		{
			AllFaster, AllSlower, RedWins, BecomeRed
		}

		class Powerup
		{
			public Vector2 Position;
			public PowerType Type;
			public int PlayerAffected;
			public float TimeLeft;
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
			// set game to fullscreen and match monitor resolution
			graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
			graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
			graphics.IsFullScreen = true;
			graphics.ApplyChanges();

			// initialize all players with a random color, and specified Speed and Radius
			players = new Player[PLAYERS];
			rand = new Random();
			int width = graphics.GraphicsDevice.Viewport.Width;     // width of screen
			int height = graphics.GraphicsDevice.Viewport.Height;   // height of screen
            for (int i = 0; i < players.Length; i++)
            {
                Player player = new Player();
                players[i] = player;
                player.Color = new Color(rand.Next(256), rand.Next(256), rand.Next(256));
                player.Speed = SPEED_1;
                player.Radius = RADIUS_1;
                player.Wins = 0;
            }

			totalGames = 0;
			paused = false;

			Reset();

			base.Initialize();
		}

		/// <summary>
		/// Starts a new round. It is important to note the difference between Reset()
		/// and Initialize(). Initialize() is called ONLY ONCE when the program is run.
		/// Reset(), in the way I have written it, is called whenever a new round is started,
		/// so when only player remains. This means resetting colors, speeds, or wins of the players
		/// should NOT be done in Reset()
		/// </summary>
		private void Reset()
		{
			resetTime = PEACE_TIME;
			powerTime = PEACE_TIME;
			current = -1;
			deadPlayers = 0;
			_gameTime = 0;

			int width = graphics.GraphicsDevice.Viewport.Width;
			int height = graphics.GraphicsDevice.Viewport.Height;
			foreach (Player player in players)
			{
				player.Position = new Vector2(rand.Next(width), rand.Next(height));
				player.Alive = true;
			}

			powerUps = new List<Powerup>();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			// create a white rectangle by making a 1x1 pixel block. Size can be 
			// changed during drawing by scaling. Color is white because color masking
			// in spritebatch (Draw) uses White as the color to replace by the mask.
			whiteRect = new Texture2D(GraphicsDevice, 1, 1);
			whiteRect.SetData(new[] { Color.White });

			// Load fonts. Important note, fonts need to be loaded and created from the
			// content manager. Look up "monogame fonts" on the internet for more info.
			font = Content.Load<SpriteFont>("Score");
			fontBig = Content.Load<SpriteFont>("ScoreBig");
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// content created manually must be disposed manually, with object.Dispose()
			whiteRect.Dispose();

			// all content loaded fron Content.Load can simply be unloaded with Content.Unload
			Content.Unload();
		}

		/// <summary>
		/// Moves player 0 based on input from the keyboard
		/// </summary>
		/// <param name="deltaTime">Time since last call</param>
		private void moveKeyboardPlayer(float deltaTime)
		{
			KeyboardState state = Keyboard.GetState();
			float speed;
            Player player = players[0];

			// Important, ALWAYS multiply speed by delta time when moving
			speed = deltaTime * player.Speed;

			if (state.IsKeyDown(Keys.Right) || state.IsKeyDown(Keys.D))
				player.Position.X += speed;
			if (state.IsKeyDown(Keys.Left) || state.IsKeyDown(Keys.A))
				player.Position.X -= speed;
			if (state.IsKeyDown(Keys.Down) || state.IsKeyDown(Keys.S))
				player.Position.Y += speed;
			if (state.IsKeyDown(Keys.Up) || state.IsKeyDown(Keys.W))
				player.Position.Y -= speed;
		}

		/// <summary>
		/// Moves players 1 to 4 based on input from gamepads
		/// </summary>
		/// <param name="deltaTime">Time since last call</param>
		private void moveGamePadPlayers(float deltaTime)
		{
			// Connected gamepads use enums, for whatever reason
			for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; index++)
			{
				// use GamePadCapabilities to see if connected gamepad has required inputs
				GamePadCapabilities capabilities = GamePad.GetCapabilities(index);
				if (capabilities.IsConnected && capabilities.HasLeftXThumbStick)
				{
					// convert enum to integer from 1-4 (for players array)
					int i = (int)index + 1;

					// make sure there is no index out of bounds if there are less than 5 players
					if (i < players.Length)
					{
						// again, ALWAYS scale anything that moves by deltaTime
						float speed;
                        Player player = players[i];
						speed = deltaTime * player.Speed;

						// add deadzone and move. Note, Y axis is inverted
						GamePadState state = GamePad.GetState(index, GamePadDeadZone.Circular);
						player.Position.X += state.ThumbSticks.Left.X * speed;
						player.Position.Y -= state.ThumbSticks.Left.Y * speed;
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
			// ALWAYS do this
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			// Note, this does not work well. See if you can figure out why (and fix it)   :)
			if (Keyboard.GetState().IsKeyDown(Keys.Space))
				paused = !paused;

			// This is pretty much how pause always works
			if (!paused) {
				_gameTime += deltaTime;

				// Decrease time for powerups (be sure to do this before any crazy messing about)
				for (int i=powerUps.Count-1; i>=0; i--)
				{
					Powerup power = powerUps [i];
                    if (power.PlayerAffected >= 0)          // powerup has been collected
                    {
                        powerUps[i].TimeLeft -= deltaTime;
                        if (powerUps[i].TimeLeft < 0)
                            powerUps.RemoveAt(i);
                    }
				}

                // Apply global powerups
				foreach (Powerup power in powerUps)
				{
					if (power.PlayerAffected >= 0)
					{
						switch (power.Type)
						{
						case PowerType.AllFaster:
							deltaTime *= POWERUP_FASTER_ALL;    // Note: changing deltaTime could have VERY SEVERE side effects, and I think it does in this case
							break;
						case PowerType.AllSlower:
							deltaTime *= POWERUP_SLOWER_ALL;
							break;
                        case PowerType.RedWins:         // Note of warning: this does not kill red. It makes the current red player instantly win.
                            if(current>=0)
                            {
                                players[current].Alive = false;
                                deadPlayers++;
                            }
                            break;
						case PowerType.BecomeRed:
							// make sure the new red player is not dead
							if (players [power.PlayerAffected].Alive) {
								players [current].Radius /= RADIUS_2_SCALE;
								players [current].Speed /= SPEED_2_SCALE;

								current = power.PlayerAffected;
								// Set new player radius and speed scales
								players [current].Speed *= SPEED_2_SCALE;
								players [current].Radius *= RADIUS_2_SCALE;
                                resetTime = 1;          // Make sure game does not try to automatically set the next current player
							}
							break;
						}
					}
				}

				// reset game once everyone is dead
				if (deadPlayers >= players.Length - 1)
				{
					players[current].Wins++;
                    players[current].Speed /= SPEED_2_SCALE;
                    players[current].Radius /= RADIUS_2_SCALE;
					Reset();
					totalGames++;
				}

				moveKeyboardPlayer(deltaTime);
				moveGamePadPlayers(deltaTime);

				// resetTime is time until red player switches
				resetTime -= deltaTime;
				if (resetTime < 0)
				{
                    // Undo radius and speed scales
                    if (current >= 0)
                    {
                        players[current].Speed /= SPEED_2_SCALE;
                        players[current].Radius /= RADIUS_2_SCALE;
                    }

					// Time to reset is randomly acquired
					resetTime = (float)rand.NextDouble() + MIN_TAG;

					int next = rand.Next(players.Length);

					// make sure the new red player is not the same or dead
					while (next == current || !players[next].Alive)
						next = rand.Next(players.Length);
					current = next;

                    // Set new player radius and speed scales
                    players[current].Speed *= SPEED_2_SCALE;
                    players[current].Radius = ((float)players[current].Radius * RADIUS_2_SCALE);
				}

				// Calculate bounds of the shrinking, playable screen by using game time since reset
				int minX = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Width / 2.0);
				int minY = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Height / 2.0);
				int width = graphics.GraphicsDevice.Viewport.Width - minX * 2;
				int height = graphics.GraphicsDevice.Viewport.Height - minY * 2;

				// powerTime is time until a powerup is spawned
				powerTime -= deltaTime;
				if (powerTime < 0)
				{
					powerTime = (float)rand.NextDouble() * (MAX_POWER_TIME - MIN_POWER_TIME) + MIN_POWER_TIME;

					// Create a new powerup
                    Powerup power = new Powerup();

					// Get type of powerup randomly
					Array values = Enum.GetValues(typeof(PowerType));
                    power.Type = (PowerType)values.GetValue(rand.Next(values.Length));
                    
                    // use something like this for testing
                    //power.Type = PowerType.RedWins;

					power.PlayerAffected = -1;  // -1 means nobody picked it up. Why? Because I said so.
					power.TimeLeft = POWERUP_DURATION;
					power.Position = new Vector2(rand.Next(width) + minX, rand.Next(height) + minY);

					powerUps.Add(power);
				}
				for (int i = 0; i < powerUps.Count; i++) {
					Powerup power = powerUps[i];
					int maxX, maxY;
					maxX = minX + width - (int)(POWERUP_LENGTH);
					maxY = minY + height - (int)(POWERUP_LENGTH);

					// Make sure powerup is within bounds
					if (power.Position.X > maxX)
						power.Position.X = maxX;
					else if (power.Position.X < minX)
						power.Position.X = minX;
					if (power.Position.Y > maxY)
						power.Position.Y = maxY;
					else if (power.Position.Y < minY)
						power.Position.Y = minY;
				}
				for (int i = 0; i < players.Length; i++)
				{
                    Player player = players[i];
                    if (player.Alive)
                    {
                        // Calculate bounds for player
                        int maxX, maxY;
                        maxX = minX + width - (int)(player.Radius * 2);
                        maxY = minY + height - (int)(player.Radius * 2);

                        // Make sure player is within bounds
                        if (player.Position.X > maxX)
                            player.Position.X = maxX;
                        else if (player.Position.X < minX)
                            player.Position.X = minX;
                        if (player.Position.Y > maxY)
                            player.Position.Y = maxY;
                        else if (player.Position.Y < minY)
                            player.Position.Y = minY;

                        // check collisions to tag player
                        if (i != current && current >= 0)
                        {
                            // Circle collisions are really easy because only the distance between the centers matter
                            float dist = (players[current].Position - player.Position).Length();
                            if (dist < players[current].Radius + player.Radius)
                            {
                                player.Alive = false;
                                deadPlayers++;
                            }
                        }

                        // check collisions to pick up powerup
                        foreach (Powerup power in powerUps)
                        {
                            if (power.PlayerAffected < 0)
                            {
                                float dist = (player.Position - power.Position).Length();
                                if (dist < player.Radius + POWERUP_LENGTH)
                                {
                                    power.PlayerAffected = i;
                                }
                            }
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

			spriteBatch.Begin();

			// Calculate bounds of the shrinking, playable screen by using game time since reset
			int minX = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Width / 2.0);
			int minY = (int)((_gameTime) / GAME_TIME * (double)graphics.GraphicsDevice.Viewport.Height / 2.0);
			int width = graphics.GraphicsDevice.Viewport.Width - minX * 2;
			int height = graphics.GraphicsDevice.Viewport.Height - minY * 2;

			// Draw a blue rectangle in the playable area, using a scale and mask on the 1x1 white rectangle
			spriteBatch.Draw(whiteRect, new Rectangle(minX, minY, width, height), Color.CornflowerBlue);

			// Draw the powerups
			foreach (Powerup power in powerUps)
			{
                if (power.PlayerAffected < 0)
				    spriteBatch.Draw(whiteRect, 
					    new Rectangle((int)power.Position.X, (int)power.Position.Y, POWERUP_LENGTH, POWERUP_LENGTH),
					    _powerUpColors[(int)power.Type]);
			}

			// Draw the players
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player.Alive)
                {
                    Texture2D circle = CreateCircle((int)player.Radius);
                    if (i == current)
                        spriteBatch.Draw(circle, player.Position, Color.Red);
                    else
                        spriteBatch.Draw(circle, player.Position, player.Color);
                }
            }

			// Display scores in the top left
			System.Text.StringBuilder text = new System.Text.StringBuilder();
			text.AppendLine("Scores");
			for (int i=0; i<players.Length; i++) {
				text.AppendLine(string.Format("Player {0}: {1}", i+1, players[i].Wins));
			}
			text.AppendLine(string.Format("Total games - {0}", totalGames));
			spriteBatch.DrawString(font, text, new Vector2(10, 10), Color.Green);

			// Show instructions if no player is red (game is in initial peace time)
			if (current < 0)
				spriteBatch.DrawString(fontBig, "Run from red", new Vector2(600, 400), Color.Red);

			// Show paused screen if game is paused
			if (paused)
				spriteBatch.DrawString(fontBig, "Paused", new Vector2(600, 100), Color.Yellow);

			spriteBatch.DrawString(fontBig, "GAME DEV CLUB!", new Vector2(500, 100), Color.Red);

			spriteBatch.End();
			base.Draw(gameTime);
		}

		/// <summary>
		/// Dynamically creates a circle with given radius
		/// </summary>
		/// <param name="radius">Desired radius of circle</param>
		/// <returns>A Texture2D circle colored white for masking</returns>
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

				// Fill in the pixels in between the points. I am using vertical fills here
				for (int i=radius*2 - y; i<=y; i++)
					data[i * outerRadius + x + 1] = Color.White;
			}

			texture.SetData(data);
			return texture;
		}
	}
}
