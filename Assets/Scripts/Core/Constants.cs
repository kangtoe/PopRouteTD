using UnityEngine;

public enum BalloonLayer
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

public static class GameConstants
{
    public const int StartLives = 20;
    public const int StartEnergy = 100;

    // Physics Layers
    public const string LayerEnemy = "Enemy";
    public const string LayerTower = "Tower";

    // Sorting Layers
    public const string SortPath = "Path";
    public const string SortTower = "Tower";
    public const string SortEnemy = "Enemy";
    public const string SortProjectile = "Projectile";

    // Wave Timing
    public const float PostSpawnTimeout = 15f;
    public const float PrepareDuration = 20f;
    public const int EarlyStartBonusMax = 50;

    // Tower Placement
    public const float PlacementCooldown = 3f;

    public static float GetBalloonSpeed(BalloonLayer layer)
    {
        return 1.0f + ((int)layer - 1) * 0.5f;
    }

    public static Color GetBalloonColor(BalloonLayer layer)
    {
        return layer switch
        {
            BalloonLayer.Red => Color.red,
            BalloonLayer.Orange => new Color(1f, 0.5f, 0f),
            BalloonLayer.Yellow => Color.yellow,
            BalloonLayer.Green => Color.green,
            BalloonLayer.Blue => Color.blue,
            BalloonLayer.Indigo => new Color(0.29f, 0f, 0.51f),
            BalloonLayer.Purple => new Color(0.58f, 0f, 0.83f),
            _ => Color.white
        };
    }
}
