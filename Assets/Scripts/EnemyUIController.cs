using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUIController : MonoBehaviour
{
    [SerializeField]Transform healthBar;
    [SerializeField]Image healthFill;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 检查必要的组件和PlayerManager是否存在
        if (PlayerManager.instance == null || healthBar == null)
        {
            return; // 如果PlayerManager或healthBar未初始化，跳过更新
        }

        Vector3 lookPoint = PlayerManager.instance.GetPlayerPosition();
        lookPoint.y = healthBar.transform.position.y;
        healthBar.LookAt(lookPoint);
    }

    public void RereshHealthBar(HPManager hpManager)
    {
        healthFill.fillAmount =  hpManager.GetRate();
    }
}
