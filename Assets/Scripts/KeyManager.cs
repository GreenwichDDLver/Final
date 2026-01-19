using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class KeyManager : Singleton<KeyManager>
{
    [Header("Key Settings")]
    [SerializeField] int requiredKeys = 5; // 需要的钥匙数量
    [SerializeField] int currentKeyCount = 0; // 当前收集的钥匙数量

    [Header("Door Settings")]
    [SerializeField] GameObject doorPrefab; // 大门Prefab（可选，如果为空则使用Door脚本）
    [SerializeField] Transform doorSpawnPoint; // 大门生成位置（可选）
    [SerializeField] Door doorScript; // 直接引用场景中的Door脚本（可选）

    [Header("Events")]
    public UnityEvent OnAllKeysCollected; // 收集完所有钥匙时触发的事件

    private bool doorSpawned = false; // 大门是否已生成

    protected override void Awake()
    {
        base.Awake();
        if (instance == this)
        {
            currentKeyCount = 0;
            doorSpawned = false;
            Debug.Log($"[KeyManager] Initialized. Required keys: {requiredKeys}");
        }
    }

    void Start()
    {
        // 确保初始化完成
    }

    // 添加钥匙
    public void AddKey()
    {
        if (doorSpawned) return; // 如果大门已生成，不再添加钥匙

        currentKeyCount++;
        Debug.Log($"[KeyManager] Key collected! Current: {currentKeyCount}/{requiredKeys}");

        // 检查是否收集完所有钥匙
        if (currentKeyCount >= requiredKeys)
        {
            OnAllKeysCollected?.Invoke();
            SpawnDoor();
        }
    }

    // 获取当前钥匙数量
    public int GetKeyCount()
    {
        return currentKeyCount;
    }

    // 获取需要的钥匙数量
    public int GetRequiredKeys()
    {
        return requiredKeys;
    }

    // 生成大门
    private void SpawnDoor()
    {
        if (doorSpawned) return; // 防止重复生成

        doorSpawned = true;
        Debug.Log("[KeyManager] All keys collected! Spawning door...");

        // 方法1: 如果指定了Door脚本引用，直接激活它
        if (doorScript != null)
        {
            doorScript.OpenDoor();
            Debug.Log("[KeyManager] Door opened via Door script reference.");
            return;
        }

        // 方法2: 如果指定了Door Prefab和生成位置，实例化Prefab
        if (doorPrefab != null && doorSpawnPoint != null)
        {
            GameObject door = Instantiate(doorPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);
            Debug.Log($"[KeyManager] Door spawned at position: {doorSpawnPoint.position}");
            return;
        }

        // 方法3: 查找场景中已存在的Door脚本
        Door existingDoor = FindObjectOfType<Door>();
        if (existingDoor != null)
        {
            existingDoor.OpenDoor();
            Debug.Log("[KeyManager] Door found in scene and opened.");
            return;
        }

        Debug.LogWarning("[KeyManager] No door prefab, spawn point, or Door script found! Please assign one in Inspector.");
    }

    // 重置钥匙数量（用于新关卡）
    public void ResetKeys()
    {
        currentKeyCount = 0;
        doorSpawned = false;
        Debug.Log("[KeyManager] Keys reset.");
    }
}
