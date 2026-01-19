using UnityEngine;

using System.Collections;

public class AttackAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] Material attackMaterial;
    [SerializeField] int flashTimes = 3;
    [SerializeField] float animTime = 0.5f;

    Material originalMaterial;
    Renderer rend;
    bool isPlay;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) originalMaterial = rend.material;
    }

    public void Play()
    {
        if ( isPlay) return;
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        isPlay = true;

        float flashInterval = animTime / (flashTimes * 2f);

        for (int i = 0; i < flashTimes; i++)
        {
            rend.material = attackMaterial;
            yield return new WaitForSeconds(flashInterval);

            rend.material = originalMaterial;
            yield return new WaitForSeconds(flashInterval);
        }

        isPlay = false;
    }
}
