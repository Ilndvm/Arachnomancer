using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "Settings")]
public class Settings : ScriptableObject
{
    [Header("Enemy Types")]
    [SerializeField] public Upgrade[] UpgradeArray;

    [System.Serializable]
    public class Upgrade
    {
        public bool isChosen = false;
        public UpgradeType upgradeType;
        public bool isUnique;
        public string description;
        public WebDrawCoordinator.Pattern pattern; 

        public Upgrade(UpgradeType upgradeType, bool isUnique, string description, WebDrawCoordinator.Pattern pattern)
        {
            this.upgradeType = upgradeType;
            this.isUnique = isUnique;
            this.description = description;
            this.pattern = pattern;
        }
        public bool CanBeChosen()
        {
            return !(isUnique && isChosen);
        }
    }

    public enum UpgradeType
    {
        BonusHP,
        BonusSpeed,
        BonusFireRate,
        BonusDamage,
        BonusLuck,
        Regeneration,
        LifeSteal,
        Slowness,
        Ricochet,
        Shield,
        Explosion,
        Poison,
        Magnet,
        Fireball
    }
    public Upgrade GetUpgrade(UpgradeType upgradeType)
    {
        foreach (Upgrade upgrade in UpgradeArray)
        {
            if (upgrade.upgradeType == upgradeType)
            {
                return new Upgrade(upgrade.upgradeType, upgrade.isUnique, upgrade.description, upgrade.pattern);
            }
        }
        return null;
    }
}