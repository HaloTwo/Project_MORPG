using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    // 가상 조이스틱과 키보드 입력을 카메라 기준 쿼터뷰 이동 방향으로 변환합니다.
    private Vector3 ReadMoveDirection()
    {
        Vector2 input = Vector2.zero;

        if (VirtualJoystick.Instance != null)
        {
            input = VirtualJoystick.Instance.InputVector;
        }

        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            input = ReadKeyboardInput();
        }

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

    // 에디터 테스트용 키보드 입력도 함께 지원합니다.
    private Vector2 ReadKeyboardInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1.0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1.0f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1.0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1.0f;
            return input;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
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

    // 이동 시작, 이동 중 방향 갱신, 정지 시점에 패킷 이벤트를 발생시킵니다.
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
