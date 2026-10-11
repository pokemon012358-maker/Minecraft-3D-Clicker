using UnityEngine;
using TMPro;

/// <summary>
/// Cập nhật các dòng chữ "Money: 0$", "Pickaxe: None", "Enchant: None" ở góc trên bên trái,
/// tự động chạy lại mỗi khi dữ liệu trong GameManager / EnchantManager thay đổi (không cần Update() mỗi frame).
///
/// CÁCH GẮN (kéo-thả):
/// 1. Add Component script này vào Canvas (hoặc 1 GameObject UI quản lý chung, ví dụ "_UIManager").
/// 2. Kéo Text (TMP) con của "ImageMoney" vào field "Money Text".
/// 3. Kéo Text (TMP) con của "ImagePickaxe" vào field "Pickaxe Text".
/// 4. Kéo Text (TMP) con của "ImageEnchant" vào field "Enchant Text".
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Kéo các Text (TMP) tương ứng vào đây")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text pickaxeText;
    [SerializeField] private TMP_Text enchantText;

    // LƯU Ý: KHÔNG subscribe trong OnEnable() — thời điểm OnEnable() của object này chạy
    // không đảm bảo GameManager.Awake() đã chạy xong (GameManager nằm ở object khác),
    // nên GameManager.Instance có thể vẫn null tại thời điểm đó -> subscribe bị bỏ qua
    // -> UI không bao giờ nhận được event OnMoneyChanged dù Money vẫn tăng bên trong GameManager.
    //
    // Unity đảm bảo: TẤT CẢ Awake() của mọi object chạy xong trước khi BẤT KỲ Start() nào chạy.
    // Vì vậy ta subscribe trong Start() để chắc chắn GameManager.Instance đã sẵn sàng.

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[HUDController] Không tìm thấy GameManager.Instance trong Scene! " +
                            "Kiểm tra lại object '_GameManager' có đang Active và có gắn script GameManager không.");
            return;
        }

        GameManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        GameManager.Instance.OnPickaxeNameChanged += HandlePickaxeChanged;

        // Hiển thị đúng giá trị ngay khi vào game
        HandleMoneyChanged(GameManager.Instance.Money);
        HandlePickaxeChanged(GameManager.Instance.GetPickaxeName());

        if (EnchantManager.Instance != null)
        {
            EnchantManager.Instance.OnEnchantResult += HandleEnchantResult;
            HandleEnchantResult(EnchantManager.Instance.CurrentType, EnchantManager.Instance.CurrentTier);
        }
        else if (enchantText != null)
        {
            enchantText.text = "Enchant: None";
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            GameManager.Instance.OnPickaxeNameChanged -= HandlePickaxeChanged;
        }

        if (EnchantManager.Instance != null)
            EnchantManager.Instance.OnEnchantResult -= HandleEnchantResult;
    }

    private void HandleMoneyChanged(float money)
    {
        if (moneyText == null)
        {
            Debug.LogWarning("[HUDController] 'Money Text' chưa được kéo vào Inspector — " +
                              "kéo Text (TMP) con của ImageMoney vào field này.");
            return;
        }
        moneyText.text = $"{Mathf.FloorToInt(money)}$";
    }

    private void HandlePickaxeChanged(string pickaxeName)
    {
        if (pickaxeText != null)
            pickaxeText.text = $"{pickaxeName}";
    }

    private void HandleEnchantResult(EnchantManager.EnchantType type, int tier)
    {
        if (enchantText != null)
            enchantText.text = $"{EnchantManager.Instance.GetDisplayName()}";
    }
}