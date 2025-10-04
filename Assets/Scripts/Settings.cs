using UnityEngine;

//[CreateAssetMenu(fileName = "Settings", menuName = "Settings")]
//public class Settings : ScriptableObject
//{
//    [Header("Enemy Types")]
//    [SerializeField] public Upgrade[] UpgradesArray;

//    [System.Serializable]
//    public class Upgrade
//    {
//        public UpgradeType enemyType;
//        public int maxHP = 0;
//        public int pointValue = 0;
//        public float sLimit = 0;

//        public Upgrade(UpgradeType upgradeType, int maxHP, int pointValue, float sLimit)
//        {
//            this.enemyType = enemyType;
//            this.maxHP = maxHP;
//            this.pointValue = pointValue;
//            this.sLimit = sLimit;
//        }
//    }

//    public enum UpgradeType
//    {
//        RamNova,
//        MechNova,
//        FireNova,
//        MineNova,
//        PulsarNova,
//        DarkNova,
//        SuperNova,
//        UltraNova,
//        BossNova
//    }
//    public Upgrade GetEnemy(UpgradeType enemyType)
//    {
//        foreach (Enemy enemy in EnemyArray)
//        {
//            if (enemy.enemyType == enemyType)
//            {
//                return new Enemy(enemy.enemyType, enemy.maxHP, enemy.pointValue, enemy.sLimit);
//            }
//        }
//        return null;
//    }
//}
