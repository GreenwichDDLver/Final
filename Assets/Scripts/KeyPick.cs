using System.Collections;
using UnityEngine;

public class KeyPick : MonoBehaviour
{
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
            // 播放拾取音效
            if (audioSource != null && pickUpSound != null)
            {
                audioSource.spatialBlend = 0f; // 改为2D音效，确保能听到
                audioSource.PlayOneShot(pickUpSound);
            }
            else
            {
                Debug.LogWarning("[KeyPick] AudioSource or pickUpSound is not assigned!");
            }

            // 添加钥匙
            KeyManager.instance.AddKey();
            
            Debug.Log($"[KeyPick] Key collected! Total keys: {KeyManager.instance.GetKeyCount()}/5");
            
            // 延迟销毁，确保音效能播放
            StartCoroutine(DestroyAfterSound());
        }
    }

    private IEnumerator DestroyAfterSound()
    {
        // 禁用碰撞体和渲染，但保留AudioSource
        Collider col = GetComponent<Collider>();
        Renderer rend = GetComponent<Renderer>();
        
        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;
        
        // 等待音效播放完成
        yield return new WaitForSeconds(1f);
        
        Destroy(gameObject);
    }
}
