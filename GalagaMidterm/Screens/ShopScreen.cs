using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;

namespace GalagaMidterm.Screens;

/// <summary>
/// Retro arcade hangar and weapon shop overlay.
/// Players spend collected coins on fighter jet unlocks, multi-shot cannons,
/// rapid-fire boosters, coin magnet, instant shields, and extra lives.
/// </summary>
public class ShopScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;
    private IGameState _gameState;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;
    private Texture2D _pixelTexture;
    private Texture2D _coinIcon;
    private Texture2D[] _shipTextures;

    private int _screenWidth;
    private int _screenHeight;
    private int _selectedIndex = 0;
    private string _statusFeedback = "";
    private Color _feedbackColor = Color.Yellow;
    private float _feedbackTimer = 0f;
    private float _glowTimer = 0f;

    private struct ShopRow
    {
        public string Title;
        public string Subtitle;
        public string PriceText;
        public Action Action;
    }

    public ShopScreen(InputManager input, GameStateManager stateManager)
    {
        _input = input;
        _stateManager = stateManager;
    }

    public void SetGameState(IGameState gameState)
    {
        _gameState = gameState;
    }

    public void LoadContent(AssetLoader assets, GraphicsDevice graphicsDevice)
    {
        _screenWidth = graphicsDevice.Viewport.Width;
        _screenHeight = graphicsDevice.Viewport.Height;

        _fontLarge = assets.GetFont(AssetPaths.FontLarge);
        _fontMedium = assets.GetFont(AssetPaths.FontMedium);
        _fontSmall = assets.GetFont(AssetPaths.FontSmall);
        _coinIcon = assets.GetTexture(AssetPaths.IconCoin);

        _shipTextures = new[]
        {
            assets.GetTexture(AssetPaths.PlayerShip1),
            assets.GetTexture(AssetPaths.PlayerShip2),
            assets.GetTexture(AssetPaths.PlayerShip9)
        };

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _glowTimer += dt;
        if (_feedbackTimer > 0) _feedbackTimer -= dt;

        // Exit shop back to game
        if (_input.Pause || _input.MenuBack || _input.OpenShop)
        {
            _stateManager.PopScreen();
            return;
        }

        var rows = BuildRows();

        if (_input.MenuUp)
        {
            _selectedIndex = (_selectedIndex - 1 + rows.Count) % rows.Count;
        }
        else if (_input.MenuDown)
        {
            _selectedIndex = (_selectedIndex + 1) % rows.Count;
        }

        if (_input.MenuConfirm)
        {
            if (_selectedIndex >= 0 && _selectedIndex < rows.Count)
            {
                AudioManager.Play(Sfx.ShopBuy);
                rows[_selectedIndex].Action?.Invoke();
            }
        }
    }

    private List<ShopRow> BuildRows()
    {
        var rows = new List<ShopRow>();
        var shop = _gameState?.Shop;
        if (shop == null) return rows;

        // 1. Ship 1: Vanguard
        bool s0Equipped = shop.SelectedShip == 0;
        rows.Add(new ShopRow
        {
            Title = "SHIP 1: VANGUARD",
            Subtitle = "Balanced Fighter - Standard Lasers",
            PriceText = s0Equipped ? "[EQUIPPED]" : "[EQUIP]",
            Action = () =>
            {
                shop.TryBuyOrEquipShip(0, out string msg);
                SetFeedback(msg, Color.Cyan);
            }
        });

        // 2. Ship 2: Interceptor
        bool s1Unlocked = shop.UnlockedShips.Contains(1);
        bool s1Equipped = shop.SelectedShip == 1;
        rows.Add(new ShopRow
        {
            Title = "SHIP 2: INTERCEPTOR",
            Subtitle = "+30% Speed - Agile Thrusters",
            PriceText = s1Equipped ? "[EQUIPPED]" : (s1Unlocked ? "[EQUIP]" : $"BUY: {ShopState.Ship2Cost} COINS"),
            Action = () =>
            {
                bool success = shop.TryBuyOrEquipShip(1, out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 3. Ship 9: Titan Dreadnought
        bool s9Unlocked = shop.UnlockedShips.Contains(2);
        bool s9Equipped = shop.SelectedShip == 2;
        rows.Add(new ShopRow
        {
            Title = "SHIP 9: TITAN DREADNOUGHT",
            Subtitle = "Heavy Armor - Free Shield Each Wave!",
            PriceText = s9Equipped ? "[EQUIPPED]" : (s9Unlocked ? "[EQUIP]" : $"BUY: {ShopState.Ship9Cost} COINS"),
            Action = () =>
            {
                bool success = shop.TryBuyOrEquipShip(2, out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 4. Multi-Shot Weapon
        string multiStatus = shop.MultiShotLevel >= 4
            ? "[MAX LV.4]"
            : $"UPGRADE: {ShopState.MultiShotCosts[shop.MultiShotLevel]} COINS";
        string multiSub = shop.MultiShotLevel switch
        {
            1 => "Current: Single -> Next: Twin Lasers",
            2 => "Current: Twin -> Next: Triple Spread",
            3 => "Current: Triple -> Next: Quad Barrage!",
            _ => "Quad Heavy Laser Barrage Active!"
        };
        rows.Add(new ShopRow
        {
            Title = $"MULTI-SHOT CANNON (LV {shop.MultiShotLevel}/4)",
            Subtitle = multiSub,
            PriceText = multiStatus,
            Action = () =>
            {
                bool success = shop.TryUpgradeMultiShot(out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 5. Rapid Fire Rate
        string fireStatus = shop.FireRateLevel >= 4
            ? "[MAX LV.4]"
            : $"UPGRADE: {ShopState.FireRateCosts[shop.FireRateLevel]} COINS";
        string fireSub = shop.FireRateLevel switch
        {
            1 => "Current: Normal -> Next: Fast Pulse",
            2 => "Current: Fast Pulse -> Next: Hyper Cannon",
            3 => "Current: Hyper Cannon -> Next: Gatling Laser!",
            _ => "Plasma Gatling Max Speed Active!"
        };
        rows.Add(new ShopRow
        {
            Title = $"FIRE RATE BOOSTER (LV {shop.FireRateLevel}/4)",
            Subtitle = fireSub,
            PriceText = fireStatus,
            Action = () =>
            {
                bool success = shop.TryUpgradeFireRate(out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 6. Coin Magnet
        rows.Add(new ShopRow
        {
            Title = "COIN MAGNET SYSTEM",
            Subtitle = "Passively draws all floating drops to ship",
            PriceText = shop.HasMagnet ? "[ACTIVE]" : $"BUY: {ShopState.MagnetCost} COINS",
            Action = () =>
            {
                bool success = shop.TryBuyMagnet(out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 7. Emergency Shield
        rows.Add(new ShopRow
        {
            Title = "EMERGENCY SHIELD OVERCHARGE",
            Subtitle = "Deploys an instant 10-second defensive shield",
            PriceText = $"BUY: {ShopState.ShieldCost} COINS",
            Action = () =>
            {
                bool success = shop.TryBuyShield(() =>
                {
                    if (_gameState is AsteroidsGameState ags) ags.DeployShopShield();
                }, out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 8. Repair Hull (+1 Life)
        rows.Add(new ShopRow
        {
            Title = "HULL REINFORCEMENT (+1 LIFE)",
            Subtitle = $"Repairs damage (Current Lives: {_gameState?.Lives}/5)",
            PriceText = $"BUY: {ShopState.LifeCost} COINS",
            Action = () =>
            {
                bool success = shop.TryBuyLife(() =>
                {
                    if (_gameState is AsteroidsGameState ags) return ags.AddLifeFromShop();
                    return false;
                }, out string msg);
                SetFeedback(msg, success ? Color.LightGreen : Color.Tomato);
            }
        });

        // 9. Back to battle
        rows.Add(new ShopRow
        {
            Title = ">> RETURN TO BATTLE <<",
            Subtitle = "Resume action with equipped upgrades",
            PriceText = "[ESC / B]",
            Action = () => _stateManager.PopScreen()
        });

        return rows;
    }

    private void SetFeedback(string text, Color color)
    {
        _statusFeedback = text;
        _feedbackColor = color;
        _feedbackTimer = 2.5f;
    }

    public void Draw(SpriteBatch batch)
    {
        var rows = BuildRows();

        batch.Begin(samplerState: SamplerState.PointClamp);

        // Dark dim backdrop
        batch.Draw(_pixelTexture, new Rectangle(0, 0, _screenWidth, _screenHeight), Color.Black * 0.88f);

        // Header Border Frame
        DrawBorder(batch, new Rectangle(16, 16, _screenWidth - 32, _screenHeight - 32), new Color(0, 180, 255), 2);

        // Header Title
        string title = "CYBER HANGAR & UPGRADE SHOP";
        Vector2 titleSize = _fontMedium.MeasureString(title);
        batch.DrawString(_fontMedium, title, new Vector2((_screenWidth - titleSize.X) / 2f, 26), Color.Gold);

        // Coins Display
        int coins = _gameState?.Coins ?? 0;
        string coinText = $"COINS: {coins}";
        Vector2 coinSize = _fontSmall.MeasureString(coinText);
        float coinX = (_screenWidth - (coinSize.X + 24)) / 2f;
        float coinY = 54;
        if (_coinIcon != null)
        {
            batch.Draw(_coinIcon, new Vector2(coinX, coinY), null, Color.Gold, 0f, Vector2.Zero, 0.14f, SpriteEffects.None, 0f);
        }
        batch.DrawString(_fontSmall, coinText, new Vector2(coinX + 24, coinY), Color.Yellow);

        // Subtitle / Feedback Bar
        if (_feedbackTimer > 0 && !string.IsNullOrEmpty(_statusFeedback))
        {
            Vector2 fbSize = _fontSmall.MeasureString(_statusFeedback);
            batch.DrawString(_fontSmall, _statusFeedback, new Vector2((_screenWidth - fbSize.X) / 2f, 76), _feedbackColor);
        }
        else
        {
            string hint = "[UP/DOWN] Select  [ENTER/SPACE] Purchase/Equip  [B/ESC] Return";
            Vector2 hintSize = _fontSmall.MeasureString(hint);
            batch.DrawString(_fontSmall, hint, new Vector2((_screenWidth - hintSize.X) / 2f, 76), Color.LightGray * 0.8f);
        }

        // Draw Menu Items
        float startY = 100f;
        float rowHeight = 62f;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            float y = startY + i * rowHeight;
            bool isSelected = i == _selectedIndex;

            Rectangle itemRect = new Rectangle(28, (int)y, _screenWidth - 56, (int)rowHeight - 6);

            if (isSelected)
            {
                float pulse = 0.6f + MathF.Sin(_glowTimer * 6f) * 0.2f;
                batch.Draw(_pixelTexture, itemRect, new Color(0, 90, 160) * pulse);
                DrawBorder(batch, itemRect, Color.Cyan, 2);
            }
            else
            {
                batch.Draw(_pixelTexture, itemRect, Color.White * 0.05f);
            }

            // Draw Ship thumbnail if ship row
            if (i < 3 && _shipTextures != null && i < _shipTextures.Length)
            {
                var shipTex = _shipTextures[i];
                batch.Draw(shipTex, new Vector2(itemRect.X + 8, itemRect.Y + 8), null, Color.White, 0f, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
            }

            float textIndent = i < 3 ? 56f : 12f;
            Color titleColor = isSelected ? Color.White : new Color(200, 220, 255);
            batch.DrawString(_fontSmall, row.Title, new Vector2(itemRect.X + textIndent, itemRect.Y + 6), titleColor);

            batch.DrawString(_fontSmall, row.Subtitle, new Vector2(itemRect.X + textIndent, itemRect.Y + 28), Color.Gray * 0.9f);

            // Price or Status Tag (Right Aligned)
            Vector2 priceSize = _fontSmall.MeasureString(row.PriceText);
            Color priceColor = row.PriceText.Contains("EQUIPPED") || row.PriceText.Contains("ACTIVE") || row.PriceText.Contains("MAX")
                ? Color.LightGreen
                : (row.PriceText.Contains("BUY") || row.PriceText.Contains("UPGRADE") ? Color.Gold : Color.Cyan);

            batch.DrawString(_fontSmall, row.PriceText,
                new Vector2(itemRect.Right - priceSize.X - 10, itemRect.Y + 14), priceColor);
        }

        batch.End();
    }

    private void DrawBorder(SpriteBatch batch, Rectangle rect, Color color, int thickness)
    {
        batch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        batch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        batch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        batch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void OnEnter() { }
    public void OnExit() { }
}
