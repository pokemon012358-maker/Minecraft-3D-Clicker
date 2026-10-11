using System;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ dữ liệu của người chơi: Tiền (Money) và các chỉ số Pickaxe
/// (Rarity, Damage %, Damage xN, Money %). Enchant và Rebirth CHƯA xử lý logic ở bản này.
///
/// CÁCH GẮN (kéo-thả):
/// 1. Tạo 1 GameObject rỗng trong Hierarchy, đặt tên "_GameManager".
/// 2. Kéo file script này vào GameObject đó (Add Component).
/// 3. Không cần kéo gì thêm vào Inspector cho script này — mọi field đều có giá trị mặc định,
///    bạn có thể chỉnh số liệu (baseCost, % mỗi cấp...) trực tiếp trên Inspector nếu muốn.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Tiền tệ")]
    [SerializeField] private float money = 0f;

    [Header("Pickaxe - Rarity (tên các tier theo thứ tự, index 0 = chưa có Pickaxe)")]
    [SerializeField]
    private string[] pickaxeTierNames = { "None", "Wood", "Stone", "Iron", "Gold" };
    [SerializeField] private int pickaxeRarityLevel = 0; // index trong pickaxeTierNames

    [Tooltip("Sát thương GỐC (chưa cộng % / xN) ứng với từng Tier ở trên, PHẢI cùng độ dài với Pickaxe Tier Names. " +
             "None=1, Wood=2, Stone=3, Iron=4, Gold=5")]
    [SerializeField]
    private float[] pickaxeDamagePerTier = { 1f, 2f, 3f, 4f, 5f };

    [Header("Pickaxe - Damage % (cộng dồn)")]
    [SerializeField] private int damagePercentLevel = 0;
    [SerializeField] private int maxDamagePercentLevel = 5;
    [SerializeField] private float damagePercentPerLevel = 10f; // mỗi cấp +10%

    [Header("Pickaxe - Damage Multiplier (1x, 2x, 3x...)")]
    [SerializeField] private int damageMultiplierLevel = 0;
    [SerializeField] private int maxDamageMultiplierLevel = 5;

    [Header("Pickaxe - Money % (cộng dồn)")]
    [SerializeField] private int moneyPercentLevel = 0;
    [SerializeField] private int maxMoneyPercentLevel = 5;
    [SerializeField] private float moneyPercentPerLevel = 10f; // mỗi cấp +10%

    // ===== EVENTS để các script UI khác tự cập nhật khi dữ liệu thay đổi =====
    public event Action<float> OnMoneyChanged;
    public event Action<string> OnPickaxeNameChanged;
    public event Action OnRebirthReset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Bắn event 1 lần lúc khởi động để UI hiển thị đúng giá trị ban đầu
        OnMoneyChanged?.Invoke(money);
        OnPickaxeNameChanged?.Invoke(GetPickaxeName());
    }

    // ---------- MONEY ----------
    public float Money => money;

    public void AddMoney(float amount)
    {
        if (amount <= 0f) return;

        // % Money từ Upgrade (mua bằng tiền) CỘNG với % Money Buff từ Enchant "Fortunate" (nếu đang sở hữu)
        float enchantMoneyPercent = EnchantManager.Instance != null ? EnchantManager.Instance.GetMoneyBonusPercent() : 0f;
        float totalMoneyPercent = (moneyPercentLevel * moneyPercentPerLevel) + enchantMoneyPercent;

        float finalAmount = amount * (1f + totalMoneyPercent / 100f);

        if (RebirthShopManager.Instance != null &&
        RebirthShopManager.Instance.HasX2Money)
        {
            finalAmount *= 2f;
        }
        money += finalAmount;
        OnMoneyChanged?.Invoke(money);
    }

    /// <summary>Trừ tiền nếu đủ, trả về true nếu thành công (dùng khi mua Upgrade / mua Block).</summary>
    public bool TrySpendMoney(float amount)
    {
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    // ---------- PICKAXE RARITY ----------
    public int PickaxeRarityLevel => pickaxeRarityLevel;
    public int MaxPickaxeRarityLevel => pickaxeTierNames.Length - 1;

    public string GetPickaxeName() =>
        pickaxeTierNames[Mathf.Clamp(pickaxeRarityLevel, 0, pickaxeTierNames.Length - 1)];

    /// <summary>Tên tier kế tiếp, dùng để hiển thị kiểu "Wood --> Stone" trên nút Upgrade.</summary>
    public string GetNextPickaxeName()
    {
        int next = Mathf.Clamp(pickaxeRarityLevel + 1, 0, pickaxeTierNames.Length - 1);
        return pickaxeTierNames[next];
    }

    public bool UpgradePickaxeRarity()
    {
        if (pickaxeRarityLevel >= MaxPickaxeRarityLevel) return false;
        pickaxeRarityLevel++;
        OnPickaxeNameChanged?.Invoke(GetPickaxeName());
        return true;
    }

    /// <summary>Sát thương gốc theo Tier Pickaxe hiện tại (None=1, Wood=2, Stone=3, Iron=4, Gold=5).</summary>
    public float GetPickaxeBaseDamage()
    {
        int index = Mathf.Clamp(pickaxeRarityLevel, 0, pickaxeDamagePerTier.Length - 1);
        return pickaxeDamagePerTier[index];
    }

    /// <summary>Sát thương gốc của Tier kế tiếp — dùng để hiển thị "1 dmg --> 2 dmg" trên nút Upgrade nếu cần.</summary>
    public float GetNextPickaxeBaseDamage()
    {
        int index = Mathf.Clamp(pickaxeRarityLevel + 1, 0, pickaxeDamagePerTier.Length - 1);
        return pickaxeDamagePerTier[index];
    }

    // ---------- DAMAGE % ----------
    public int DamagePercentLevel => damagePercentLevel;
    public int MaxDamagePercentLevel => maxDamagePercentLevel;
    public float DamagePercentPerLevel => damagePercentPerLevel;
    public float CurrentDamagePercent => damagePercentLevel * damagePercentPerLevel;

    public bool UpgradeDamagePercent()
    {
        if (damagePercentLevel >= maxDamagePercentLevel) return false;
        damagePercentLevel++;
        return true;
    }

    // ---------- DAMAGE MULTIPLIER ----------
    public int DamageMultiplierLevel => damageMultiplierLevel;
    public int MaxDamageMultiplierLevel => maxDamageMultiplierLevel;
    public float CurrentDamageMultiplier => 1f + damageMultiplierLevel; // 1x,2x,3x...

    public bool UpgradeDamageMultiplier()
    {
        if (damageMultiplierLevel >= maxDamageMultiplierLevel) return false;
        damageMultiplierLevel++;
        return true;
    }

    // ---------- MONEY % ----------
    public int MoneyPercentLevel => moneyPercentLevel;
    public int MaxMoneyPercentLevel => maxMoneyPercentLevel;
    public float MoneyPercentPerLevel => moneyPercentPerLevel;

    public bool UpgradeMoneyPercent()
    {
        if (moneyPercentLevel >= maxMoneyPercentLevel) return false;
        moneyPercentLevel++;
        return true;
    }

    // ---------- TỔNG HỢP SÁT THƯƠNG CUỐI CÙNG ----------
    /// <summary>Kết quả của lần click gần nhất có phải Crit hay không — UI có thể đọc để hiện hiệu ứng (vd: chữ "CRIT!").</summary>
    public bool LastHitWasCrit { get; private set; }

    /// <summary>
    /// Sát thương mỗi click gây ra: lấy sát thương gốc theo Pickaxe Tier hiện tại
    /// (None=1, Wood=2, Stone=3, Iron=4, Gold=5...), cộng thêm % Damage (Upgrade) + % DMG Buff
    /// từ Enchant "Sharpness" (nếu có), nhân với hệ số xN đã mua, sau đó roll Crit Chance
    /// (từ Enchant "Lucky") — nếu trúng Crit thì SẼ LUÔN nhân x2 lên TOÀN BỘ sát thương vừa tính được.
    /// </summary>
    public float GetFinalClickDamage()
    {
        float baseDamage = GetPickaxeBaseDamage();

        float enchantDamagePercent = EnchantManager.Instance != null ? EnchantManager.Instance.GetDamageBonusPercent() : 0f;
        float totalDamagePercent = CurrentDamagePercent + enchantDamagePercent;

        float withPercent = baseDamage * (1f + totalDamagePercent / 100f);

        float withMultiplier = withPercent * CurrentDamageMultiplier;

        if (RebirthShopManager.Instance != null &&
        RebirthShopManager.Instance.HasX2Damage)
        {
            withMultiplier *= 2f;
        }

        // ----- Roll Crit Chance (Enchant "Lucky") -----
        float critChancePercent = EnchantManager.Instance != null ? EnchantManager.Instance.GetCritChancePercent() : 0f;
        bool isCrit = critChancePercent > 0f && UnityEngine.Random.Range(0f, 100f) < critChancePercent;
        LastHitWasCrit = isCrit;

        // Crit LUÔN nhân x2 lên tổng sát thương hiện tại (không cộng %, mà nhân thẳng x2 kết quả cuối).
        return isCrit ? withMultiplier * 2f : withMultiplier;
    }


    public void ResetForRebirth()
    {
        // Reset Money
        money = 0f;

        // Reset toàn bộ Upgrade Pickaxe
        pickaxeRarityLevel = 0;
        damagePercentLevel = 0;
        damageMultiplierLevel = 0;
        moneyPercentLevel = 0;

        // Cập nhật UI
        OnMoneyChanged?.Invoke(money);
        OnPickaxeNameChanged?.Invoke(GetPickaxeName());

        // Báo các UI Upgrade cập nhật lại
        OnRebirthReset?.Invoke();
    }
}