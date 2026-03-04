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
    Last,
    Close,
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
    public const int StartGold = 200;

    // Physics Layers
    public const string LayerEnemy = "Enemy";
    public const string LayerTower = "Tower";
    public const string LayerItem = "Item";

    // Sorting Layers
    public const string SortBackground = "Background";
    public const string SortPath = "Path";
    public const string SortTower = "Tower";
    public const string SortItem = "Item";
    public const string SortEnemy = "Enemy";
    public const string SortProjectile = "Projectile";
    public const string SortUI = "UI";
    public const string SortPreview = "Preview";

    // Wave Scaling
    public const float WaveScalingRate = 1.25f;
    public const int WaveBaseBudget = 400;
    public const float WaveBaseDuration = 30f;
    public const float WaveMinInterval = 0.02f;

    // Wave Timing
    public const float PostSpawnTimeout = 15f;
    public const float PrepareDuration = 20f;
    public const int EarlyStartBonusMax = 50;

    // Tower
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
    public const float EnhancedSpeedMultiplier = 1.5f;
    public const float EnhancedStatusResistance = 0.5f;

    // Shield
    public const int DefaultShieldHp = 10;
    public const int ShieldReward = 5;

    // Status Effects (고정 수치)
    public const int BurnDamagePerTick = 1;
    public const float BurnTickInterval = 0.5f;
    public const float SlowSpeedMultiplier = 0.5f;

    // Enemy Speed
    public const float EnemyMinBaseSpeed = 0.75f;
    public const float EnemyMaxBaseSpeed = 2.0f;
    public const int EnemySpeedMaxWave = 40;
    public const float EnemySpeedPerLayer = 0.75f;

    public static float GetEnemySpeed(EnemyLayer layer, int waveNumber)
    {
        float t = (float)(waveNumber - 1) / (EnemySpeedMaxWave - 1);
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
        float baseSpeed = EnemyMinBaseSpeed + (EnemyMaxBaseSpeed - EnemyMinBaseSpeed) * t;
        return baseSpeed + ((int)layer - 1) * EnemySpeedPerLayer;
    }

    // UI Colors
    public static readonly UnityEngine.Color UIColorNormal = new(0.3f, 0.3f, 0.4f);
    public static readonly UnityEngine.Color UIColorActive = new(0.2f, 0.6f, 0.3f);
    public static readonly UnityEngine.Color UIColorLocked = new(0.5f, 0.5f, 0.5f);

    // UI Values
    public const float HoldDuration = 0.5f;
    public const float DetailCardGap = 18f;
}
