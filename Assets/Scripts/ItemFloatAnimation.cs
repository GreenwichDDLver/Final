using UnityEngine;

public class ItemFloatAnimation : MonoBehaviour
{
    [Header("旋转设置")]
    [SerializeField] bool enableRotation = true;
    [SerializeField] Vector3 rotationAxis = Vector3.up; // 默认绕Y轴旋转
    [SerializeField] float rotationSpeed = 90f; // 度/秒

    [Header("上下移动设置")]
    [SerializeField] bool enableFloat = true;
    [SerializeField] float floatHeight = 0.5f; // 移动高度范围（从起始位置上下各移动的高度）
    [SerializeField] float floatSpeed = 2f; // 上下移动速度

    private Vector3 startPosition;

    void Start()
    {
        // 记录起始位置
        startPosition = transform.position;
    }

    void Update()
    {
        // 旋转动画
        if (enableRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        // 上下浮动动画（使用正弦波实现平滑的上下移动）
        if (enableFloat)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
}