using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float inertiaFactor = 5f;
    [SerializeField] float runMultiplier = 1.5f;
    [SerializeField] float crouchSpeedMultiplier = 0.5f; // 蹲下时的速度倍数
    [SerializeField] float crouchHeight = 0.5f; // 蹲下时的高度

    [Header("Jump")]
    [SerializeField] int jumpCnt = 1;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float gravity = -9.8f;

    [Header("Camera")]
    [SerializeField] float mouseSensitive = 2f;
    [SerializeField] Transform cameraPoint;

    [Header("Audio")]
    [SerializeField] AudioClip footstepSound; // 脚步声
    [SerializeField] AudioClip landingSound; // 落地声
    [SerializeField] AudioSource audioSource; // 音频源组件
    [SerializeField] float footstepInterval = 0.5f; // 脚步声间隔

    CharacterController controller;

    Vector3 moveDir;
    Vector3 currentVelocity;

    float yVelocity;
    int currentJumpCnt;
    float footstepTimer = 0f; // 脚步声计时器
    bool wasGrounded = true; // 上一帧是否在地面
    bool isRunning = false; // 是否在奔跑
    bool isCrouching = false; // 是否在蹲下

    float xRot;
    float originalHeight; // 原始CharacterController高度
    float originalCenterY; // 原始CharacterController中心Y

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentJumpCnt = 0;

        // 保存原始CharacterController的高度和中心
        originalHeight = controller.height;
        originalCenterY = controller.center.y;

        // 如果没有指定AudioSource，尝试获取或添加
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D音效（玩家的脚步声通常是2D）
            }
        }

        wasGrounded = controller.isGrounded;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Move();
        CameraControl();
        
        // 持续检查是否可以站起（如果玩家想站起但被卡住）
        if (isCrouching)
        {
            // 检查上方是否有空间可以站起
            if (!Physics.CheckCapsule(
                transform.position + Vector3.up * (controller.radius),
                transform.position + Vector3.up * (originalHeight - controller.radius + 0.1f),
                controller.radius))
            {
                // 有空间，但保持蹲下状态直到玩家再次按X
                // 这里不自动站起，等待玩家输入
            }
        }
    }

    void Move()
    {
        // 处理奔跑切换（按Shift切换）
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isRunning = !isRunning;
        }

        // 处理蹲下（按X切换）
        if (Input.GetKeyDown(KeyCode.X))
        {
            isCrouching = !isCrouching;
            UpdateCrouchState();
        }

        float speed = moveSpeed;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // 应用奔跑速度
        if (isRunning && !isCrouching) // 蹲下时不能奔跑
        {
            speed *= runMultiplier;
        }
        
        // 应用蹲下速度
        if (isCrouching)
        {
            speed *= crouchSpeedMultiplier;
        }

        Vector3 camForward = cameraPoint.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraPoint.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 targetDir = camForward * v + camRight * h;
        targetDir.Normalize();

        // �����ƶ���ƽ��˥����
        currentVelocity = Vector3.Lerp(currentVelocity, targetDir * speed, inertiaFactor * Time.deltaTime);

        // ����
        bool isGrounded = controller.isGrounded;
        
        if (isGrounded)
        {
            if (yVelocity < 0)
                yVelocity = -2f;

            currentJumpCnt = jumpCnt;

            // 检测落地（从空中落地）
            if (!wasGrounded && landingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(landingSound);
            }

            // 播放脚步声（当玩家在移动时）
            if (targetDir.magnitude > 0.1f)
            {
                footstepTimer += Time.deltaTime;
                if (footstepTimer >= footstepInterval)
                {
                    if (footstepSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(footstepSound);
                    }
                    footstepTimer = 0f;
                }
            }
            else
            {
                footstepTimer = 0f; // 停止时重置计时器
            }
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
            footstepTimer = 0f; // 在空中时重置脚步声计时器
        }

        wasGrounded = isGrounded;

        // 蹲下时不能跳跃
        if (Input.GetKeyDown(KeyCode.Space) && currentJumpCnt > 0 && !isCrouching)
        {
            yVelocity = jumpForce;
            currentJumpCnt--;
        }

        Vector3 finalMove = currentVelocity + Vector3.up * yVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    void UpdateCrouchState()
    {
        if (isCrouching)
        {
            // 蹲下：降低高度和中心点
            controller.height = crouchHeight;
            Vector3 center = controller.center;
            center.y = originalCenterY - (originalHeight - crouchHeight) / 2f;
            controller.center = center;
            Debug.Log("[PlayerMovement] Crouching - Height: " + controller.height + ", Center: " + controller.center);
        }
        else
        {
            // 站起：强制恢复原始高度和中心点（不做顶部空间检测）
            // 需求：即使会“卡住”也允许站起，玩家自己承担风险
            controller.height = originalHeight;
            Vector3 center = controller.center;
            center.y = originalCenterY;
            controller.center = center;
            Debug.Log("[PlayerMovement] Standing up - Height: " + controller.height + ", Center: " + controller.center);
        }
    }

    void CameraControl()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitive * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitive * 100f * Time.deltaTime;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -80f, 80f);

        cameraPoint.localRotation = Quaternion.Euler(xRot, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
    }
}
