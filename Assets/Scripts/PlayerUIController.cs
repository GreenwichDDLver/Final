using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PlayerUIController : MonoBehaviour
{
    [SerializeField]TextMeshProUGUI ammoText;
    //[SerializeField]float highlightTime = 1;
    float highlightTime = 0;
    bool isHighlight = false;

    [SerializeField] Image healthFill;

    public void RereshHealthBar(HPManager hpManager)
    {
        healthFill.fillAmount = hpManager.GetRate();
    }

    public void Awake()
    {
        // 确保ammoText已赋值
        if (ammoText == null)
        {
            Debug.LogError("[PlayerUIController] ammoText is not assigned in Inspector!");
        }
        else
        {
            // 修复TextMeshPro字体问题（黑色方块通常是字体缺失）
            FixTextMeshProFont();
        }
    }

    private void FixTextMeshProFont()
    {
        if (ammoText == null) return;

        // 如果字体为null或显示异常，尝试使用默认字体
        if (ammoText.font == null)
        {
            // 尝试加载TextMeshPro默认字体
            TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            
            if (defaultFont == null)
            {
                // 尝试其他可能的路径
                defaultFont = Resources.Load<TMP_FontAsset>("TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF");
            }

            if (defaultFont != null)
            {
                ammoText.font = defaultFont;
                Debug.Log("[PlayerUIController] Fixed TextMeshPro font - using default font");
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] Could not find TextMeshPro default font. Please assign a font in Inspector.");
            }
        }

        // 确保颜色不是黑色（如果显示为黑色方块）
        if (ammoText.color == Color.black)
        {
            ammoText.color = Color.white;
        }

        // 设置字体属性，强制使用ASCII字符集
        if (ammoText.font != null)
        {
            // 设置字体大小（如果太小可能导致显示问题）
            if (ammoText.fontSize < 12)
            {
                ammoText.fontSize = 24; // 设置一个合适的默认大小
            }

            // 通过设置临时文本来预加载ASCII字符（数字、斜杠、竖线、空格）
            string originalText = ammoText.text;
            ammoText.text = "0123456789/| ";
            ammoText.ForceMeshUpdate();
            ammoText.text = originalText;
            
            // 强制刷新显示
            ammoText.ForceMeshUpdate();
        }
    }

    public void Start()
    {
        // 启动时尝试初始化UI（如果武器系统已准备好）
        WeaponWheel weaponWheel = FindObjectOfType<WeaponWheel>();
        if (weaponWheel != null)
        {
            GunController currentGun = weaponWheel.GetCurGunController();
            if (currentGun != null)
            {
                RefreshAmmoText(currentGun);
            }
        }
    }

    public void RefreshAmmoText(GunController gunController)
    {
        if (gunController == null)
        {
            Debug.LogWarning("[PlayerUIController] RefreshAmmoText called with null GunController!");
            return;
        }

        if (ammoText == null)
        {
            Debug.LogError("[PlayerUIController] ammoText is null! Please assign it in Inspector.");
            return;
        }
        
        // 确保字体已修复
        if (ammoText.font == null)
        {
            FixTextMeshProFont();
        }
        
        // 格式化显示：当前弹药/弹夹容量 | 剩余弹夹数（使用纯ASCII字符）
        string newText = gunController.ammo.ToString() + "/" + gunController.ammoInMag.ToString() + " | " + gunController.mags.ToString();
        
        // 确保文本只包含ASCII字符
        ammoText.text = newText;
        
        // 强制刷新TextMeshPro组件
        ammoText.ForceMeshUpdate();
        
        Debug.Log($"[PlayerUIController] Refreshed ammo text: {newText} (from weapon: {gunController.gameObject.name})");
    }

    public void HighlightAmmoText() 
    {
        highlightTime += Time.deltaTime;
        StartCoroutine(HighlightCoroutine());
    }



    IEnumerator HighlightCoroutine() 
    {
        if(isHighlight) yield break;
        isHighlight = true;
        
        Color originalColor = ammoText.color;
        Vector3 originalScale = ammoText.transform.localScale;
        ammoText.color = Color.red;
        ammoText.transform.localScale = originalScale * 1.5f;
        
        while (highlightTime >0) { highlightTime-= Time.deltaTime; yield return null; }
        
        ammoText.color = originalColor;
        ammoText.transform.localScale = originalScale;
        highlightTime = 0;

        isHighlight = false;
    }

}
