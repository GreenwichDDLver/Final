using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPick : MonoBehaviour
{
    [SerializeField] int mags = 5;
    
    [Header("Audio")]
    [SerializeField] AudioClip pickUpSound; // 拾取音效
    [SerializeField] AudioSource audioSource; // 音频源组件

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 播放拾取音效（在销毁前播放）
            if (audioSource != null && pickUpSound != null)
            {
                // 使用3D音效，让玩家能听到
                audioSource.spatialBlend = 0f; // 改为2D音效，确保能听到
                audioSource.PlayOneShot(pickUpSound);
            }
            else
            {
                Debug.LogWarning("[AmmoPick] AudioSource or pickUpSound is not assigned!");
            }

            // 添加弹夹
            PlayerManager.instance.AddMags(mags);
            
            // 延迟销毁，确保音效能播放
            StartCoroutine(DestroyAfterSound());
        }
    }

    private System.Collections.IEnumerator DestroyAfterSound()
    {
        // 禁用碰撞体和渲染，但保留AudioSource
        Collider col = GetComponent<Collider>();
        Renderer rend = GetComponent<Renderer>();
        
        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;
        
        // 等待音效播放完成（假设音效最长1秒）
        yield return new WaitForSeconds(1f);
        
        Destroy(gameObject);
    }
}
