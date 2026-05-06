using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class QuarterViewPlayerController : MonoBehaviour
{
    [Header("Actor")]
    [SerializeField] private int actorId = 1;
    [SerializeField] private ClassType classType = ClassType.None;

    [Header("Joystick Move")]
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float rotationSpeed = 16.0f;
    [SerializeField] private bool sendMovementPackets = true;
    [SerializeField] private float movePacketInterval = 0.12f;
    [SerializeField] private float inputDeadZone = 0.12f;

    private CharacterController characterController;
    private Vector3 lastMoveDirection;
    private bool wasMoving;
    private float verticalVelocity;
    private float nextMovePacketTime;

    public event Action<MovePacket> OnMoveStateChanged;
    public event Action<StopPacket> OnStopStateChanged;

    public int ActorId => actorId;
    public ClassType ClassType => classType;
    public bool IsMoving => wasMoving;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 moveDirection = ReadMoveDirection();
        Move(moveDirection);
        Rotate(moveDirection);
        DetectMoveState(moveDirection);
    }

    // 서버에서 선택된 캐릭터 데이터를 받아 플레이어 ID, 직업, 위치를 적용합니다.
    public void InitializeFromCharacter(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        actorId = character.CharacterId;
        classType = character.ClassType;
        Teleport(character.Position);
    }

    // 서버 보정이나 캐릭터 입장 시 즉시 위치를 옮깁니다.
    public void Teleport(Vector3 position)
    {
        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = position;
        wasMoving = false;
        characterController.enabled = wasEnabled;
    }

    // 모바일 우선 구조라 이동 입력은 가상 조이스틱에서만 읽습니다.
    private Vector3 ReadMoveDirection()
    {
        Vector2 input = VirtualJoystick.Instance != null ? VirtualJoystick.Instance.InputVector : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1.0f);
        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return Vector3.zero;
        }

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0.0f;
        right.y = 0.0f;
        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    // CharacterController를 사용해 조이스틱 방향으로 이동하고 간단한 중력을 적용합니다.
    private void Move(Vector3 moveDirection)
    {
        if (characterController.isGrounded && verticalVelocity < 0.0f)
        {
            verticalVelocity = -1.0f;
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    // 이동 방향을 바라보도록 캡슐을 회전시킵니다.
    private void Rotate(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // 이동 상태를 매 프레임 검사하되, 패킷은 시작/방향 변경/주기적 보정/정지 시점에만 보냅니다.
    private void DetectMoveState(Vector3 moveDirection)
    {
        bool isMoving = moveDirection.sqrMagnitude > 0.001f;

        if (isMoving)
        {
            bool shouldSend = !wasMoving || Time.time >= nextMovePacketTime || Vector3.Distance(moveDirection, lastMoveDirection) > 0.25f;
            if (shouldSend)
            {
                SendMovePacket(moveDirection);
                nextMovePacketTime = Time.time + movePacketInterval;
            }

            lastMoveDirection = moveDirection;
            wasMoving = true;
            return;
        }

        if (!wasMoving)
        {
            return;
        }

        wasMoving = false;
        SendStopPacket();
    }

    // 이동 중 서버로 보낼 MovePacket을 만듭니다.
    private void SendMovePacket(Vector3 direction)
    {
        MovePacket packet = new MovePacket(actorId, transform.position, direction, transform.eulerAngles.y, moveSpeed);
        OnMoveStateChanged?.Invoke(packet);

        if (sendMovementPackets)
        {
            NetworkManager.Instance.SendPacket(packet);
        }
    }

    // 조이스틱을 놓았을 때 서버로 보낼 StopPacket을 만듭니다.
    private void SendStopPacket()
    {
        StopPacket packet = new StopPacket(actorId, transform.position, transform.eulerAngles.y);
        OnStopStateChanged?.Invoke(packet);

        if (sendMovementPackets)
        {
            NetworkManager.Instance.SendPacket(packet);
        }
    }
}
