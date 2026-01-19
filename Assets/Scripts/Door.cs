using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] bool isOpen = false; // 门是否已打开
    [SerializeField] float openSpeed = 2f; // 开门速度
    [SerializeField] Vector3 openOffset = new Vector3(0, 5, 0); // 开门时的移动偏移（向上移动5单位）
    [SerializeField] bool useRotation = false; // 是否使用旋转开门（而不是平移）
    [SerializeField] Vector3 openRotation = new Vector3(0, 90, 0); // 开门时的旋转角度

    [Header("Portal Settings")]
    [SerializeField] string nextSceneName = ""; // 下一个场景的名称（如果为空则不切换场景）
    [SerializeField] bool loadNextSceneOnEnter = true; // 进入门后是否自动加载下一个场景
    [SerializeField] float sceneLoadDelay = 1f; // 加载场景前的延迟时间

    [Header("Visual Effects")]
    [SerializeField] GameObject portalEffect; // 传送门特效（可选）
    [SerializeField] AudioClip openSound; // 开门音效
    [SerializeField] AudioClip portalSound; // 传送门音效
    [SerializeField] AudioSource audioSource; // 音频源组件

    private Vector3 closedPosition; // 关闭时的位置
    private Quaternion closedRotation; // 关闭时的旋转
    private Vector3 targetPosition; // 目标位置
    private Quaternion targetRotation; // 目标旋转
    private bool isMoving = false; // 是否正在移动

    void Start()
    {
        // 保存初始位置和旋转
        closedPosition = transform.position;
        closedRotation = transform.rotation;
        targetPosition = closedPosition;
        targetRotation = closedRotation;

        // 初始化音频源
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // 如果门初始状态是关闭的，隐藏或禁用
        if (!isOpen)
        {
            gameObject.SetActive(false);
        }
    }

    // 开门
    public void OpenDoor()
    {
        if (isOpen) return; // 如果已经打开，不再执行

        isOpen = true;
        gameObject.SetActive(true); // 激活门对象

        // 计算目标位置和旋转
        targetPosition = closedPosition + openOffset;
        targetRotation = closedRotation * Quaternion.Euler(openRotation);

        // 播放开门音效
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // 显示传送门特效
        if (portalEffect != null)
        {
            portalEffect.SetActive(true);
        }

        Debug.Log("[Door] Door opened!");
        
        // 开始开门动画
        StartCoroutine(OpenDoorAnimation());
    }

    // 开门动画协程
    private IEnumerator OpenDoorAnimation()
    {
        isMoving = true;
        float elapsedTime = 0f;
        float duration = Vector3.Distance(transform.position, targetPosition) / openSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // 平滑移动位置
            transform.position = Vector3.Lerp(closedPosition, targetPosition, t);

            // 如果使用旋转，平滑旋转
            if (useRotation)
            {
                transform.rotation = Quaternion.Lerp(closedRotation, targetRotation, t);
            }

            yield return null;
        }

        // 确保到达目标位置
        transform.position = targetPosition;
        if (useRotation)
        {
            transform.rotation = targetRotation;
        }

        isMoving = false;
        Debug.Log("[Door] Door animation completed.");
    }

    // 玩家进入门时触发
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isOpen && !isMoving)
        {
            Debug.Log("[Door] Player entered the door!");

            // 播放传送门音效
            if (audioSource != null && portalSound != null)
            {
                audioSource.PlayOneShot(portalSound);
            }

            // 如果设置了下一个场景，加载场景
            if (loadNextSceneOnEnter && !string.IsNullOrEmpty(nextSceneName))
            {
                StartCoroutine(LoadNextScene());
            }
        }
    }

    // 加载下一个场景
    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        
        Debug.Log($"[Door] Loading next scene: {nextSceneName}");
        
        // 使用Unity的场景管理系统加载场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    // 设置下一个场景名称（可以在运行时动态设置）
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }
}
