using UnityEngine;

public enum EnemyLayer
{
    None = 0,
    Red = 1,
    Orange = 2,
    Yellow = 3,
    Green = 4,
    Blue = 5,
    Indigo = 6,
    Purple = 7
}

public enum TargetPriority
{
    First,
    Close,
    Weak,
    Strong
}

public enum GameState
{
    Prepare,
    Wave,
    Clear,
    GameOver
}

public enum EnemyVariant
{
    Normal,
    Shielded,
    Enhanced,
    EnhancedShielded
}

public enum StatusEffectType
{
    None,
    Burn,
    Slow,
    Stun
}

public static class GameConstants
{
    public const int StartLives = 20;
    public const int StartEnergy = 200;

    // Physics Layers
    public const string LayerEnemy = "Enemy";
    public const string LayerTower = "Tower";
    public const string LayerItem = "Item";

    // Sorting Layers
    public const string SortPath = "Path";
    public const string SortTower = "Tower";
    public const string SortItem = "Item";
    public const string SortEnemy = "Enemy";
    public const string SortProjectile = "Projectile";

    // Wave Timing
    public const float PostSpawnTimeout = 15f;
    public const float PrepareDuration = 20f;
    public const int EarlyStartBonusMax = 50;

    // Tower
    public const float PlacementCooldown = 3f;
    public const float SellRefundRate = 0.5f;

    // 보상
    public const int BaseLayerReward = 1;
    public const int WaveClearRewardBase = 5;

    public static int GetWaveClearReward(int waveNumber)
    {
        return WaveClearRewardBase * waveNumber;
    }

    // Enhanced 배율
    public const int EnhancedMultiplier = 2;
    public const float EnhancedSpeedMultiplier = 1.2f;

    // Shield
    public const int DefaultShieldHp = 10;

    // Status Effects (고정 수치)
    public const int BurnDamagePerTick = 1;
    public const float BurnTickInterval = 0.5f;
    public const float SlowSpeedMultiplier = 0.5f;

    public static float GetEnemySpeed(EnemyLayer layer)
    {
        return 1.0f + ((int)layer - 1) * 0.5f;
    }

    public static Color GetEnemyColor(EnemyLayer layer)
    {
        return layer switch
        {
            EnemyLayer.Red => Color.red,
            EnemyLayer.Orange => new Color(1f, 0.5f, 0f),
            EnemyLayer.Yellow => Color.yellow,
            EnemyLayer.Green => Color.green,
            EnemyLayer.Blue => Color.blue,
            EnemyLayer.Indigo => new Color(0.29f, 0f, 0.51f),
            EnemyLayer.Purple => new Color(0.58f, 0f, 0.83f),
            _ => Color.white
        };
    }
}
