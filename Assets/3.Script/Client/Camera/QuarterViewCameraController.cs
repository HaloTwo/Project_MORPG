using UnityEngine;

public sealed class QuarterViewCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 17.0f;
    [SerializeField] private float minDistance = 8.0f;
    [SerializeField] private float maxDistance = 25.0f;
    [SerializeField] private float pitch = 50.0f;
    [SerializeField] private float minPitch = 35.0f;
    [SerializeField] private float maxPitch = 68.0f;
    [SerializeField] private float yaw;
    [SerializeField] private float smoothTime = 0.10f;

    private Vector3 followVelocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
    }

    // 화면 드래그 입력으로 카메라 좌우 회전과 위아래 각도를 갱신합니다.
    public void AddOrbit(float deltaYaw, float deltaPitch)
    {
        yaw += deltaYaw;
        pitch = Mathf.Clamp(pitch + deltaPitch, minPitch, maxPitch);
    }

    // 두 손가락 핀치 입력으로 플레이어 중심 카메라 거리를 조절합니다.
    public void AddZoom(float deltaDistance)
    {
        distance = Mathf.Clamp(distance + deltaDistance, minDistance, maxDistance);
    }

    // 고정된 쿼터뷰 각도에서 플레이어를 부드럽게 따라갑니다.
    private void FollowTarget()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 desiredPosition = target.position + orbitRotation * new Vector3(0.0f, 0.0f, -distance);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
        transform.rotation = orbitRotation;
    }
}
