using System;
using System.Collections.Generic;

namespace GalagaMidterm.Core;

public enum ShopItemCategory
{
    Ship,
    Weapons,
    Utility
}

public class ShopItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Cost { get; set; }
    public ShopItemCategory Category { get; set; }
    public Func<ShopState, bool> IsUnlocked { get; set; }
    public Func<ShopState, bool> IsEquipped { get; set; }
    public Func<ShopState, bool> CanUpgrade { get; set; }
    public Func<ShopState, string> GetStatusText { get; set; }
}

/// <summary>
/// Manages player coin currency, unlocked ships, weapon upgrades (multi-shot, fire rate),
/// and utilities (coin magnet, shields, extra lives).
/// </summary>
public class ShopState
{
    public int Coins { get; set; } = 0;
    public int SelectedShip { get; set; } = 0; // 0 = Ship_1, 1 = Ship_2, 2 = Ship_9
    public HashSet<int> UnlockedShips { get; } = new() { 0 };

    public int FireRateLevel { get; set; } = 1; // 1 to 4
    public int MultiShotLevel { get; set; } = 1; // 1 to 4
    public bool HasMagnet { get; set; } = false;

    // Costs
    public static readonly int[] FireRateCosts = { 0, 5, 10, 18 };
    public static readonly int[] MultiShotCosts = { 0, 8, 15, 25 };
    public const int Ship2Cost = 10;
    public const int Ship9Cost = 20;
    public const int MagnetCost = 12;
    public const int ShieldCost = 5;
    public const int LifeCost = 10;

    public bool TryBuyOrEquipShip(int shipIndex, out string message)
    {
        if (UnlockedShips.Contains(shipIndex))
        {
            SelectedShip = shipIndex;
            message = $"EQUIPPED SHIP {GetShipName(shipIndex)}!";
            return true;
        }

        int cost = shipIndex == 1 ? Ship2Cost : Ship9Cost;
        if (Coins >= cost)
        {
            Coins -= cost;
            UnlockedShips.Add(shipIndex);
            SelectedShip = shipIndex;
            message = $"PURCHASED & EQUIPPED {GetShipName(shipIndex)}!";
            return true;
        }

        message = $"NEED {cost - Coins} MORE COINS!";
        return false;
    }

    public bool TryUpgradeFireRate(out string message)
    {
        if (FireRateLevel >= 4)
        {
            message = "FIRE RATE AT MAX LEVEL!";
            return false;
        }

        int cost = FireRateCosts[FireRateLevel];
        if (Coins >= cost)
        {
            Coins -= cost;
            FireRateLevel++;
            message = $"FIRE RATE UPGRADED TO LV {FireRateLevel}!";
            return true;
        }

        message = $"NEED {cost - Coins} MORE COINS!";
        return false;
    }

    public bool TryUpgradeMultiShot(out string message)
    {
        if (MultiShotLevel >= 4)
        {
            message = "MULTI-SHOT AT MAX LEVEL!";
            return false;
        }

        int cost = MultiShotCosts[MultiShotLevel];
        if (Coins >= cost)
        {
            Coins -= cost;
            MultiShotLevel++;
            message = $"MULTI-SHOT UPGRADED TO LV {MultiShotLevel}!";
            return true;
        }

        message = $"NEED {cost - Coins} MORE COINS!";
        return false;
    }

    public bool TryBuyMagnet(out string message)
    {
        if (HasMagnet)
        {
            message = "COIN MAGNET ALREADY ACTIVE!";
            return false;
        }

        if (Coins >= MagnetCost)
        {
            Coins -= MagnetCost;
            HasMagnet = true;
            message = "COIN MAGNET ACTIVATED!";
            return true;
        }

        message = $"NEED {MagnetCost - Coins} MORE COINS!";
        return false;
    }

    public bool TryBuyShield(Action grantShield, out string message)
    {
        if (Coins >= ShieldCost)
        {
            Coins -= ShieldCost;
            grantShield?.Invoke();
            message = "DEFENSIVE SHIELD DEPLOYED!";
            return true;
        }

        message = $"NEED {ShieldCost - Coins} MORE COINS!";
        return false;
    }

    public bool TryBuyLife(Func<bool> addLife, out string message)
    {
        if (Coins >= LifeCost)
        {
            if (addLife?.Invoke() == true)
            {
                Coins -= LifeCost;
                message = "HULL REPAIRED (+1 LIFE)!";
                return true;
            }
            message = "LIVES ALREADY AT MAXIMUM!";
            return false;
        }

        message = $"NEED {LifeCost - Coins} MORE COINS!";
        return false;
    }

    public string GetShipName(int index) => index switch
    {
        1 => "INTERCEPTOR (AGILE)",
        2 => "TITAN (HEAVY)",
        _ => "VANGUARD (BALANCED)"
    };
}
