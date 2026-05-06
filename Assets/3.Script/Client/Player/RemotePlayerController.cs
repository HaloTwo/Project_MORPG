using System.Collections.Generic;
using UnityEngine;

public sealed class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private float interpolationDelay = 0.10f;
    [SerializeField] private float maxExtrapolationTime = 0.035f;
    [SerializeField] private float rotationLerpSpeed = 18.0f;
    [SerializeField] private float snapDistance = 5.0f;

    private readonly List<RemoteSnapshot> snapshots = new List<RemoteSnapshot>();
    private Quaternion targetRotation;

    public int ActorId { get; private set; }

    private struct RemoteSnapshot
    {
        public float Time;
        public Vector3 Position;
        public Vector3 Direction;
        public float MoveSpeed;
        public Quaternion Rotation;
        public bool Moving;
    }

    private void Awake()
    {
        targetRotation = transform.rotation;
        AddSnapshot(transform.position, Vector3.zero, transform.eulerAngles.y, 0.0f, false);
    }

    private void Update()
    {
        if (snapshots.Count == 0)
        {
            return;
        }

        Vector3 renderPosition = GetInterpolatedPosition(Time.time - interpolationDelay);
        if (Vector3.Distance(transform.position, renderPosition) > snapDistance)
        {
            transform.position = renderPosition;
        }
        else
        {
            transform.position = renderPosition;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
        TrimOldSnapshots();
    }

    // 서버가 부여한 원격 플레이어 ID를 초기화합니다.
    public void Initialize(int actorId)
    {
        ActorId = actorId;
    }

    /// 서버에서 받은 최신 위치와 회전값을 보간 버퍼에 저장합니다.
    public void SetTargetPosition(Vector3 position, float yaw)
    {
        AddSnapshot(position, Vector3.zero, yaw, 0.0f, false);
    }

    /// 이동 패킷은 바로 앞을 예측하지 않고, 짧은 버퍼 안에서 일정하게 재생합니다.
    public void SetMoveTarget(Vector3 position, Vector3 direction, float yaw, float moveSpeed)
    {
        Vector3 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        AddSnapshot(position, normalizedDirection, yaw, moveSpeed, true);
    }

    /// 정지 패킷은 서버가 알려준 최종 위치를 버퍼에 넣고 외삽을 멈춥니다.
    public void SetStopTarget(Vector3 position, float yaw)
    {
        AddSnapshot(position, Vector3.zero, yaw, 0.0f, false);
    }

    /// 수신 시각 기준 스냅샷을 쌓아 원격 캐릭터가 목표를 쫓지 않고 과거 위치 사이를 재생하게 합니다.
    private void AddSnapshot(Vector3 position, Vector3 direction, float yaw, float moveSpeed, bool moving)
    {
        RemoteSnapshot snapshot = new RemoteSnapshot
        {
            Time = Time.time,
            Position = position,
            Direction = direction,
            MoveSpeed = moveSpeed,
            Rotation = Quaternion.Euler(0.0f, yaw, 0.0f),
            Moving = moving
        };

        snapshots.Add(snapshot);
        targetRotation = snapshot.Rotation;
    }

    /// 보간 지점이 두 스냅샷 사이면 Lerp, 최신 스냅샷보다 뒤면 아주 짧게만 외삽합니다.
    private Vector3 GetInterpolatedPosition(float renderTime)
    {
        if (snapshots.Count == 1)
        {
            return snapshots[0].Position;
        }

        for (int i = 0; i < snapshots.Count - 1; i++)
        {
            RemoteSnapshot from = snapshots[i];
            RemoteSnapshot to = snapshots[i + 1];
            if (renderTime < from.Time || renderTime > to.Time)
            {
                continue;
            }

            float duration = Mathf.Max(to.Time - from.Time, 0.0001f);
            float t = Mathf.Clamp01((renderTime - from.Time) / duration);
            targetRotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
            return Vector3.Lerp(from.Position, to.Position, t);
        }

        RemoteSnapshot latest = snapshots[snapshots.Count - 1];
        targetRotation = latest.Rotation;
        if (!latest.Moving)
        {
            return latest.Position;
        }

        float extrapolationTime = Mathf.Clamp(renderTime - latest.Time, 0.0f, maxExtrapolationTime);
        return latest.Position + latest.Direction * latest.MoveSpeed * extrapolationTime;
    }

    /// 오래된 스냅샷은 버퍼에서 제거해 메모리와 탐색 비용을 제한합니다.
    private void TrimOldSnapshots()
    {
        float keepAfter = Time.time - 1.0f;
        while (snapshots.Count > 2 && snapshots[1].Time < keepAfter)
        {
            snapshots.RemoveAt(0);
        }
    }
}
