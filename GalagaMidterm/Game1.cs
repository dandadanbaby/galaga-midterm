using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.Screens;

namespace GalagaMidterm;

/// <summary>
/// Main Game class. Initializes the window, loads all content,
/// wires up the screen state machine, and dispatches Update/Draw.
///
/// BACKEND INTEGRATION:
///   The AsteroidsGameState replaces the old MockGameState.
///   It ports the Java game logic (from Main.java) into C#,
///   adapted to work with the existing MonoGame front-end.
/// </summary>
public class GalagaGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // ── Core systems ───────────────────────────────────────
    private AssetLoader _assets;
    private GameAssets _gameAssets;
    private InputManager _input;
    private GameStateManager _screenManager;
    private Camera2D _camera;

    // ── Real game state ────────────────────────────────────
    private AsteroidsGameState _gameState;

    // ── Screen dimensions (portrait arcade ratio) ──────────
    public const int SCREEN_WIDTH = 480;
    public const int SCREEN_HEIGHT = 720;

    public GalagaGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        // Configure window
        _graphics.PreferredBackBufferWidth = SCREEN_WIDTH;
        _graphics.PreferredBackBufferHeight = SCREEN_HEIGHT;
        _graphics.ApplyChanges();

        Window.Title = "GALAGA - Midterm Edition";
        Window.AllowUserResizing = false;

        // Initialize core systems
        _input = new InputManager();
        _camera = new Camera2D();
        _screenManager = new GameStateManager();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // ── Step 1: Load raw assets via content pipeline ───
        _assets = new AssetLoader(Content);
        _assets.LoadAll();

        // ── Step 2: Build typed asset façade ───────────────
        _gameAssets = new GameAssets(_assets);
        _gameAssets.Initialize(GraphicsDevice);

        // ── Step 3: Validate all assets loaded correctly ───
        var validation = AssetManifest.Validate(_assets, _gameAssets);
        AssetManifest.LogResults(validation);
        if (!validation.AllAssetsPresent)
        {
            Debug.WriteLine("WARNING: Some assets failed to load. See manifest above.");
        }
        AssetDiagnostics.RunFullDiagnostic(_gameAssets);

        // Initialize screen manager
        _screenManager.Initialize(GraphicsDevice);

        // ────────────────────────────────────────────────────
        // REAL GAME STATE SETUP
        // ────────────────────────────────────────────────────
        _gameState = new AsteroidsGameState();
        _gameState.Initialize(SCREEN_WIDTH, SCREEN_HEIGHT, _input);
        // ────────────────────────────────────────────────────

        // Create and register all screens
        var mainMenu = new MainMenuScreen(_input, _screenManager);
        mainMenu.LoadContent(_assets, GraphicsDevice);

        var gameplay = new GameplayScreen(_input, _screenManager, _camera);
        gameplay.LoadContent(_assets, GraphicsDevice);
        gameplay.SetGameState(_gameState); // ← Inject real game state

        var pause = new PauseScreen(_input, _screenManager);
        pause.LoadContent(_assets, GraphicsDevice);

        var gameOver = new GameOverScreen(_input, _screenManager);
        gameOver.LoadContent(_assets, GraphicsDevice);

        var controls = new ControlsScreen(_input, _screenManager);
        controls.LoadContent(_assets, GraphicsDevice);

        var intro = new IntroScreen(_input, _screenManager);
        intro.LoadContent(_assets, GraphicsDevice);

        var victory = new VictoryScreen(_input, _screenManager);
        victory.LoadContent(_assets, GraphicsDevice);

        var shop = new ShopScreen(_input, _screenManager);
        shop.LoadContent(_assets, GraphicsDevice);
        shop.SetGameState(_gameState);

        _screenManager.RegisterScreen("mainmenu", mainMenu);
        _screenManager.RegisterScreen("gameplay", gameplay);
        _screenManager.RegisterScreen("pause", pause);
        _screenManager.RegisterScreen("gameover", gameOver);
        _screenManager.RegisterScreen("controls", controls);
        _screenManager.RegisterScreen("intro", intro);
        _screenManager.RegisterScreen("victory", victory);
        _screenManager.RegisterScreen("shop", shop);

        // Start at main menu
        _screenManager.SwitchTo("mainmenu");
    }

    protected override void Update(GameTime gameTime)
    {
        // Update input first (before anything reads it)
        _input.Update();

        // Game state is updated by GameplayScreen.Update() — NOT here.
        // This ensures pausing (pushing PauseScreen overlay) naturally freezes gameplay.

        // Update screen state machine
        _screenManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Draw the current screen + transition overlay
        _screenManager.Draw(_spriteBatch, SCREEN_WIDTH, SCREEN_HEIGHT);

        base.Draw(gameTime);
    }
}
