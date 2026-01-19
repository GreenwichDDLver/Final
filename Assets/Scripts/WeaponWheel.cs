using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon
{
    public GameObject gun;
    public bool isUnlock;
}

public class WeaponWheel : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] List<Weapon> weapons;

    int index = 0;
    PlayerUIController uiController;

    void Start()
    {
        // 查找PlayerUIController
        uiController = FindObjectOfType<PlayerUIController>();
        if (uiController == null)
        {
            Debug.LogWarning("[WeaponWheel] PlayerUIController not found in scene!");
        }
        UpdateWeaponActive();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f) SwitchWeapon(1);
        else if (scroll < 0f) SwitchWeapon(-1);
    }

    void SwitchWeapon(int direction)
    {
        if (weapons == null || weapons.Count == 0) return;

        int startIndex = index;

        do
        {
            index = (index + direction + weapons.Count) % weapons.Count;
            // ���ѭ����������û�н����������Ͳ��л�
            if (index == startIndex) break;
        } while (!weapons[index].isUnlock);

        UpdateWeaponActive();
    }

    void UpdateWeaponActive()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].gun != null) weapons[i].gun.SetActive(false);
        }
        weapons[index].gun.SetActive(true);
        
        // 获取当前武器的GunController并刷新UI
        GunController currentGun = GetCurGunController();
        if (currentGun != null)
        {
            // 确保PlayerUIController已找到
            if (uiController == null)
            {
                uiController = FindObjectOfType<PlayerUIController>();
            }
            
            // 手动刷新UI（使用正确的当前武器）
            // 不触发AmmoChangeEvent，因为UnityEvent中可能硬编码了错误的武器引用
            if (uiController != null)
            {
                uiController.RefreshAmmoText(currentGun);
                Debug.Log($"[WeaponWheel] Switched to weapon {index + 1}, Ammo: {currentGun.ammo}/{currentGun.ammoInMag} | Mags: {currentGun.mags}");
            }
            else
            {
                // 尝试重新查找（可能在Start之后才初始化）
                uiController = FindObjectOfType<PlayerUIController>();
                if (uiController != null)
                {
                    uiController.RefreshAmmoText(currentGun);
                    Debug.Log($"[WeaponWheel] Switched to weapon {index + 1}, Ammo: {currentGun.ammo}/{currentGun.ammoInMag} | Mags: {currentGun.mags}");
                }
            }
        }
    }
    public void AddMags(int cnt) 
    {
        GetCurGunController().AddMags(cnt);
    }
    public GunController GetCurGunController()
    {
        GunController gunController =  weapons[index].gun.GetComponent<GunController>();
        if (gunController == null) Debug.LogError("gunCtrl empty");
        return gunController;   
    }
}
