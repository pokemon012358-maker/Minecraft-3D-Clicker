
using UnityEngine;
using UnityEngine.UI;

public class RebirthController : MonoBehaviour
{
    [Header("Nút Rebirth ? thanh d??i cùng")]
    [SerializeField] private Image rebirthButtonImage;

    [Header("Nút xác nh?n Rebirth trong Panel")]
    [SerializeField] private Button confirmRebirthButton;

    [Header("Màu nút")]
    [SerializeField] private Color lockedColor = Color.black;
    [SerializeField]
    private Color unlockedColor = new Color(
        1f, 0.55f, 0.35f, 1f
    );

    private void Start()
    {
        if (confirmRebirthButton != null)
        {
            confirmRebirthButton.onClick.AddListener(
                OnConfirmRebirth
            );
        }

        if (BlockShopManager.Instance != null)
        {
            BlockShopManager.Instance.OnBlocksChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (confirmRebirthButton != null)
        {
            confirmRebirthButton.onClick.RemoveListener(
                OnConfirmRebirth
            );
        }

        if (BlockShopManager.Instance != null)
        {
            BlockShopManager.Instance.OnBlocksChanged -= RefreshUI;
        }
    }

    private bool CanRebirth()
    {
        return BlockShopManager.Instance != null
            && GameManager.Instance != null
            && BlockShopManager.Instance.AreAllBlocksBought();
    }

    public void RefreshUI()
    {
        bool canRebirth = CanRebirth();

        // Nút Rebirth chuy?n sang ?en khi ch?a ?? ?i?u ki?n
        if (rebirthButtonImage != null)
        {
            rebirthButtonImage.color = canRebirth
                ? unlockedColor
                : lockedColor;
        }

        // Ch? cho phép xác nh?n khi ?ã mua ?? Block
        if (confirmRebirthButton != null)
        {
            confirmRebirthButton.interactable = canRebirth;
        }
    }

    public void OnConfirmRebirth()
    {
        if (!CanRebirth())
        {
            RefreshUI();
            return;
        }

        // Reset Block tr??c ?? Equip l?i Coal
        BlockShopManager.Instance.ResetForRebirth();

        // Reset Money và Upgrade Pickaxe
        GameManager.Instance.ResetForRebirth();
        if (RebirthShopManager.Instance != null)
        {
            RebirthShopManager.Instance.AddToken(1);
        }
        RefreshUI();

        Debug.Log("Rebirth thành công! Enchant ???c gi? nguyên.");
    }
}