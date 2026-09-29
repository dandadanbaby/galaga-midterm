using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;

namespace GalagaMidterm.Screens;

/// <summary>
/// Interface for all game screens (menu, gameplay, pause, game over).
/// Each screen manages its own rendering and input handling.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Called once when the screen's assets need to be loaded.
    /// </summary>
    void LoadContent(AssetLoader assets, GraphicsDevice graphicsDevice);

    /// <summary>
    /// Called every frame for logic updates.
    /// </summary>
    void Update(GameTime gameTime);

    /// <summary>
    /// Called every frame for rendering.
    /// </summary>
    void Draw(SpriteBatch spriteBatch);

    /// <summary>
    /// Called when this screen becomes the active screen (transition in).
    /// </summary>
    void OnEnter();

    /// <summary>
    /// Called when this screen is being left (transition out / cleanup).
    /// </summary>
    void OnExit();
}
