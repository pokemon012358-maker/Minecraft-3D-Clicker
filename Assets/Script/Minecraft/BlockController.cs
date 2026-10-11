using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào GameObject "Block" (khối 3D ở giữa màn hình).
/// - Luôn tự xoay (Update).
/// - Khi click vào khối: phóng to rồi thu nhỏ lại (hiệu ứng "punch scale"), đồng thời trừ HP của khối.
/// - Khi HP về 0: cộng tiền thưởng (theo Block đang Equip trong Shop) và reset lại HP đầy.
///
/// YÊU CẦU: GameObject "Block" phải có 1 Collider (Box Collider / Mesh Collider) để OnMouseDown hoạt động
/// (không cần EventSystem / Physics Raycaster, OnMouseDown dùng trực tiếp Camera chính + Collider).
///
/// CÁCH GẮN (kéo-thả):
/// 1. Chọn GameObject "Block" trong Hierarchy.
/// 2. Add Component > BlockController.
/// 3. Nếu "Block" chưa có Collider, Add Component > Box Collider (hoặc Mesh Collider, tick "Convex").
/// 4. Kéo Image dạng Filled (thanh máu đỏ, con của "ImageHPBlock") vào field "Hp Fill Image".
/// 5. Kéo Text (TMP) hiển thị số "10" vào field "Hp Text".
/// </summary>
[RequireComponent(typeof(Collider))]
public class BlockController : MonoBehaviour
{
    [Header("Xoay khối")]
    [SerializeField] private float rotateSpeed = 30f; // độ/giây quanh trục Y

    [Header("Hiệu ứng Click (Punch Scale)")]
    [SerializeField] private float punchScaleMultiplier = 1.25f; // phóng to bao nhiêu lần khi click
    [SerializeField] private float punchDuration = 0.12f;        // thời gian phóng to / thu nhỏ lại

    [Header("HP của Block (dự phòng — chỉ dùng khi BlockShopManager chưa Equip block nào)")]
    [SerializeField] private float fallbackMaxHP = 10f;

    [Header("UI Hiển thị HP (kéo-thả từ ImageHPBlock)")]
    [Tooltip("Image có Image Type = Filled, Fill Method = Horizontal")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;

    // Model 3D của từng loại Block được khai báo ngay trong BlockShopManager (field "Model Object" của mỗi entry) —
    // đó là các GameObject model bạn đã đặt sẵn, lồng vào nhau tại cùng vị trí bên trong "Block".
    // BlockController chỉ cần Ẩn/Hiện (SetActive) đúng cái đang Equip, KHÔNG Instantiate gì cả.

    [Header("Model gốc ban đầu (model có sẵn từ trước, dùng tạm cho giai đoạn Coal)")]
    [Tooltip("Kéo GameObject model gốc (đã có sẵn trong Scene từ trước, ví dụ model mặc định đang hiện ở giai đoạn Coal) vào đây. " +
             "Model này sẽ TỰ ĐỘNG ẨN ngay khi người chơi Equip lên Lapis Block trở lên (index >= 1), " +
             "và tự hiện lại nếu quay về Equip Coal Block (index 0). Để trống nếu không cần dùng cơ chế này.")]
    [SerializeField] private GameObject initialBlockModel;
    public static BlockController Instance { get; private set; }

    private float currentHP;
    private Vector3 originalScale;
    private Coroutine punchRoutine;

    private void Awake()
    {
        Instance = this;
        originalScale = transform.localScale;
    }

    private void Start()
    {
        currentHP = CurrentMaxHP();
        UpdateHPUI();
        RefreshModelVisibility();

        if (BlockShopManager.Instance != null)
            BlockShopManager.Instance.OnEquippedChanged += HandleEquippedChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (BlockShopManager.Instance != null)
            BlockShopManager.Instance.OnEquippedChanged -= HandleEquippedChanged;
    }

    /// <summary>Khi người chơi Equip sang Block khác trong Shop: HP reset đầy theo maxHP mới, và đổi model hiển thị.</summary>
    private void HandleEquippedChanged(int newlyEquippedIndex)
    {
        currentHP = CurrentMaxHP();
        UpdateHPUI();
        RefreshModelVisibility();
    }

    /// <summary>
    /// Ẩn TẤT CẢ model trong danh sách Blocks của Shop, chỉ Hiện đúng model của Block đang được Equip.
    /// Model nào chưa gán (null) trong Inspector sẽ được bỏ qua, không gây lỗi.
    /// </summary>
    private void RefreshModelVisibility()
    {
        if (BlockShopManager.Instance == null) return;

        var blocks = BlockShopManager.Instance.Blocks;
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].modelObject == null) continue;
            blocks[i].modelObject.SetActive(BlockShopManager.Instance.IsEquipped(i));
        }

        // Model gốc ban đầu: chỉ hiện khi đang Equip Coal (index 0) hoặc chưa Equip gì (index -1),
        // tự ẩn ngay khi người chơi mua và Equip lên Lapis Block (index 1) trở lên.
        if (initialBlockModel != null)
        {
            int equippedIndex = BlockShopManager.Instance.EquippedIndex;
            initialBlockModel.SetActive(equippedIndex <= 0);
        }
    }

    /// <summary>HP tối đa hiện tại — lấy theo Block đang Equip trong Shop, nếu chưa có thì dùng fallbackMaxHP.</summary>
    private float CurrentMaxHP()
    {
        return BlockShopManager.Instance != null
            ? BlockShopManager.Instance.GetEquippedMaxHP()
            : fallbackMaxHP;
    }

    private void Update()
    {
        // Luôn xoay khối quanh trục Y, không phụ thuộc vào click
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>Unity tự động gọi hàm này khi người chơi click chuột trái vào Collider của object.</summary>
    private void OnMouseDown()
    {
        HandleClick();
    }

    private void HandleClick()
    {
        // 1. Hiệu ứng phóng to khối rồi thu nhỏ lại
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchScaleRoutine());

        // 2. Tính sát thương: lấy theo Pickaxe Tier hiện tại (None=1, Wood=2, Stone=3, Iron=4, Gold=5...),
        //    đã áp dụng thêm % Damage và Damage xN từ GameManager
        float damage = GameManager.Instance != null
            ? GameManager.Instance.GetFinalClickDamage()
            : 1f;

        currentHP -= damage;

        if (currentHP <= 0f)
        {
            RewardAndReset();
        }
        else
        {
            UpdateHPUI();
        }
    }

    private void RewardAndReset()
    {
        float reward = BlockShopManager.Instance != null
            ? BlockShopManager.Instance.GetEquippedMoneyGiven()
            : 1f;

        GameManager.Instance?.AddMoney(reward);

        // Reset HP đầy lại — lấy maxHP MỚI NHẤT (phòng trường hợp người chơi vừa đổi Equip Block khác)
        currentHP = CurrentMaxHP();
        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        float max = CurrentMaxHP();

        if (hpFillImage != null)
            hpFillImage.fillAmount = max > 0f ? Mathf.Clamp01(currentHP / max) : 0f;

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(Mathf.Max(currentHP, 0f)).ToString();
    }

    private IEnumerator PunchScaleRoutine()
    {
        Vector3 targetScale = originalScale * punchScaleMultiplier;
        float t = 0f;

        // Phóng to
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / punchDuration);
            yield return null;
        }

        // Thu nhỏ về lại kích thước gốc
        t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / punchDuration);
            yield return null;
        }

        transform.localScale = originalScale;
    }
    public void AutoClickBlock()
    {
        HandleClick();
    }

}