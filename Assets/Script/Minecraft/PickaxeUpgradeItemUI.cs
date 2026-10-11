using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào TỪNG item con bên trong panel "UpgradeP" (4 item theo ảnh mockup):
///   - Item 1: Rarity        ("Wood --> Stone", 10$)
///   - Item 2: DamagePercent ("0% --> 10%", Damage, 20$)
///   - Item 3: MoneyPercent  ("0% --> 10%", Money, 20$)
///   - Item 4: DamageMultiplier ("1x --> 2x", Damage, 100$)
///
/// CÁCH GẮN (kéo-thả), lặp lại cho cả 4 item:
/// 1. Chọn item (hàng) trong Hierarchy, Add Component > PickaxeUpgradeItemUI.
/// 2. Chọn đúng "Upgrade Type" trong Inspector (Rarity / DamagePercent / DamageMultiplier / MoneyPercent).
/// 3. Set "Base Cost" theo đúng số tiền hiển thị trong ảnh: 10 / 20 / 20 / 100.
/// 4. Kéo Text (TMP) hiển thị "Wood --> Stone" / "0% --> 10%" vào field "Effect Description Text".
/// 5. Kéo Text (TMP) hiển thị "Upgrade: 0/4" vào field "Level Text".
/// 6. Kéo Text (TMP) hiển thị giá tiền "10$" vào field "Cost Text".
/// 7. Kéo Button "Buy" của chính item đó vào field "Buy Button".
/// </summary>
public class PickaxeUpgradeItemUI : MonoBehaviour
{
    public enum UpgradeType { Rarity, DamagePercent, DamageMultiplier, MoneyPercent }

    [Header("Loại Upgrade mà item này đại diện")]
    [SerializeField] private UpgradeType upgradeType;

    [Header("Giá tiền (tăng dần theo mỗi cấp)")]
    [SerializeField] private float baseCost = 10f;
    [SerializeField] private float costIncreasePerLevel = 10f;

    [Header("UI (kéo-thả các Text/Button con của item này vào đây)")]
    [SerializeField] private TMP_Text effectDescriptionText; // "Wood --> Stone" hoặc "0% --> 10%"
    [SerializeField] private TMP_Text levelText;             // "Upgrade: 0/4"
    [SerializeField] private TMP_Text costText;              // "10$"
    [SerializeField] private Button buyButton;

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthReset += RefreshUI;
        RefreshUI();
    }

    private int CurrentLevel()
    {
        var gm = GameManager.Instance;
        switch (upgradeType)
        {
            case UpgradeType.Rarity: return gm.PickaxeRarityLevel;
            case UpgradeType.DamagePercent: return gm.DamagePercentLevel;
            case UpgradeType.DamageMultiplier: return gm.DamageMultiplierLevel;
            case UpgradeType.MoneyPercent: return gm.MoneyPercentLevel;
            default: return 0;
        }
    }

    private int MaxLevel()
    {
        var gm = GameManager.Instance;
        switch (upgradeType)
        {
            case UpgradeType.Rarity: return gm.MaxPickaxeRarityLevel;
            case UpgradeType.DamagePercent: return gm.MaxDamagePercentLevel;
            case UpgradeType.DamageMultiplier: return gm.MaxDamageMultiplierLevel;
            case UpgradeType.MoneyPercent: return gm.MaxMoneyPercentLevel;
            default: return 0;
        }
    }

    private float CurrentCost()
    {
        return baseCost + CurrentLevel() * costIncreasePerLevel;
    }

    private void OnBuyClicked()
    {
        var gm = GameManager.Instance;
        if (CurrentLevel() >= MaxLevel()) return;
        if (!gm.TrySpendMoney(CurrentCost())) return; // không đủ tiền thì không làm gì cả

        switch (upgradeType)
        {
            case UpgradeType.Rarity: gm.UpgradePickaxeRarity(); break;
            case UpgradeType.DamagePercent: gm.UpgradeDamagePercent(); break;
            case UpgradeType.DamageMultiplier: gm.UpgradeDamageMultiplier(); break;
            case UpgradeType.MoneyPercent: gm.UpgradeMoneyPercent(); break;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var gm = GameManager.Instance;
        int level = CurrentLevel();
        int max = MaxLevel();
        bool isMax = level >= max;

        levelText.text = $"Upgrade: {level}/{max}";
        costText.text = isMax ? "MAX" : $"{CurrentCost()}$";
        buyButton.interactable = !isMax;

        switch (upgradeType)
        {
            case UpgradeType.Rarity:
                effectDescriptionText.text = isMax
                    ? gm.GetPickaxeName()
                    : $"{gm.GetPickaxeName()} --> {gm.GetNextPickaxeName()}";
                break;

            case UpgradeType.DamagePercent:
                effectDescriptionText.text =
                    $"{level * gm.DamagePercentPerLevel}% --> {Mathf.Min((level + 1), max) * gm.DamagePercentPerLevel}%";
                break;

            case UpgradeType.DamageMultiplier:
                effectDescriptionText.text = $"{1 + level}x --> {1 + Mathf.Min(level + 1, max)}x";
                break;

            case UpgradeType.MoneyPercent:
                effectDescriptionText.text =
                    $"{level * gm.MoneyPercentPerLevel}% --> {Mathf.Min((level + 1), max) * gm.MoneyPercentPerLevel}%";
                break;
        }
    }
    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthReset -= RefreshUI;
    }
}
