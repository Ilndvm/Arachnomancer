using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField] private Settings settings;
    [SerializeField] private SpiderController player;

    // Runtime state
    private HashSet<Settings.UpgradeType> appliedUnique = new HashSet<Settings.UpgradeType>();
    private Dictionary<Settings.UpgradeType, int> stacks = new Dictionary<Settings.UpgradeType, int>();

    [Header("Stack values (per stack)")]
    [SerializeField] private float hpPerStack = 10f;
    [SerializeField] private float speedMultiplierPerStack = 0.05f;    // additive multiplier per stack (1 + n * val)
    [SerializeField] private float fireRateMultiplierPerStack = 0.10f; // additive fraction to multiply fireRate by (1 + n * val)
    [SerializeField] private int damagePerStack = 1;
    [SerializeField] private int luckPerStack = 1;
    [SerializeField] private float magnetRadiusPerStack = 1.5f;

    [Header("Unique effects")]
    [SerializeField] public float lifeStealPercent = 0.15f; // 15% life steal when LifeSteal applied
    [SerializeField] public float slowness = 0.5f; // 0.5 half speed for enemy
    [SerializeField] public float slownessTime = 5f;
    [SerializeField] public int poison = 1; // poison damage every sec
    [SerializeField] public float poisonTime = 5f;
    [SerializeField] public float explosionRadius = 1.5f; // used by Explosion unique (gameplay must use this)
    [SerializeField] public float shield = 10f;



    // Event so UI / other systems can react
    public event Action OnUpgradesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryUpgrade(Settings.UpgradeType upgrade)
    {
        var data = settings.GetUpgrade(upgrade);

        if (data.isUnique)
        {
            if (appliedUnique.Contains(upgrade))
            {
                // already applied, reject
                return false;
            }

            // apply uniqueness
            appliedUnique.Add(upgrade);

            // run any one-off logic (spawn visuals, immediately give shield, etc.)
            ApplyUniqueEffect(upgrade);
        }
        else
        {
            // stackable: increment count
            if (!stacks.ContainsKey(upgrade)) stacks[upgrade] = 0;
            stacks[upgrade] += 1;

            // optional: immediate side-effects for some upgrades (e.g., grant a small regen buff instantly)
            ApplyStackEffect(upgrade);
        }

        // notify listeners
        OnUpgradesChanged?.Invoke();
        return true;
    }

    // Add at top of file if missing:
    // using System.Text;

    public string GetStackableUpgradesString()
    {
        if (settings == null) return string.Empty;

        var sb = new StringBuilder();

        foreach (Settings.UpgradeType t in Enum.GetValues(typeof(Settings.UpgradeType)))
        {
            var def = settings.GetUpgrade(t);
            if (def == null) continue;

            // skip uniques, only show stackable upgrades
            if (def.isUnique) continue;

            int count = GetStackCount(t);
            sb.AppendLine($"{t}: {count}");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string GetUniqueUpgradesString()
    {
        if (settings == null) return string.Empty;

        var sb = new StringBuilder();

        foreach (Settings.UpgradeType t in Enum.GetValues(typeof(Settings.UpgradeType)))
        {
            var def = settings.GetUpgrade(t);
            if (def == null) continue;

            // skip stackables, only show uniques
            if (!def.isUnique) continue;

            bool haveIt = HasUnique(t);
            sb.AppendLine($"{t}: {(haveIt ? "O" : "X")}");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }


    public float GetBonusHP()
    {
        return GetStackCount(Settings.UpgradeType.HP) * hpPerStack;
    }

    public float GetSpeedMultiplier()
    {
        int stacksCount = GetStackCount(Settings.UpgradeType.Speed);
        return 1f + stacksCount * speedMultiplierPerStack;
    }

    public float GetFireRateMultiplier()
    {
        int stacksCount = GetStackCount(Settings.UpgradeType.FireRate);
        return 1f + stacksCount * fireRateMultiplierPerStack;
    }

    public int GetDamageBonus()
    {
        return GetStackCount(Settings.UpgradeType.Damage) * damagePerStack;
    }
    public int GetLuckBonus()
    {
        return GetStackCount(Settings.UpgradeType.Luck) * luckPerStack;
    }
    public float GetMagnetBonus()
    {
        return GetStackCount(Settings.UpgradeType.Magnet) * magnetRadiusPerStack;

    }

    public bool HasUnique(Settings.UpgradeType t) => appliedUnique.Contains(t);
    public int GetStackCount(Settings.UpgradeType t) => stacks.TryGetValue(t, out var n) ? n : 0;

    // convenience boolean flags
    public bool HasRegeneration => HasUnique(Settings.UpgradeType.Regeneration);
    public bool HasLifeSteal => HasUnique(Settings.UpgradeType.LifeSteal);
    public bool HasSlowness => HasUnique(Settings.UpgradeType.Slowness);
    public bool HasRicochet => HasUnique(Settings.UpgradeType.Ricochet);
    public bool HasShield => HasUnique(Settings.UpgradeType.Shield);
    public bool HasExplosion => HasUnique(Settings.UpgradeType.Explosion);
    public bool HasTwoTargets => HasUnique(Settings.UpgradeType.TwoTargets);
    public bool HasPoison => HasUnique(Settings.UpgradeType.Poison);

    private void ApplyUniqueEffect(Settings.UpgradeType t)
    {
        //switch (t)
        //{
        //    case Settings.UpgradeType.Shield:
        //        // Provide shield HP to the player. Game systems should call GetShieldHP() to read how much.
        //        // Optionally: give the player an initial shield fill (TODO: call player's method to grant shield).
        //        // e.g. PlayerShieldComponent.ApplyShield(GetShieldHP());
        //        Debug.Log("[UpgradeManager] Shield acquired.");
        //        break;

        //    case Settings.UpgradeType.LifeSteal:
        //        // enables life steal behavior; projectiles or damage system should query GetLifeStealPercent().
        //        Debug.Log("[UpgradeManager] LifeSteal acquired.");
        //        break;

        //    case Settings.UpgradeType.Ricochet:
        //        Debug.Log("[UpgradeManager] Ricochet acquired.");
        //        break;

        //    case Settings.UpgradeType.Fireball:
        //        Debug.Log("[UpgradeManager] Fireball acquired.");
        //        break;

        //    default:
        //        // Many unique upgrades just set the flag; game code should query them.
        //        Debug.Log($"[UpgradeManager] Applied unique upgrade: {t}");
        //        break;
        //}
    }

    private void ApplyStackEffect(Settings.UpgradeType t)
    {
        // Called when a stackable upgrade receives +1 stack — allow immediate effects if desired.
        switch (t)
        {
            case Settings.UpgradeType.HP:
                player.UpdateMaxHP();
                break;
            
            case Settings.UpgradeType.Magnet:
                player.UpdateMagnet();
                break;
            default:
                break;
        }
    }

    [ContextMenu("Clear All Upgrades")]
    public void ClearAllUpgrades()
    {
        appliedUnique.Clear();
        stacks.Clear();
        OnUpgradesChanged?.Invoke();
    }
}