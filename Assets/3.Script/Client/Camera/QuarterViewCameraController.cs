using UnityEngine;

public sealed class QuarterViewCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0.0f, 13.0f, -11.0f);
    [SerializeField] private Vector3 rotationEuler = new Vector3(50.0f, 0.0f, 0.0f);
    [SerializeField] private float smoothTime = 0.10f;

    private Vector3 followVelocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
    }

    // 고정된 쿼터뷰 각도에서 플레이어를 부드럽게 따라갑니다.
    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
        transform.rotation = Quaternion.Euler(rotationEuler);
    }
}
