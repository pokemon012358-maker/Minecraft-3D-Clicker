using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào TỪNG item con bên trong panel "ShopBlock" (Lapis Block, Gold Block, Emerald Block, Diamond Block).
/// Nút hành động sẽ tự đổi nhãn theo đúng trạng thái:
///   - Chưa mua      -> nút hiện "Buy"       -> bấm sẽ trừ tiền và chuyển sang trạng thái "đã mua"
///   - Đã mua        -> nút hiện "Equip"     -> bấm sẽ Equip block này (tự hủy Equip của block khác)
///   - Đang Equip    -> nút hiện "Equipped"  -> không bấm được nữa (interactable = false)
///
/// CÁCH GẮN (kéo-thả), lặp lại cho cả 4 item:
/// 1. Chọn item (hàng) trong Hierarchy, Add Component > BlockShopItemUI.
/// 2. Set "Block Index" đúng theo thứ tự đã khai báo trong BlockShopManager (Inspector của _GameManager):
///      Lapis Block = 0, Gold Block = 1, Emerald Block = 2, Diamond Block = 3
/// 3. Kéo Text (TMP) hiển thị tên block ("Lapis Block") vào field "Name Text".
/// 4. Kéo Text (TMP) hiển thị "Gives 5$ | 10$" vào field "Info Text".
/// 5. Kéo Button của item vào field "Action Button".
/// 6. Kéo Text (TMP) con của Button đó (chữ "Buy") vào field "Action Button Text".
/// </summary>
public class BlockShopItemUI : MonoBehaviour
{
    [Header("Vị trí của item này trong mảng Blocks của BlockShopManager")]
    [SerializeField] private int blockIndex;

    [Header("UI (kéo-thả các thành phần con của item này vào đây)")]
    [SerializeField] private TMP_Text nameText;         // "Lapis Block"
    [SerializeField] private TMP_Text infoText;         // "Gives 5$ | 10$"
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText; // Text con của actionButton: "Buy" / "Equip" / "Equipped"

    private void Start()
    {
        var entry = BlockShopManager.Instance.Blocks[blockIndex];
        nameText.text = entry.blockName;
        infoText.text = $"Gives {entry.moneyGiven}$ | {entry.cost}$";

        actionButton.onClick.AddListener(OnActionClicked);

        BlockShopManager.Instance.OnEquippedChanged += OnAnyEquippedChanged;

        RefreshButton();
    }

    private void OnDestroy()
    {
        if (BlockShopManager.Instance != null)
            BlockShopManager.Instance.OnEquippedChanged -= OnAnyEquippedChanged;
    }

    private void OnAnyEquippedChanged(int newlyEquippedIndex)
    {
        // Có item khác vừa được Equip -> item này cần refresh lại (có thể phải quay về "Equip" nếu trước đó đang Equipped)
        RefreshButton();
    }

    private void OnActionClicked()
    {
        var shop = BlockShopManager.Instance;

        if (!shop.IsBought(blockIndex))
        {
            // Trạng thái 1: chưa mua -> bấm "Buy"
            shop.TryBuy(blockIndex);
        }
        else if (!shop.IsEquipped(blockIndex))
        {
            // Trạng thái 2: đã mua nhưng chưa Equip -> bấm "Equip"
            shop.Equip(blockIndex);
        }
        // Trạng thái 3: đang "Equipped" -> nút đã bị disable, không vào nhánh này

        RefreshButton();
    }

    private void RefreshButton()
    {
        var shop = BlockShopManager.Instance;

        if (shop.IsEquipped(blockIndex))
        {
            actionButtonText.text = "Equipped";
            actionButton.interactable = false;
        }
        else if (shop.IsBought(blockIndex))
        {
            actionButtonText.text = "Equip";
            actionButton.interactable = true;
        }
        else
        {
            actionButtonText.text = "Buy";
            actionButton.interactable = true;
        }
    }
}
