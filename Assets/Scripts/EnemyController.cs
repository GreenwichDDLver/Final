using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    enum EnemyState
    {
        Idle,
        Chase,
        GoLastPos,
        Wait,
        Return
    }

    [Header("Setting")]
    [SerializeField] float chaseDistance = 10f;
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] GunController gunController;
    [SerializeField] Transform cameraPoint;
    [SerializeField] float angularVelocity = 120f;
    [SerializeField] float stopDistance = 2f;
    [SerializeField] float stopTime = 2f;
    [SerializeField] float shootDistance = 8f; // 射击距离，到达这个距离时停止并射击
    [SerializeField] float shootAngleThreshold = 30f; // 射击角度阈值（度），只有当敌人面向玩家角度差小于此值时才射击

    [Header("掉落物设置")]
    [SerializeField] GameObject dropItem1; // 第一种掉落物（弹药箱/心脏等）
    [SerializeField] GameObject dropItem2; // 第二种掉落物（弹药箱/心脏等）
    [SerializeField] float dropRate = 1f; // 掉落概率（0-1，1表示100%掉落）

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] RuntimeAnimatorController animatorController;

    [Header("Audio")]
    [SerializeField] AudioClip deathSound; // 死亡音效
    [SerializeField] AudioSource audioSource; // 音频源组件

    NavMeshAgent agent;
    Vector3 initPos;
    Vector3 lastPlayerPos;

    EnemyState curState;
    string currentAnimationState = "";
    bool isDying = false; // 是否正在死亡

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = angularVelocity;
        agent.stoppingDistance = stopDistance;
        initPos = transform.position;
        agent.SetDestination(initPos);
        curState = EnemyState.Idle;

        // 查找士兵模型子对象
        Transform soldierModel = null;
        soldierModel = transform.Find("Soldier_marine_support");
        if (soldierModel == null)
        {
            soldierModel = transform.Find("Soldier_marine_sniper");
        }
        if (soldierModel == null)
        {
            // 尝试找第一个包含Renderer的子对象（可能是士兵模型）
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r.transform != transform && r.transform.parent == transform)
                {
                    // 检查是否包含Animator或Animation组件（士兵模型的特征）
                    if (r.GetComponent<Animator>() != null || r.GetComponent<Animation>() != null)
                    {
                        soldierModel = r.transform;
                        break;
                    }
                }
            }
        }
        
        // 调整士兵模型位置，确保它贴地
        if (soldierModel != null)
        {
            // 士兵模型的pivot可能在中心，需要向下偏移
            // 假设士兵高度约为1.8米，pivot在中心，所以需要向下移动0.9米
            Bounds bounds = GetRenderBounds(soldierModel);
            float modelBottomOffset = bounds.center.y - bounds.min.y;
            
            if (soldierModel.localPosition.y != -modelBottomOffset)
            {
                Vector3 pos = soldierModel.localPosition;
                pos.y = -modelBottomOffset;
                soldierModel.localPosition = pos;
                Debug.Log($"[EnemyController] {gameObject.name}: Adjusted soldier model position by {-modelBottomOffset} units to align with ground");
            }
        }

        // 如果没有手动分配Animator，尝试在子对象（士兵模型）上查找
        if (animator == null)
        {
            if (soldierModel != null)
            {
                animator = soldierModel.GetComponent<Animator>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        // 如果还是找不到，尝试添加Animator组件
        if (animator == null && soldierModel != null)
        {
            animator = soldierModel.gameObject.AddComponent<Animator>();
        }

        // 如果有Animator Controller，设置它
        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            animator.enabled = true; // 确保Animator启用
        }

        // 调试日志
        if (animator == null)
        {
            Debug.LogWarning($"[EnemyController] {gameObject.name}: Animator component not found! Animation will not play.");
        }
        else
        {
            Debug.Log($"[EnemyController] {gameObject.name}: Animator found on {animator.gameObject.name}, Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "None")}, Enabled: {animator.enabled}, UpdateMode: {animator.updateMode}");
            
            // 检查Animator的当前状态
            if (animator.runtimeAnimatorController != null)
            {
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    Debug.Log($"[EnemyController] Current playing clip: {clipInfo[0].clip.name}");
                }
            }
        }

        // 设置初始动画
        UpdateAnimation();

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

    void Update()
    {
        // 如果正在死亡，不更新状态
        if (isDying) return;

        // 检查PlayerManager是否存在
        if (PlayerManager.instance == null)
        {
            return; // PlayerManager未初始化，跳过更新
        }

        Vector3 playerPos = PlayerManager.instance.GetPlayerPosition();
        float dis = Vector3.Distance(transform.position, playerPos);

        switch (curState)
        {
            case EnemyState.Idle:
                IdleUpdate(dis, playerPos);
                break;

            case EnemyState.Chase:
                ChaseUpdate(dis, playerPos);
                break;

            case EnemyState.GoLastPos:
                GoLastPosUpdate(dis, playerPos);
                break;

            case EnemyState.Wait:
                break;

            case EnemyState.Return:
                ReturnUpdate(dis, playerPos);
                break;
        }
    }

    void IdleUpdate(float dis, Vector3 playerPos)
    {

        if (dis <= chaseDistance)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    void ChaseUpdate(float dis, Vector3 playerPos)
    {
        if (dis > chaseDistance)
        {
            lastPlayerPos = playerPos;
            agent.SetDestination(lastPlayerPos);
            ChangeState(EnemyState.GoLastPos);
            return;
        }

        // 根据距离决定是否停止移动并射击
        if (dis <= shootDistance)
        {
            // 停止移动，原地射击
            agent.SetDestination(transform.position);
            cameraPoint.LookAt(playerPos);
            
            // 只有当面向玩家时才射击
            if (IsFacingPlayer(playerPos))
            {
                gunController.Fire(false);
            }
        }
        else
        {
            // 继续追击
            agent.SetDestination(playerPos);
            cameraPoint.LookAt(playerPos);
            
            // 只有当面向玩家时才射击
            if (IsFacingPlayer(playerPos))
            {
                gunController.Fire(false);
            }
        }
        
        // 根据距离更新动画（可能会在Chase状态内切换）
        UpdateAnimationForChase(dis);
    }
    
    void UpdateAnimationForChase(float distanceToPlayer)
    {
        if (animator == null) return;
        
        if (distanceToPlayer <= shootDistance)
        {
            PlayAnimation("combat_shoot");
        }
        else
        {
            PlayAnimation("combat_run");
        }
    }

    void GoLastPosUpdate(float dis, Vector3 playerPos)
    {
        if (dis <= chaseDistance)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // 确保在前往最后位置时持续播放walk动画
        if (animator != null && currentAnimationState != "combat_walk")
        {
            PlayAnimation("combat_walk");
        }

        if (Vector3.Distance(transform.position, lastPlayerPos) <= stopDistance)
        {
            ChangeState(EnemyState.Wait);
            StartCoroutine(WaitRoutine());
        }
    }

    void ReturnUpdate(float dis, Vector3 playerPos)
    {
        if (dis <= chaseDistance)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        agent.SetDestination(initPos);

        // 确保在返回初始位置时持续播放walk动画（直到到达目标）
        if (animator != null && currentAnimationState != "combat_walk")
        {
            PlayAnimation("combat_walk");
        }

        if (Vector3.Distance(transform.position, initPos) <= stopDistance)
        {
            ChangeState(EnemyState.Idle);
        }
    }

    void ChangeState(EnemyState newState)
    {
        if (curState != newState)
        {
            curState = newState;
            UpdateAnimation();
        }
    }

    void UpdateAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[EnemyController] {gameObject.name}: UpdateAnimation called but animator is null!");
            return;
        }

        string targetAnimation = "";
        switch (curState)
        {
            case EnemyState.Idle:
                targetAnimation = "combat_idle";
                break;

            case EnemyState.Chase:
                // Chase状态的动画会在ChaseUpdate中根据距离动态更新
                // 这里先设置默认的跑步动画
                targetAnimation = "combat_run";
                break;

            case EnemyState.GoLastPos:
            case EnemyState.Return:
                targetAnimation = "combat_walk";
                break;

            case EnemyState.Wait:
                targetAnimation = "combat_idle";
                break;
        }

        if (!string.IsNullOrEmpty(targetAnimation))
        {
            Debug.Log($"[EnemyController] {gameObject.name}: UpdateAnimation - State: {curState}, Target: {targetAnimation}, Current: {currentAnimationState}");
            PlayAnimation(targetAnimation);
        }
    }

    void PlayAnimation(string animationName)
    {
        if (animator == null)
        {
            // 每帧都尝试查找Animator，以防它在Start之后才被添加
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null && animatorController != null)
                {
                    animator.runtimeAnimatorController = animatorController;
                }
            }
            if (animator == null)
            {
                Debug.LogWarning($"[EnemyController] {gameObject.name}: PlayAnimation - Animator is null!");
                return;
            }
        }
        
        if (animationName == currentAnimationState)
        {
            Debug.Log($"[EnemyController] {gameObject.name}: PlayAnimation - Animation '{animationName}' already playing, skipping");
            return;
        }

        // 使用Animator的Play方法播放动画
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[EnemyController] {gameObject.name}: Animator has no Controller assigned! Cannot play animation '{animationName}'");
            return;
        }

        // 确保Animator已启用
        if (!animator.enabled)
        {
            animator.enabled = true;
        }

        Debug.Log($"[EnemyController] {gameObject.name}: PlayAnimation - Attempting to play '{animationName}' (Controller: {animator.runtimeAnimatorController.name}, Enabled: {animator.enabled})");
        
        // 使用Animator的Play方法播放动画（使用layer 0，normalizedTime = 0表示从开始播放）
        animator.Play(animationName, 0, 0f);
        currentAnimationState = animationName;
        
        // 等待一帧后检查动画是否真的在播放
        StartCoroutine(VerifyAnimationPlaying(animationName));
        
        Debug.Log($"[EnemyController] {gameObject.name}: PlayAnimation called for '{animationName}'");
    }

    IEnumerator WaitRoutine()
    {
        yield return new WaitForSeconds(stopTime);
        ChangeState(EnemyState.Return);
    }

    IEnumerator VerifyAnimationPlaying(string animationName)
    {
        yield return null; // 等待一帧，让Animator有时间更新
        
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            
            if (clipInfo.Length > 0)
            {
                string actualClipName = clipInfo[0].clip.name;
                bool isPlaying = stateInfo.IsName(animationName) || actualClipName.Contains(animationName);
                
                Debug.Log($"[EnemyController] {gameObject.name}: Animation verification - Expected: '{animationName}', Actual: '{actualClipName}', IsPlaying: {isPlaying}, NormalizedTime: {stateInfo.normalizedTime}");
                
                if (!isPlaying)
                {
                    Debug.LogWarning($"[EnemyController] {gameObject.name}: Animation '{animationName}' may not be playing! Check if the animation clip is properly imported as Generic type.");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyController] {gameObject.name}: No animation clip info found! Animation may not be assigned correctly.");
            }
        }
    }

    // 获取Transform及其所有子对象的渲染边界
    Bounds GetRenderBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
        
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r.enabled) bounds.Encapsulate(r.bounds);
        }
        
        // 转换为本地空间
        Vector3 localCenter = root.InverseTransformPoint(bounds.center);
        Vector3 localSize = bounds.size;
        localSize.x /= root.lossyScale.x;
        localSize.y /= root.lossyScale.y;
        localSize.z /= root.lossyScale.z;
        
        return new Bounds(localCenter, localSize);
    }

    // 检查敌人是否面向玩家
    bool IsFacingPlayer(Vector3 playerPos)
    {
        // 计算敌人到玩家的方向向量
        Vector3 dirToPlayer = (playerPos - transform.position).normalized;
        
        // 获取敌人的前方向量（使用cameraPoint的forward，因为这是武器的朝向）
        Vector3 enemyForward = cameraPoint.forward;
        
        // 计算两个向量之间的角度
        float angle = Vector3.Angle(enemyForward, dirToPlayer);
        
        // 如果角度小于阈值，说明面向玩家
        return angle <= shootAngleThreshold;
    }

    public void Die()
    {
        // 如果已经在死亡过程中，不重复执行
        if (isDying) return;

        Debug.Log($"[EnemyController] Die() called on {gameObject.name}");
        
        // 标记为正在死亡
        isDying = true;

        // 停止NavMeshAgent移动
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 停止所有协程（除了死亡协程）
        StopAllCoroutines();

        // 播放死亡动画并等待后销毁
        StartCoroutine(DieRoutine());
    }

    // 死亡协程：播放死亡动画，然后等待4秒后销毁
    IEnumerator DieRoutine()
    {
        // 播放死亡音效
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 随机掉落物品
        DropRandomItem();

        // 播放死亡动画
        if (animator != null)
        {
            // 随机选择死亡动画（death_A 或 death_B）
            string[] deathAnimations = { "death_A", "death_B" };
            string selectedDeathAnim = deathAnimations[Random.Range(0, deathAnimations.Length)];
            PlayAnimation(selectedDeathAnim);
            
            // 等待动画播放完成（获取动画长度）
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animLength = stateInfo.length;
            
            // 如果获取不到长度，使用默认值（约1-2秒）
            if (animLength <= 0.1f)
            {
                animLength = 2f; // 默认死亡动画长度
            }
            
            Debug.Log($"[EnemyController] Playing death animation, length: {animLength} seconds");
            
            // 等待动画播放完成
            yield return new WaitForSeconds(animLength);
        }
        else
        {
            // 没有Animator时，直接等待
            yield return new WaitForSeconds(0.5f);
        }

        // 动画播放完成后，再等待4秒
        yield return new WaitForSeconds(4f);

        // 销毁敌人
        Destroy(gameObject);
    }

    // 随机掉落物品
    void DropRandomItem()
    {
        // 检查是否掉落（根据掉落概率）
        if (Random.value > dropRate)
        {
            return; // 不掉落
        }

        // 检查是否有配置掉落物
        if (dropItem1 == null && dropItem2 == null)
        {
            Debug.LogWarning($"[EnemyController] {gameObject.name}: No drop items configured!");
            return;
        }

        // 如果只有一个掉落物，直接使用它
        GameObject itemToDrop = null;
        if (dropItem1 == null)
        {
            itemToDrop = dropItem2;
        }
        else if (dropItem2 == null)
        {
            itemToDrop = dropItem1;
        }
        else
        {
            // 两个掉落物都存在，随机选择
            itemToDrop = Random.value < 0.5f ? dropItem1 : dropItem2;
        }

        // 生成掉落物（在敌人位置生成，稍微抬高一点）
        if (itemToDrop != null)
        {
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f; // 稍微抬高，避免与地面重叠
            Instantiate(itemToDrop, dropPosition, Quaternion.identity);
            Debug.Log($"[EnemyController] {gameObject.name}: Dropped item: {itemToDrop.name} at position {dropPosition}");
        }
    }
}
