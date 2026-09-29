using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Audio;
using GalagaMidterm.Core;
using GalagaMidterm.MockState;
using GalagaMidterm.Screens;

namespace GalagaMidterm;

/// <summary>
/// Main Game class. Initializes the window, loads all content,
/// wires up the screen state machine, and dispatches Update/Draw.
///
/// MOCK DATA INJECTION SITE:
///   The MockGameState is created here and injected into the GameplayScreen.
///   When the backend team delivers a real IGameState, replace the mock
///   instantiation below — all rendering code stays unchanged.
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
    private AudioManager _audio;

    // ── Mock state (the ONLY place mock is referenced) ──────
    private MockGameState _mockState;

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
        _audio = new AudioManager();

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


        // Initialize audio
        _audio.Initialize(Content);

        // Initialize screen manager
        _screenManager.Initialize(GraphicsDevice);

        // ────────────────────────────────────────────────────
        // MOCK DATA SETUP — Replace this block for real backend
        // ────────────────────────────────────────────────────
        _mockState = new MockGameState();
        _mockState.Initialize(SCREEN_WIDTH, SCREEN_HEIGHT);
        // ────────────────────────────────────────────────────

        // Create and register all screens (pass GameAssets for typed access)
        var mainMenu = new MainMenuScreen(_input, _screenManager);
        mainMenu.LoadContent(_assets, GraphicsDevice);

        var gameplay = new GameplayScreen(_input, _screenManager, _camera);
        gameplay.LoadContent(_assets, GraphicsDevice);
        gameplay.SetGameState(_mockState); // ← Inject data source (mock or real)

        var pause = new PauseScreen(_input, _screenManager);
        pause.LoadContent(_assets, GraphicsDevice);

        var gameOver = new GameOverScreen(_input, _screenManager);
        gameOver.LoadContent(_assets, GraphicsDevice);

        _screenManager.RegisterScreen("mainmenu", mainMenu);
        _screenManager.RegisterScreen("gameplay", gameplay);
        _screenManager.RegisterScreen("pause", pause);
        _screenManager.RegisterScreen("gameover", gameOver);

        // Start at main menu
        _screenManager.SwitchTo("mainmenu");
    }

    protected override void Update(GameTime gameTime)
    {
        // Update input first (before anything reads it)
        _input.Update();

        // Update mock state (presentation animation driver)
        // When using a real backend, this line is replaced by backend.Update()
        _mockState?.Update(gameTime);

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
