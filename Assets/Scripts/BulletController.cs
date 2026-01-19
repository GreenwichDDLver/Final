using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    int demageHp;
    bool isPlayerFire;
    bool isFired;
    bool isDamaged = false;


    public void Fire(Vector3 target, int demageHp, float lifeTime, float speed, bool isPlayerFire)
    {
        if (isFired) return; // ��ֹ�ظ�����

        isFired = true;

        this.demageHp = demageHp;
        this.isPlayerFire = isPlayerFire;

        Vector3 dir = (target - transform.position).normalized;

         StartCoroutine(FireCoroutine(dir, lifeTime, speed));
    }

    IEnumerator FireCoroutine(Vector3 dir, float lifeTime, float speed)
    {
        float timer = 0f;

        while (timer < lifeTime)
        {
            transform.position += dir * speed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
        HPManager hpManager = other.GetComponent<HPManager>();
        if (hpManager != null) 
        {
            if (isDamaged == true) return;
            isDamaged = true;
            if (other.CompareTag("Player") != isPlayerFire) 
            {
                Debug.Log("bullet demage = " + demageHp + "  "+gameObject.name);
                hpManager.Attack(demageHp);
            }
        }

    }
}
