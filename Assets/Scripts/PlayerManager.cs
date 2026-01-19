using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    [SerializeField] WeaponWheel weaponWheel;
    
    private Vector3 respawnPoint; // 重生点位置
    private bool isDead = false; // 玩家是否死亡
    
    void Start()
    {
        // 将当前场景的玩家位置设置为重生点
        respawnPoint = transform.position;
        Debug.Log($"[PlayerManager] Respawn point set to: {respawnPoint}");
    }
    
    public Vector3 GetPlayerPosition() 
    {
        return transform.position;
    }

    public void AddMags(int cnt) 
    {
        weaponWheel.AddMags(cnt);
    }
    
    // 设置新的重生点（可以用于检查点系统）
    public void SetRespawnPoint(Vector3 newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
        Debug.Log($"[PlayerManager] Respawn point updated to: {respawnPoint}");
    }
    
    // 获取重生点
    public Vector3 GetRespawnPoint()
    {
        return respawnPoint;
    }
    
    // 玩家死亡
    public void OnPlayerDeath()
    {
        if (isDead) return; // 防止重复调用
        
        isDead = true;
        Debug.Log("[PlayerManager] Player died, starting respawn countdown...");
        
        // 禁用玩家控制
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // 3秒后复活
        StartCoroutine(RespawnAfterDelay(3f));
    }
    
    // 复活玩家
    public void RespawnPlayer()
    {
        Debug.Log($"[PlayerManager] Respawning player at: {respawnPoint}");
        
        // 传送玩家到重生点
        transform.position = respawnPoint;
        
        // 恢复生命值
        HPManager hpManager = GetComponent<HPManager>();
        if (hpManager != null)
        {
            hpManager.RestoreFullHP(); // 恢复满血
        }
        else
        {
            Debug.LogWarning("[PlayerManager] HPManager not found on player!");
        }
        
        // 恢复玩家控制
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        isDead = false;
        Debug.Log("[PlayerManager] Player respawned!");
    }
    
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RespawnPlayer();
    }
    
    public bool IsDead()
    {
        return isDead;
    }
}
