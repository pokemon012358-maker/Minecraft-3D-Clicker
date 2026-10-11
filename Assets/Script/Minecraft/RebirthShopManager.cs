
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RebirthShopManager : MonoBehaviour
{
    public static RebirthShopManager Instance { get; private set; }

    [Header("Token")]
    [SerializeField] private int tokens = 0;
    [SerializeField] private TMP_Text tokenText;

    [Header("Nút Buy")]
    [SerializeField] private Button x2MoneyButton;
    [SerializeField] private Button x2DamageButton;
    [SerializeField] private Button autoClickButton;

    [Header("Auto Click")]
    [SerializeField] private float autoClickInterval = 1f;

    public int Tokens => tokens;
    public bool HasX2Money { get; private set; }
    public bool HasX2Damage { get; private set; }
    public bool HasAutoClick { get; private set; }

    private float autoClickTimer;

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
        if (x2MoneyButton != null)
            x2MoneyButton.onClick.AddListener(BuyX2Money);

        if (x2DamageButton != null)
            x2DamageButton.onClick.AddListener(BuyX2Damage);

        if (autoClickButton != null)
            autoClickButton.onClick.AddListener(BuyAutoClick);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (x2MoneyButton != null)
            x2MoneyButton.onClick.RemoveListener(BuyX2Money);

        if (x2DamageButton != null)
            x2DamageButton.onClick.RemoveListener(BuyX2Damage);

        if (autoClickButton != null)
            autoClickButton.onClick.RemoveListener(BuyAutoClick);

        if (Instance == this)
            Instance = null;
    }

    public void AddToken(int amount = 1)
    {
        if (amount <= 0) return;

        tokens += amount;
        RefreshUI();
    }

    private bool SpendToken()
    {
        if (tokens < 1) return false;

        tokens--;
        RefreshUI();
        return true;
    }

    public void BuyX2Money()
    {
        if (HasX2Money || !SpendToken()) return;

        HasX2Money = true;
        RefreshUI();
        Debug.Log("?ã mua X2 Money!");
    }

    public void BuyX2Damage()
    {
        if (HasX2Damage || !SpendToken()) return;

        HasX2Damage = true;
        RefreshUI();
        Debug.Log("?ã mua X2 Damage!");
    }

    public void BuyAutoClick()
    {
        if (HasAutoClick || !SpendToken()) return;

        HasAutoClick = true;
        RefreshUI();
        Debug.Log("?ã mua Auto Click!");
    }

    private void Update()
    {
        if (!HasAutoClick || BlockController.Instance == null)
            return;

        autoClickTimer += Time.deltaTime;

        if (autoClickTimer >= Mathf.Max(0.05f, autoClickInterval))
        {
            autoClickTimer = 0f;
            BlockController.Instance.AutoClickBlock();
        }
    }

    private void RefreshUI()
    {
        if (tokenText != null)
            tokenText.text = $"Token: {tokens}";

        if (x2MoneyButton != null)
            x2MoneyButton.interactable = !HasX2Money && tokens >= 1;

        if (x2DamageButton != null)
            x2DamageButton.interactable = !HasX2Damage && tokens >= 1;

        if (autoClickButton != null)
            autoClickButton.interactable = !HasAutoClick && tokens >= 1;
    }
}