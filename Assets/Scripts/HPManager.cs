using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Events;

public class HPManager : MonoBehaviour
{
    [Header("HP Data")]
    [SerializeField] int curHP;
    [SerializeField] int maxHP;

    [Header("Events")]
    [SerializeField] UnityEvent hpChangeEvent;
    [SerializeField] UnityEvent HealEvent;
    [SerializeField] UnityEvent DieEvent;
    [SerializeField] UnityEvent AttackEvent;

    void Awake()
    {
        // 自动连接事件（如果引用丢失，尝试自动修复）
        AutoConnectEvents();
    }

    void AutoConnectEvents()
    {
        // 检查并自动连接 DieEvent 到 EnemyController
        bool dieEventConnected = false;
        for (int i = 0; i < DieEvent.GetPersistentEventCount(); i++)
        {
            if (DieEvent.GetPersistentTarget(i) != null)
            {
                dieEventConnected = true;
                break;
            }
        }
        
        if (!dieEventConnected)
        {
            // 先在当前对象查找，再在父对象查找
            EnemyController enemyController = GetComponent<EnemyController>();
            if (enemyController == null)
            {
                enemyController = GetComponentInParent<EnemyController>();
            }
            
            if (enemyController != null)
            {
                Debug.Log($"[HPManager] Auto-connecting DieEvent to {enemyController.gameObject.name}.EnemyController.Die");
                DieEvent.AddListener(enemyController.Die);
            }
            else
            {
                Debug.LogWarning($"[HPManager] {gameObject.name}: Could not find EnemyController to connect DieEvent!");
            }
        }
        else
        {
            // 即使有连接，也检查一下连接是否正确
            for (int i = 0; i < DieEvent.GetPersistentEventCount(); i++)
            {
                var target = DieEvent.GetPersistentTarget(i);
                if (target != null)
                {
                    Debug.Log($"[HPManager] {gameObject.name}: DieEvent connected to {target.name}.{DieEvent.GetPersistentMethodName(i)}");
                }
            }
        }

        // 检查并自动连接 AttackEvent 到 AttackAnimation
        bool attackEventConnected = false;
        for (int i = 0; i < AttackEvent.GetPersistentEventCount(); i++)
        {
            if (AttackEvent.GetPersistentTarget(i) != null)
            {
                attackEventConnected = true;
                break;
            }
        }
        
        if (!attackEventConnected)
        {
            AttackAnimation attackAnim = GetComponent<AttackAnimation>();
            if (attackAnim != null)
            {
                AttackEvent.AddListener(attackAnim.Play);
            }
        }

        // 检查 hpChangeEvent（需要参数，保持序列化方式，但确保有引用）
        bool hpChangeEventConnected = false;
        for (int i = 0; i < hpChangeEvent.GetPersistentEventCount(); i++)
        {
            if (hpChangeEvent.GetPersistentTarget(i) != null)
            {
                hpChangeEventConnected = true;
                break;
            }
        }
        
        if (!hpChangeEventConnected)
        {
            EnemyUIController uiController = GetComponentInChildren<EnemyUIController>();
            if (uiController != null)
            {
                // 使用lambda包装，因为RereshHealthBar需要HPManager参数
                hpChangeEvent.AddListener(() => uiController.RereshHealthBar(this));
            }
        }
    }

    public void Heal(int hp)
    {
        curHP += hp;
        if (curHP > maxHP) curHP = maxHP;

        hpChangeEvent?.Invoke();
        HealEvent?.Invoke();
    }
    
    // 直接设置HP（用于复活时恢复满血）
    public void SetHP(int hp)
    {
        curHP = Mathf.Clamp(hp, 0, maxHP);
        hpChangeEvent?.Invoke();
    }
    
    // 恢复满血
    public void RestoreFullHP()
    {
        curHP = maxHP;
        hpChangeEvent?.Invoke();
    }

    public float GetRate() { return (float)curHP /(float) maxHP; }
    public void Attack(int hp)
    {
        int oldHP = curHP;
        curHP -= hp;
        if (curHP < 0) curHP = 0;

        Debug.Log($"[HPManager] {gameObject.name}: HP {oldHP} -> {curHP} (damage: {hp}, maxHP: {maxHP})");

        hpChangeEvent?.Invoke();
        AttackEvent?.Invoke();

        if (curHP == 0)
        {
            Debug.Log($"[HPManager] {gameObject.name}: HP reached 0, invoking DieEvent. Event count: {DieEvent.GetPersistentEventCount()}");
            
            // 检查是否是玩家
            if (gameObject.CompareTag("Player"))
            {
                // 玩家死亡，通知PlayerManager
                PlayerManager playerManager = GetComponent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.OnPlayerDeath();
                }
                else
                {
                    Debug.LogWarning("[HPManager] Player died but PlayerManager not found!");
                }
            }
            else
            {
                // 敌人死亡逻辑
                // 先尝试调用事件
                DieEvent?.Invoke();
                
                // 同时直接查找并调用父对象的EnemyController（确保一定会执行）
                EnemyController enemyController = GetComponentInParent<EnemyController>();
                if (enemyController != null)
                {
                    Debug.Log($"[HPManager] Directly calling {enemyController.gameObject.name}.Die() as backup");
                    enemyController.Die();
                }
                else
                {
                    Debug.LogWarning($"[HPManager] {gameObject.name}: HP is 0 but no EnemyController found in parent!");
                }
            }
        }
    }
}
