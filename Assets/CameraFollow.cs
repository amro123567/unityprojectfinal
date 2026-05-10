using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(0f, 2f);

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;

    void LateUpdate()
    {
        if (player == null) return;

        // Target position
        Vector3 targetPosition = new Vector3(
            player.position.x + offset.x,
            player.position.y + offset.y,
            -10f  // Keep camera at -10 on Z axis for 2D
        );

        // Apply bounds if enabled
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // Smooth follow
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
