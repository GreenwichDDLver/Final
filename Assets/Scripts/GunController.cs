using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GunController : MonoBehaviour
{
    [Header("Gun Setting")]
    [SerializeField] float fireInterval = 0.2f;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform cameraPoint;
    [SerializeField] int damgeHp = 10;
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField] float bulletLifeTime = 3f;
    [SerializeField] Transform FirePoint;

    [Header("Events")]
    public UnityEvent FireEvent;
    public UnityEvent AmmoChangeEvent;
    public UnityEvent AmmoEmptyEvent;

    [Header("Ammo Data")]
    public int ammo;
    public int mags;
    public int ammoInMag = 30;

    [Header("Audio")]
    [SerializeField] AudioClip playerFireSound; // 玩家射击音效
    [SerializeField] AudioClip enemyFireSound; // 敌人射击音效
    [SerializeField] AudioSource audioSource; // 音频源组件

    bool isFire;

    void Start()
    {
        // 如果没有指定AudioSource，尝试获取或添加
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D音效
            }
        }
    }

    public void Fire(bool isPlayerFire)
    {
        if (isFire) return;

        StartCoroutine(FireRoutine(isPlayerFire));
    }

    IEnumerator FireRoutine(bool isPlayerFire)
    {
        isFire = true;

        if (ammo <= 0)
        {
            if (mags > 0)
            {
                mags--;
                ammo += ammoInMag;
                // 换弹后也需要刷新UI（如果是玩家武器）
                if (isPlayerFire) RefreshUI();
            }
            else
            {
                AmmoEmptyEvent?.Invoke();
                isFire = false;
                yield break;
            }
        }

        ammo--;
        
        // 如果是玩家武器，手动刷新UI（确保使用当前武器的数据）
        // 不触发AmmoChangeEvent，因为UnityEvent中可能硬编码了错误的武器引用
        if (isPlayerFire) 
        {
            RefreshUI();
        }
        else
        {
            // 非玩家武器（敌人）才触发事件
            AmmoChangeEvent?.Invoke();
        }

        // ���߼��Ŀ���
        Ray ray = new Ray(cameraPoint.position, cameraPoint.forward);
        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = cameraPoint.position + cameraPoint.forward * 100f;
        }

        // 播放射击音效
        if (audioSource != null)
        {
            AudioClip soundToPlay = isPlayerFire ? playerFireSound : enemyFireSound;
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
        }

        // �����ӵ�
        GameObject bullet = Instantiate(bulletPrefab, FirePoint.position, FirePoint.rotation);

        BulletController bc = bullet.GetComponent<BulletController>();

        
        bc.Fire(targetPoint,damgeHp,bulletLifeTime,bulletSpeed,isPlayerFire);
        

        FireEvent?.Invoke();

        yield return new WaitForSeconds(fireInterval);
        isFire = false;
    }

    public void AddMags(int cnt)
    {
        // 增加弹夹数量（后面的数字）
        mags += cnt;
        // 手动刷新UI，不依赖AmmoChangeEvent（可能硬编码了错误的武器引用）
        RefreshUI();
    }

    // 直接增加当前弹药数（前面的数字）
    public void AddAmmo(int cnt)
    {
        ammo += cnt;
        // 如果超过弹夹容量，多余部分转为弹夹
        if (ammo > ammoInMag)
        {
            int extraAmmo = ammo - ammoInMag;
            int extraMags = extraAmmo / ammoInMag;
            ammo = ammoInMag;
            mags += extraMags;
        }
        RefreshUI();
    }

    // 手动刷新UI，确保使用当前GunController的数据
    void RefreshUI()
    {
        PlayerUIController uiController = FindObjectOfType<PlayerUIController>();
        if (uiController != null)
        {
            uiController.RefreshAmmoText(this);
        }
    }
}
