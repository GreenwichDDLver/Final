using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    public GunController gunController;
    [SerializeField] WeaponWheel weaponWheel;
    private void Awake()
    {
        if (weaponWheel == null) Debug.LogError("weaponWheel eMPTU  : " + gameObject.name);

    }
    void Update()
    {
        if (Input.GetMouseButton(0)) weaponWheel.GetCurGunController().Fire(true) ;                
    }
}
