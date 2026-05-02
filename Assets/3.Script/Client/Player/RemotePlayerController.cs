using UnityEngine;

public sealed class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private float positionLerpSpeed = 12.0f;
    [SerializeField] private float rotationLerpSpeed = 14.0f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public int ActorId { get; private set; }

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    // 서버가 부여한 원격 플레이어 ID를 초기화합니다.
    public void Initialize(int actorId)
    {
        ActorId = actorId;
    }

    // 서버에서 받은 최신 위치와 회전값을 보간 이동 목표로 설정합니다.
    public void SetTargetPosition(Vector3 position, float yaw)
    {
        targetPosition = position;
        targetRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
    }
}

