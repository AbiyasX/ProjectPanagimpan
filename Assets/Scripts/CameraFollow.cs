using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3f;        
    public bool followX = false;            
    public bool followY = true;
    public Vector2 offset = Vector2.zero;

    Vector3 velocity;

    public void SetTarget(Transform newTarget, bool snap)
    {
        target = newTarget;
        if (snap && target != null)
            transform.position = GetTargetPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position, GetTargetPosition(), ref velocity, smoothTime);
    }

    Vector3 GetTargetPosition()
    {
        Vector3 pos = transform.position;   
        if (followX) pos.x = target.position.x + offset.x;
        if (followY) pos.y = target.position.y + offset.y;
        return pos;
    }
}