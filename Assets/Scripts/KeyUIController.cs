using UnityEngine;
using TMPro;

public class KeyUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI keyText; // 钥匙数量文本（例如："Keys: 3/5"）
    [SerializeField] GameObject keyIconPrefab; // 钥匙图标Prefab（可选，用于显示钥匙图标）
    [SerializeField] Transform keyIconParent; // 钥匙图标的父对象（可选）

    private void Start()
    {
        // 初始化UI
        UpdateKeyUI();
    }

    private void OnEnable()
    {
        // 订阅KeyManager的事件（如果KeyManager有事件的话）
        if (KeyManager.instance != null)
        {
            // 可以在这里订阅事件，但目前KeyManager没有事件系统
            // 我们使用Update方法定期更新
        }
    }

    private void Update()
    {
        // 定期更新UI（如果KeyManager存在）
        if (KeyManager.instance != null)
        {
            UpdateKeyUI();
        }
    }

    // 更新钥匙UI
    public void UpdateKeyUI()
    {
        if (keyText != null && KeyManager.instance != null)
        {
            int currentKeys = KeyManager.instance.GetKeyCount();
            int requiredKeys = KeyManager.instance.GetRequiredKeys();
            keyText.text = $"Keys: {currentKeys}/{requiredKeys}";
        }
    }
}
