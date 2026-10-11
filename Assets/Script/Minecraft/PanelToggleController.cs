using UnityEngine;

/// <summary>
/// Quản lý việc mở/đóng 4 Panel dưới cùng màn hình: Upgrade Pickaxe ("UpgradeP"),
/// Enchant Pickaxe (chưa có panel thật, để trống tạm), Shop Block ("ShopBlock"),
/// Rebirth (chưa có panel thật, để trống tạm).
/// Mỗi lần chỉ mở được 1 panel; bấm nút khác hoặc nút "X" sẽ đóng panel đang mở.
///
/// CÁCH GẮN (kéo-thả):
/// 1. Add Component script này vào Canvas (hoặc "_UIManager").
/// 2. Kéo GameObject "UpgradeP" vào field "Upgrade Pickaxe Panel".
/// 3. Kéo GameObject "ShopBlock" vào field "Shop Block Panel".
/// 4. "Enchant Panel" và "Rebirth Panel" để trống (null) — chưa cần xử lý, chỉ cần không gán gì cả.
/// 5. Chọn 4 nút dưới cùng (trong Hierarchy tên là "Button", "Button (1)", "Button (2)", "Button (3)"):
///      - Nút "Upgrade Pickaxe"  -> Button component -> OnClick() -> + -> kéo object chứa script này vào
///        -> chọn PanelToggleController.OnClick_UpgradePickaxe
///      - Nút "Enchant Pickaxe"  -> OnClick_EnchantPickaxe
///      - Nút "Shop Block"       -> OnClick_ShopBlock
///      - Nút "Rebirth"          -> OnClick_Rebirth
/// 6. Nếu panel "UpgradeP" / "ShopBlock" có nút "X" riêng để đóng (như trong ảnh mockup),
///    gắn OnClick() của nút "X" đó -> PanelToggleController.CloseAll().
/// </summary>
public class PanelToggleController : MonoBehaviour
{
    [Header("Panels (kéo GameObject Panel tương ứng vào đây)")]
    [SerializeField] private GameObject upgradePickaxePanel; // "UpgradeP"
    [SerializeField] private GameObject enchantPanel;        // "EnchantP"
    [SerializeField] private GameObject shopBlockPanel;      // "ShopBlock"
    [SerializeField] private GameObject rebirthPanel;        // chưa có, để trống (null) tạm thời

    private GameObject[] AllPanels => new[] { upgradePickaxePanel, enchantPanel, shopBlockPanel, rebirthPanel };

    private void Start()
    {
        CloseAll();
    }

    // ===== Gắn 4 hàm dưới đây vào OnClick() của 4 nút tương ứng =====

    public void OnClick_UpgradePickaxe()
    {
        ToggleOnly(upgradePickaxePanel);
    }

    public void OnClick_EnchantPickaxe()
    {
        ToggleOnly(enchantPanel);
    }

    public void OnClick_ShopBlock()
    {
        ToggleOnly(shopBlockPanel);
    }

    public void OnClick_Rebirth()
    {
        // TODO: chưa xử lý logic Rebirth — tạm thời chỉ mở/đóng panel nếu đã gán
        ToggleOnly(rebirthPanel);
    }

    /// <summary>Gắn vào nút "X" bên trong mỗi panel để đóng panel đó lại.</summary>
    public void CloseAll()
    {
        foreach (var p in AllPanels)
            if (p != null) p.SetActive(false);
    }

    private void ToggleOnly(GameObject panel)
    {
        if (panel == null) return;

        bool willOpen = !panel.activeSelf;
        CloseAll();
        panel.SetActive(willOpen);
    }
}