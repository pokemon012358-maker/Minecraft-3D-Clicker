using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển hiệu ứng quay Gacha trong panel "EnchantP":
/// - 3 "Background Xanh" (1 cho mỗi hàng Sharpness / Fortunate / Lucky) mặc định ĐANG ẨN.
/// - Khi bấm nút "Gacha": nếu đủ tiền, 3 background này sẽ lần lượt Hiện/Ẩn thay phiên nhau liên tục
///   trong 5 giây (giống hiệu ứng quay số/slot machine) — mỗi lần chỉ 1 cái hiện, các cái còn lại ẩn.
/// - Sau 5 giây, dừng lại đúng ở background tương ứng với kết quả thật sự đã roll được (Sharpness/Fortunate/Lucky),
///   các background khác ẩn hẳn.
///
/// CÁCH GẮN (kéo-thả):
/// 1. Add Component vào panel "EnchantP" (hoặc GameObject quản lý UI chung).
/// 2. Kéo đúng 3 object "Background Xanh" vào mảng "Spin Backgrounds" THEO ĐÚNG THỨ TỰ:
///      Index 0 = Background của hàng Sharpness
///      Index 1 = Background của hàng Fortunate
///      Index 2 = Background của hàng Lucky
///    (Dựa theo Hierarchy hiện tại của bạn, đây chính là 3 object "BackG", "BackG (1)", "BackG (2)".)
/// 3. Kéo nút "Gacha" vào field "Gacha Button".
/// 4. (Tuỳ chọn) Nếu có object hiệu ứng "đang hồi hộp" muốn hiện khi đang quay (ví dụ "Sus"),
///    kéo vào field "Suspense Object" — sẽ tự hiện lúc đang quay, tự ẩn khi có kết quả.
/// </summary>
public class EnchantPanelController : MonoBehaviour
{
    [Header("Nút Gacha")]
    [SerializeField] private Button gachaButton;

    [Header("3 Background Xanh (theo đúng thứ tự: Sharpness, Fortunate, Lucky)")]
    [SerializeField] private GameObject[] spinBackgrounds = new GameObject[3];

    [Header("Tốc độ nhấp nháy khi đang quay (giây giữa mỗi lần đổi)")]
    [SerializeField] private float spinInterval = 0.12f;

    [Header("Tuỳ chọn: object hiệu ứng hồi hộp trong lúc quay")]
    [SerializeField] private GameObject suspenseObject;

    private Coroutine spinCoroutine;

    private void Start()
    {
        // Mặc định ẩn hết 3 Background Xanh
        SetAllBackgroundsActive(false);
        if (suspenseObject != null) suspenseObject.SetActive(false);

        gachaButton.onClick.AddListener(OnGachaClicked);

        if (EnchantManager.Instance != null)
        {
            EnchantManager.Instance.OnRollStarted += HandleRollStarted;
            EnchantManager.Instance.OnEnchantResult += HandleRollResult;
        }

        // Nếu đã có Enchant từ trước (ví dụ load lại Scene), hiện sẵn đúng background tương ứng
        RefreshFinalBackground();
    }

    private void OnDestroy()
    {
        if (EnchantManager.Instance != null)
        {
            EnchantManager.Instance.OnRollStarted -= HandleRollStarted;
            EnchantManager.Instance.OnEnchantResult -= HandleRollResult;
        }
    }

    private void OnGachaClicked()
    {
        EnchantManager.Instance.TryStartGachaRoll();
        // Nếu không đủ tiền hoặc đang quay dở, TryStartGachaRoll trả về false và không có gì xảy ra.
    }

    private void HandleRollStarted()
    {
        gachaButton.interactable = false; // chặn bấm liên tục trong lúc đang quay
        if (suspenseObject != null) suspenseObject.SetActive(true);

        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinLoop());
    }

    private void HandleRollResult(EnchantManager.EnchantType type, int tier)
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        if (suspenseObject != null) suspenseObject.SetActive(false);
        gachaButton.interactable = true;

        RefreshFinalBackground();
    }

    /// <summary>Hiện đúng 1 Background Xanh khớp với Enchant hiện tại, ẩn 2 cái còn lại.</summary>
    private void RefreshFinalBackground()
    {
        if (EnchantManager.Instance == null) return;

        int indexToShow = EnchantManager.Instance.CurrentType switch
        {
            EnchantManager.EnchantType.Sharpness => 0,
            EnchantManager.EnchantType.Fortunate => 1,
            EnchantManager.EnchantType.Lucky => 2,
            _ => -1 // None -> ẩn hết
        };

        for (int i = 0; i < spinBackgrounds.Length; i++)
        {
            if (spinBackgrounds[i] != null)
                spinBackgrounds[i].SetActive(i == indexToShow);
        }
    }

    private IEnumerator SpinLoop()
    {
        int current = -1; // -1 = chưa có ô nào được chọn trước đó
        SetAllBackgroundsActive(false);

        while (true)
        {
            // Random 1 index MỚI, khác với index vừa sáng ngay trước đó (tránh sáng trùng 2 lần liên tiếp)
            int next;
            if (spinBackgrounds.Length <= 1)
            {
                next = 0;
            }
            else
            {
                do
                {
                    next = UnityEngine.Random.Range(0, spinBackgrounds.Length);
                } while (next == current);
            }
            current = next;

            SetAllBackgroundsActive(false);
            if (spinBackgrounds[current] != null)
                spinBackgrounds[current].SetActive(true);

            yield return new WaitForSeconds(spinInterval);
        }
    }

    private void SetAllBackgroundsActive(bool active)
    {
        foreach (var bg in spinBackgrounds)
            if (bg != null) bg.SetActive(active);
    }
}