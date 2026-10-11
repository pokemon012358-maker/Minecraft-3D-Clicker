using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý hệ thống Gacha Enchant: 3 loại Enchant (Sharpness = DMG Buff, Fortunate = Money Buff,
/// Lucky = Crit Chance Buff), mỗi loại có 3 tier (I, II, III). Người chơi chỉ có thể sở hữu
/// 1 Enchant tại 1 thời điểm (roll mới sẽ ghi đè roll cũ). Enchant KHÔNG bị mất khi Rebirth
/// (đúng như dòng chữ "Cost: 1000$ Per Roll - Save Even When You Rebirth!!").
///
/// CÁCH GẮN (kéo-thả):
/// 1. Gắn script này vào GameObject "_GameManager" (cùng chỗ với GameManager, BlockShopManager).
/// 2. Không bắt buộc phải kéo gì thêm — các bảng % và giá trị buff có thể chỉnh trực tiếp trong Inspector.
/// </summary>
public class EnchantManager : MonoBehaviour
{
    public static EnchantManager Instance { get; private set; }

    public enum EnchantType { None, Sharpness, Fortunate, Lucky }

    [Header("Giá Gacha mỗi lần Roll")]
    [SerializeField] private float rollCost = 1000f;

    [Header("Thời gian hiệu ứng quay (giây)")]
    [SerializeField] private float spinDuration = 5f;

    [Header("Sharpness (DMG Buff %) theo Tier I / II / III")]
    [SerializeField] private float[] sharpnessDamagePercent = { 50f, 100f, 150f };
    [Tooltip("Trọng số random (không cần cộng đúng 100%, hệ thống tự chuẩn hoá) cho Tier I/II/III")]
    [SerializeField] private float[] sharpnessWeight = { 50f, 25f, 10f };

    [Header("Fortunate (Money Buff %) theo Tier I / II / III")]
    [SerializeField] private float[] fortunateMoneyPercent = { 10f, 20f, 30f };
    [SerializeField] private float[] fortunateWeight = { 45f, 20f, 10f };

    [Header("Lucky (Crit Chance %) theo Tier I / II / III")]
    [SerializeField] private float[] luckyCritPercent = { 15f, 25f, 50f };
    [SerializeField] private float[] luckyWeight = { 20f, 15f, 5f };

    // ===== Trạng thái hiện tại =====
    private EnchantType currentType = EnchantType.None;
    private int currentTier = 0; // 1..3, 0 = chưa có Enchant

    public bool IsRolling { get; private set; }

    /// <summary>Bắn ra ngay khi bắt đầu Roll (sau khi đã trừ tiền thành công) — UI dùng để bắt đầu hiệu ứng quay.</summary>
    public event Action OnRollStarted;
    /// <summary>Bắn ra khi Roll kết thúc (sau spinDuration giây), trả về Loại và Tier vừa trúng.</summary>
    public event Action<EnchantType, int> OnEnchantResult;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---------- GỌI TỪ NÚT "GACHA" ----------
    /// <summary>Thử bắt đầu 1 lượt Roll. Trả về false nếu không đủ tiền hoặc đang quay dở.</summary>
    public bool TryStartGachaRoll()
    {
        if (IsRolling) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(rollCost)) return false;

        IsRolling = true;
        OnRollStarted?.Invoke();
        StartCoroutine(RollRoutine());
        return true;
    }

    private IEnumerator RollRoutine()
    {
        yield return new WaitForSeconds(spinDuration);

        (EnchantType type, int tier) result = RollWeightedResult();
        currentType = result.type;
        currentTier = result.tier;

        IsRolling = false;
        OnEnchantResult?.Invoke(currentType, currentTier);
    }

    private (EnchantType, int) RollWeightedResult()
    {
        // Gộp toàn bộ 9 khả năng (3 loại x 3 tier) thành 1 bảng trọng số, random theo tổng trọng số.
        float totalWeight =
            sharpnessWeight[0] + sharpnessWeight[1] + sharpnessWeight[2] +
            fortunateWeight[0] + fortunateWeight[1] + fortunateWeight[2] +
            luckyWeight[0] + luckyWeight[1] + luckyWeight[2];

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        cumulative += sharpnessWeight[0]; if (roll < cumulative) return (EnchantType.Sharpness, 1);
        cumulative += sharpnessWeight[1]; if (roll < cumulative) return (EnchantType.Sharpness, 2);
        cumulative += sharpnessWeight[2]; if (roll < cumulative) return (EnchantType.Sharpness, 3);

        cumulative += fortunateWeight[0]; if (roll < cumulative) return (EnchantType.Fortunate, 1);
        cumulative += fortunateWeight[1]; if (roll < cumulative) return (EnchantType.Fortunate, 2);
        cumulative += fortunateWeight[2]; if (roll < cumulative) return (EnchantType.Fortunate, 3);

        cumulative += luckyWeight[0]; if (roll < cumulative) return (EnchantType.Lucky, 1);
        cumulative += luckyWeight[1]; if (roll < cumulative) return (EnchantType.Lucky, 2);
        return (EnchantType.Lucky, 3);
    }

    // ---------- TRUY VẤN BUFF HIỆN TẠI (dùng ở GameManager) ----------
    public float GetDamageBonusPercent()
    {
        if (currentType != EnchantType.Sharpness || currentTier <= 0) return 0f;
        return sharpnessDamagePercent[currentTier - 1];
    }

    public float GetMoneyBonusPercent()
    {
        if (currentType != EnchantType.Fortunate || currentTier <= 0) return 0f;
        return fortunateMoneyPercent[currentTier - 1];
    }

    public float GetCritChancePercent()
    {
        if (currentType != EnchantType.Lucky || currentTier <= 0) return 0f;
        return luckyCritPercent[currentTier - 1];
    }

    /// <summary>Tên hiển thị dạng "Sharpness II", "Fortunate III", "Lucky I", hoặc "None" nếu chưa có.</summary>
    public string GetDisplayName()
    {
        if (currentType == EnchantType.None || currentTier <= 0) return "None";
        string roman = currentTier == 1 ? "I" : currentTier == 2 ? "II" : "III";
        return $"{currentType} {roman}";
    }

    public EnchantType CurrentType => currentType;
    public int CurrentTier => currentTier;
}
