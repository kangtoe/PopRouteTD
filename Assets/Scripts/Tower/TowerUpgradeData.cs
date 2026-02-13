using UnityEngine;

public enum UpgradeTrack { None, A, B }

[System.Serializable]
public class StatusEffectEntry
{
    public StatusEffectType type;
    public float duration;
}

[System.Serializable]
public class UpgradeLevel
{
    public string levelName;
    [TextArea] public string description;
    public int cost;

    [Header("Combat")]
    public float attackDamage;
    public float attackInterval;
    public float attackRange;
    public float splashRadius;
    public int pierceCount;

    [Header("Status Effect")]
    public StatusEffectEntry[] statusEffects;
}

[CreateAssetMenu(fileName = "NewTowerUpgrade", menuName = "Tower/UpgradeData")]
public class TowerUpgradeData : ScriptableObject
{
    public string towerName;

    [Header("Main Module (Lv1~Lv4)")]
    public UpgradeLevel main1;
    public UpgradeLevel main2;
    public UpgradeLevel main3;
    public UpgradeLevel main4;

    [Header("Sub Module (Pick One)")]
    public UpgradeLevel subA;
    public UpgradeLevel subB;
}
