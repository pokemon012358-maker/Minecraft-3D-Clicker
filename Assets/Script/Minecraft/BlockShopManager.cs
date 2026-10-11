using System;
using UnityEngine;

/// <summary>
/// Quản lý danh sách Block trong "Shop Block" (Lapis, Gold, Emerald, Diamond...),
/// trạng thái đã mua / đang Equip, và giá trị tiền thưởng khi đập khối (dựa theo Block đang Equip).
///
/// CÁCH GẮN (kéo-thả):
/// 1. Gắn script này vào CHUNG GameObject "_GameManager" (cùng chỗ với GameManager.cs).
/// 2. Trong Inspector, mở mục "Blocks", đặt Size = 4 (hoặc đúng số Block bạn có trong Shop),
///    rồi điền theo đúng bảng trong Shop Block:
///       [0] Lapis Block   | Gives 5   | Cost 10
///       [1] Gold Block    | Gives 10  | Cost 20
///       [2] Emerald Block | Gives 15  | Cost 30
///       [3] Diamond Block | Gives 25  | Cost 50
/// </summary>
public class BlockShopManager : MonoBehaviour
{
    public static BlockShopManager Instance { get; private set; }
    public event Action OnBlocksChanged;

    [Serializable]
    public class BlockShopEntry
    {
        public string blockName = "Lapis Block"; // Tên hiển thị
        public float moneyGiven = 5f;            // Tiền nhận được mỗi lần đập khối khi Equip block này
        public float cost = 10f;                 // Giá mua
        public float maxHP = 10f;                // Số HP của Block khi đang Equip loại này (càng cao càng tốn nhiều click)
        [Tooltip("Kéo GameObject model 3D ĐÃ ĐẶT SẴN TRONG SCENE của loại Block này vào đây " +
                 "(ví dụ: object con 'Model_Lapis' nằm bên trong 'Block'). " +
                 "KHÔNG phải Prefab trong Project — đây là object thật trong Hierarchy. " +
                 "Script sẽ chỉ Ẩn/Hiện (SetActive) object này, không Instantiate.")]
        public GameObject modelObject;
        [HideInInspector] public bool isBought = false;
    }

    [Tooltip("Block đầu tiên (index 0) nên để isBought sẵn = true trong code (Coal Block) " +
             "để người chơi luôn có 1 block mặc định dùng được ngay từ đầu game.")]
    [SerializeField]
    private BlockShopEntry[] blocks = new BlockShopEntry[]
    {
        new BlockShopEntry { blockName = "Coal Block",    moneyGiven = 2f,  cost = 0f,  maxHP = 10f,  isBought = true },
        new BlockShopEntry { blockName = "Lapis Block",   moneyGiven = 5f,  cost = 10f, maxHP = 20f },
        new BlockShopEntry { blockName = "Gold Block",    moneyGiven = 10f, cost = 20f, maxHP = 30f },
        new BlockShopEntry { blockName = "Emerald Block", moneyGiven = 15f, cost = 30f, maxHP = 50f },
        new BlockShopEntry { blockName = "Diamond Block", moneyGiven = 25f, cost = 50f, maxHP = 100f },
    };

    [Tooltip("HP của Block khi CHƯA Equip Block nào (dự phòng, thường không dùng tới vì Coal Block đã isBought=true sẵn)")]
    [SerializeField] private float defaultMaxHP = 10f;

    [Tooltip("Tiền nhận được mỗi lần đập khối khi CHƯA Equip Block nào (dự phòng)")]
    [SerializeField] private float defaultMoneyGiven = 1f;

    private int equippedIndex = -1; // -1 = chưa equip block nào

    /// <summary>Bắn ra mỗi khi Equip 1 Block mới, int = index block vừa được equip.</summary>
    public event Action<int> OnEquippedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tự động Equip block đầu tiên đã isBought=true sẵn (mặc định là Coal Block) khi vừa vào game.
        // Đặt trong Awake() (không phải Start()) để đảm bảo chạy TRƯỚC Start() của BlockShopItemUI / BlockController,
        // vì Unity đảm bảo mọi Awake() chạy xong trước khi bất kỳ Start() nào chạy.
        if (equippedIndex < 0)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].isBought)
                {
                    equippedIndex = i;
                    break;
                }
            }
        }

        // BẢO VỆ: nếu KHÔNG có block nào isBought=true (thường do dữ liệu cũ đã lưu sẵn trong Inspector từ trước
        // khi thêm Coal Block — Unity không tự ghi đè giá trị mặc định mới trong code lên dữ liệu đã serialize),
        // thì tự động coi Block ĐẦU TIÊN (index 0) là Block khởi điểm: miễn phí, luôn sở hữu, luôn Equip sẵn.
        // Đảm bảo người chơi LUÔN có 1 block để chơi ngay từ đầu, bất kể Inspector đã cấu hình đúng hay chưa.
        if (equippedIndex < 0 && blocks.Length > 0)
        {
            blocks[0].isBought = true;
            blocks[0].cost = 0f;
            equippedIndex = 0;
        }
    }

    public BlockShopEntry[] Blocks => blocks;

    public float GetEquippedMoneyGiven()
    {
        if (equippedIndex < 0 || equippedIndex >= blocks.Length) return defaultMoneyGiven;
        return blocks[equippedIndex].moneyGiven;
    }

    /// <summary>HP tối đa của Block hiện tại, dựa theo Block đang được Equip trong Shop.</summary>
    public float GetEquippedMaxHP()
    {
        if (equippedIndex < 0 || equippedIndex >= blocks.Length) return defaultMaxHP;
        return blocks[equippedIndex].maxHP;
    }

    /// <summary>GameObject model (đã đặt sẵn trong Scene) của Block đang được Equip (null nếu chưa Equip hoặc chưa gán).</summary>
    public GameObject GetEquippedModelObject()
    {
        if (equippedIndex < 0 || equippedIndex >= blocks.Length) return null;
        return blocks[equippedIndex].modelObject;
    }

    public bool IsBought(int index) => blocks[index].isBought;
    public bool IsEquipped(int index) => equippedIndex == index;

    /// <summary>Index của Block đang Equip (0 = Coal mặc định, -1 = chưa Equip gì).</summary>
    public int EquippedIndex => equippedIndex;

    /// <summary>Thử mua Block tại index, trừ tiền qua GameManager. Trả về true nếu mua thành công.</summary>
    public bool TryBuy(int index)
    {
        if (blocks[index].isBought) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(blocks[index].cost))
            return false;

        blocks[index].isBought = true;
        OnBlocksChanged?.Invoke();
        return true;
    }

    /// <summary>Equip Block đã mua tại index (chỉ 1 Block được Equip tại 1 thời điểm).</summary>
    public void Equip(int index)
    {
        if (!blocks[index].isBought) return;
        equippedIndex = index;
        OnEquippedChanged?.Invoke(index);
    }

    public bool AreAllBlocksBought()
    {
        if (blocks == null || blocks.Length <= 1)
            return false;

        for (int i = 1; i < blocks.Length; i++)
        {
            if (!blocks[i].isBought)
                return false;
        }

        return true;
    }


    public void ResetForRebirth()
    {
        if (blocks == null || blocks.Length == 0)
            return;

        // Chỉ giữ Block mặc định (Coal Block)
        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i].isBought = (i == 0);
        }

        // Equip lại Coal Block
        equippedIndex = 0;
        blocks[0].isBought = true;

        // Báo BlockController cập nhật model và HP
        OnEquippedChanged?.Invoke(equippedIndex);

        // Cập nhật trạng thái nút Rebirth
        OnBlocksChanged?.Invoke();
    }
}