using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private QuarterViewPlayerController localPlayerPrefab;
    [SerializeField] private RemotePlayerController remotePlayerPrefab;
    [SerializeField] private Transform playerRoot;

    private readonly Dictionary<int, RemotePlayerController> remotePlayers = new Dictionary<int, RemotePlayerController>();
    private QuarterViewPlayerController localPlayer;
    private PacketDispatcher subscribedDispatcher;

    public QuarterViewPlayerController LocalPlayer => localPlayer;
    public IReadOnlyDictionary<int, RemotePlayerController> RemotePlayers => remotePlayers;

    private void Awake()
    {
        if (playerRoot == null)
        {
            playerRoot = transform;
        }
    }

    private void OnEnable()
    {
        SubscribeDispatcher();
    }

    private void OnDisable()
    {
        UnsubscribeDispatcher();
    }

    private void OnDestroy()
    {
        UnsubscribeDispatcher();
    }

    private void Start()
    {
        PrepareLocalPlayer();
        NetworkManager.Instance.Connect();
        ApplySelectedCharacterIfExists();
        BuildReturnButton();
    }

    // 씬에 이미 놓인 플레이어를 먼저 찾고, 없으면 프리팹으로 생성합니다.
    private void PrepareLocalPlayer()
    {
        localPlayer = FindFirstObjectByType<QuarterViewPlayerController>();

        if (localPlayer == null && localPlayerPrefab != null)
        {
            localPlayer = Instantiate(localPlayerPrefab, Vector3.zero, Quaternion.identity, playerRoot);
        }

        if (localPlayer == null)
        {
            localPlayer = CreateFallbackLocalPlayer();
        }

        AttachCameraToLocalPlayer();
    }

    // Main Camera가 로컬 플레이어를 따라가도록 연결합니다.
    private void AttachCameraToLocalPlayer()
    {
        if (localPlayer == null || Camera.main == null)
        {
            return;
        }

        QuarterViewCameraController cameraController = Camera.main.GetComponent<QuarterViewCameraController>();
        if (cameraController != null)
        {
            cameraController.Target = localPlayer.transform;
        }
    }

    /// <summary>
    /// 이미 캐릭터 선택 씬에서 선택된 캐릭터가 있으면 게임 씬 시작 시 바로 적용합니다.
    /// </summary>
    private void ApplySelectedCharacterIfExists()
    {
        CharacterData selectedCharacter = CharacterSession.Instance.SelectedCharacter;
        if (selectedCharacter != null)
        {
            ApplyCharacterToLocalPlayer(selectedCharacter);
        }
    }

    // 선택 캐릭터 데이터를 로컬 플레이어에 적용합니다.
    private void ApplyCharacterToLocalPlayer(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        PrepareLocalPlayer();
        if (localPlayer == null)
        {
            Debug.LogError("[GameManager] LocalPlayer를 찾거나 생성하지 못했습니다.");
            return;
        }

        localPlayer.InitializeFromCharacter(character);

        SkillController skillController = localPlayer.GetComponent<SkillController>();
        if (skillController != null)
        {
            skillController.InitializeFromCharacter(character);
        }

        ApplyClassVisual(character.ClassType);
        Debug.Log($"[GameManager] Entered as {character.Name} / {character.GetClassNameKr()}");
    }

    // 캐릭터 입장 성공 시 로컬 플레이어에 ID, 직업, 위치, 스킬을 적용합니다.
    private void HandleEnterGameResponse(EnterGameResponsePacket packet)
    {
        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        if (!packet.Success || packet.Character == null)
        {
            Debug.LogWarning($"[GameManager] EnterGame failed: {packet.Message}");
            return;
        }

        CharacterSession.Instance.SetSelectedCharacter(packet.Character);
        ApplyCharacterToLocalPlayer(packet.Character);
    }

    // 임시 캡슐 색으로 직업 차이를 보여줍니다.
    private void ApplyClassVisual(ClassType classType)
    {
        if (localPlayer == null)
        {
            return;
        }

        Renderer renderer = localPlayer.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        switch (classType)
        {
            case ClassType.Warrior:
                renderer.material.color = new Color(0.9f, 0.25f, 0.2f, 1.0f);
                break;
            case ClassType.Archer:
                renderer.material.color = new Color(0.2f, 0.75f, 0.25f, 1.0f);
                break;
            case ClassType.Rogue:
                renderer.material.color = new Color(0.45f, 0.25f, 0.9f, 1.0f);
                break;
        }
    }

    // 서버 데이터 기준으로 원격 플레이어를 생성하거나 위치를 갱신합니다.
    public RemotePlayerController SpawnRemotePlayer(int actorId, Vector3 position, float yaw)
    {
        if (remotePlayers.TryGetValue(actorId, out RemotePlayerController existing))
        {
            existing.SetTargetPosition(position, yaw);
            return existing;
        }

        if (remotePlayerPrefab == null)
        {
            Debug.LogWarning("[GameManager] Remote player prefab이 아직 지정되지 않았습니다.");
            return null;
        }

        RemotePlayerController remotePlayer = Instantiate(remotePlayerPrefab, position, Quaternion.Euler(0.0f, yaw, 0.0f), playerRoot);
        remotePlayer.Initialize(actorId);
        remotePlayer.SetTargetPosition(position, yaw);
        remotePlayers.Add(actorId, remotePlayer);
        return remotePlayer;
    }

    // 나중에 Despawn 패킷을 받았을 때 원격 플레이어를 제거합니다.
    public void DespawnRemotePlayer(int actorId)
    {
        if (!remotePlayers.TryGetValue(actorId, out RemotePlayerController remotePlayer))
        {
            return;
        }

        remotePlayers.Remove(actorId);
        Destroy(remotePlayer.gameObject);
    }

    // PacketDispatcher에서 전달된 Spawn 패킷을 받아 처리합니다.
    private void HandleSpawn(SpawnPacket packet)
    {
        if (packet.EntityType == "Player")
        {
            SpawnRemotePlayer(packet.ActorId, packet.Position, packet.Yaw);
        }
    }

    // PacketDispatcher에서 전달된 원격 플레이어 이동 패킷을 받아 처리합니다.
    private void HandleMove(MovePacket packet)
    {
        if (localPlayer != null && packet.ActorId == localPlayer.ActorId)
        {
            return;
        }

        if (remotePlayers.TryGetValue(packet.ActorId, out RemotePlayerController remotePlayer))
        {
            remotePlayer.SetTargetPosition(packet.Position, packet.Yaw);
        }
    }

    // PacketDispatcher에서 전달된 원격 플레이어 정지 패킷을 받아 처리합니다.
    private void HandleStop(StopPacket packet)
    {
        if (remotePlayers.TryGetValue(packet.ActorId, out RemotePlayerController remotePlayer))
        {
            remotePlayer.SetTargetPosition(packet.Position, packet.Yaw);
        }
    }

    /// 게임 씬에서도 현재 계정을 유지한 채 캐릭터 선택 화면으로 돌아갈 수 있게 합니다.
    private void BuildReturnButton()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("GameMenuCanvas");
        canvas.sortingOrder = 100;
        Button returnButton = RuntimeUiFactory.CreateButton(canvas.transform, "ReturnCharacterSelectButton", "캐릭터 선택", new Vector2(0.84f, 0.90f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);
        returnButton.onClick.AddListener(ReturnToCharacterSelect);
    }

    /// 선택 캐릭터만 비우고 로그인 계정/캐릭터 목록은 유지한 채 선택 씬으로 이동합니다.
    private void ReturnToCharacterSelect()
    {
        CharacterSession.Instance.SetSelectedCharacter(null);
        SceneFlow.SetNextScene(SceneNames.CharacterSelect);
        SceneManager.LoadScene(SceneNames.Loading);
    }

    /// 씬에 플레이어가 없고 프리팹도 비어 있을 때 테스트 가능한 기본 로컬 플레이어를 생성합니다.
    private QuarterViewPlayerController CreateFallbackLocalPlayer()
    {
        GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "RuntimeLocalPlayer";
        playerObject.transform.SetParent(playerRoot, false);
        playerObject.transform.position = Vector3.zero;

        CapsuleCollider collider = playerObject.GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        playerObject.AddComponent<CharacterController>();
        playerObject.AddComponent<SkillController>();
        return playerObject.AddComponent<QuarterViewPlayerController>();
    }

    /// NetworkManager가 씬보다 오래 살아있으므로 같은 Dispatcher 참조로 이벤트를 정리합니다.
    private void SubscribeDispatcher()
    {
        if (subscribedDispatcher != null)
        {
            return;
        }

        subscribedDispatcher = NetworkManager.Instance.Dispatcher;
        subscribedDispatcher.EnterGameResponseReceived += HandleEnterGameResponse;
        subscribedDispatcher.SpawnReceived += HandleSpawn;
        subscribedDispatcher.MoveReceived += HandleMove;
        subscribedDispatcher.StopReceived += HandleStop;
    }

    /// 씬 전환 후 파괴된 GameManager가 늦은 서버 응답을 받지 않도록 구독을 해제합니다.
    private void UnsubscribeDispatcher()
    {
        if (subscribedDispatcher == null)
        {
            return;
        }

        subscribedDispatcher.EnterGameResponseReceived -= HandleEnterGameResponse;
        subscribedDispatcher.SpawnReceived -= HandleSpawn;
        subscribedDispatcher.MoveReceived -= HandleMove;
        subscribedDispatcher.StopReceived -= HandleStop;
        subscribedDispatcher = null;
    }
}

