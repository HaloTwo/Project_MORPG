using UnityEngine;

public sealed class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.06f;
    [SerializeField] private float rotationLerpSpeed = 18.0f;
    [SerializeField] private float predictionTime = 0.10f;
    [SerializeField] private float snapDistance = 5.0f;

    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Quaternion targetRotation;

    public int ActorId { get; private set; }

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) > snapDistance)
        {
            transform.position = targetPosition;
            currentVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    // 서버가 부여한 원격 플레이어 ID를 초기화합니다.
    public void Initialize(int actorId)
    {
        ActorId = actorId;
    }

    /// 서버에서 받은 최신 위치와 회전값을 보간 이동 목표로 설정합니다.
    public void SetTargetPosition(Vector3 position, float yaw)
    {
        targetPosition = position;
        targetRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
    }

    /// 이동 패킷은 방향과 속도를 이용해 약간 앞의 위치를 목표로 잡아 끊김을 줄입니다.
    public void SetMoveTarget(Vector3 position, Vector3 direction, float yaw, float moveSpeed)
    {
        Vector3 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        targetPosition = position + normalizedDirection * moveSpeed * predictionTime;
        targetRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
    }

    /// 정지 패킷은 예측을 제거하고 서버가 알려준 최종 위치로 수렴시킵니다.
    public void SetStopTarget(Vector3 position, float yaw)
    {
        targetPosition = position;
        currentVelocity = Vector3.zero;
        targetRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
    }
}

