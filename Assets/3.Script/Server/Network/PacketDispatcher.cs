using System;
using UnityEngine;
using System.Collections.Generic;

public sealed class PacketDispatcher
{
    public event Action<LoginResponsePacket> LoginResponseReceived;
    public event Action<RegisterResponsePacket> RegisterResponseReceived;
    public event Action<CharacterListPacket> CharacterListReceived;
    public event Action<EnterGameResponsePacket> EnterGameResponseReceived;
    public event Action<CreateCharacterResponsePacket> CreateCharacterResponseReceived;
    public event Action<DeleteCharacterResponsePacket> DeleteCharacterResponseReceived;
    public event Action<MovePacket> MoveReceived;
    public event Action<StopPacket> StopReceived;
    public event Action<SpawnPacket> SpawnReceived;
    public event Action<DespawnPacket> DespawnReceived;
    public event Action<DamagePacket> DamageReceived;
    public event Action<SkillPacket> SkillReceived;
    public event Action<ChatPacket> ChatReceived;

    private readonly Dictionary<int, SpawnPacket> knownRemoteSpawns = new Dictionary<int, SpawnPacket>();

    /// 씬 로딩 중 먼저 도착한 SPAWN을 GameScene 시작 시 복원할 수 있게 제공합니다.
    public List<SpawnPacket> GetKnownRemoteSpawns()
    {
        return new List<SpawnPacket>(knownRemoteSpawns.Values);
    }

    // 패킷 ID를 기준으로 어떤 처리 함수로 보낼지 분기합니다.
    public void Dispatch(PacketBase packet)
    {
        if (packet == null)
        {
            return;
        }

        switch (packet.Id)
        {
            case PacketId.LoginResponse:
                HandleLoginResponse((LoginResponsePacket)packet);
                break;
            case PacketId.RegisterResponse:
                HandleRegisterResponse((RegisterResponsePacket)packet);
                break;
            case PacketId.CharacterList:
                HandleCharacterList((CharacterListPacket)packet);
                break;
            case PacketId.EnterGameResponse:
                HandleEnterGameResponse((EnterGameResponsePacket)packet);
                break;
            case PacketId.CreateCharacterResponse:
                HandleCreateCharacterResponse((CreateCharacterResponsePacket)packet);
                break;
            case PacketId.DeleteCharacterResponse:
                HandleDeleteCharacterResponse((DeleteCharacterResponsePacket)packet);
                break;
            case PacketId.Move:
                HandleMove((MovePacket)packet);
                break;
            case PacketId.Stop:
                HandleStop((StopPacket)packet);
                break;
            case PacketId.Spawn:
                HandleSpawn((SpawnPacket)packet);
                break;
            case PacketId.Despawn:
                HandleDespawn((DespawnPacket)packet);
                break;
            case PacketId.Damage:
                HandleDamage((DamagePacket)packet);
                break;
            case PacketId.Skill:
                HandleSkill((SkillPacket)packet);
                break;
            case PacketId.Chat:
                HandleChat((ChatPacket)packet);
                break;
            default:
                Debug.LogWarning($"Unhandled packet id: {packet.Id}");
                break;
        }
    }

    // 로그인 결과를 클라이언트 로그인 흐름으로 전달합니다.
    private void HandleLoginResponse(LoginResponsePacket packet)
    {
        Debug.Log($"[PacketDispatcher] Login success={packet.Success} account={packet.AccountId}");
        LoginResponseReceived?.Invoke(packet);
    }

    /// <summary>
    /// 회원가입 결과를 로그인 화면으로 전달합니다.
    /// 성공하면 바로 로그인처럼 계정 ID와 빈 캐릭터 목록을 받을 준비를 합니다.
    /// </summary>
    private void HandleRegisterResponse(RegisterResponsePacket packet)
    {
        Debug.Log($"[PacketDispatcher] Register success={packet.Success} account={packet.AccountId}");
        RegisterResponseReceived?.Invoke(packet);
    }

    // 서버에서 내려준 캐릭터 목록을 클라이언트 선택 화면으로 전달합니다.
    private void HandleCharacterList(CharacterListPacket packet)
    {
        Debug.Log($"[PacketDispatcher] CharacterList count={packet.Characters.Count}");
        CharacterListReceived?.Invoke(packet);
    }

    // 캐릭터 입장 결과를 게임 매니저로 전달합니다.
    private void HandleEnterGameResponse(EnterGameResponsePacket packet)
    {
        Debug.Log($"[PacketDispatcher] EnterGame success={packet.Success}");
        EnterGameResponseReceived?.Invoke(packet);
    }

    /// <summary>
    /// 캐릭터 생성 결과를 캐릭터 선택 화면으로 전달합니다.
    /// 성공 시 선택 화면은 세션 캐릭터 목록을 갱신하고 3슬롯 UI를 다시 그립니다.
    /// </summary>
    private void HandleCreateCharacterResponse(CreateCharacterResponsePacket packet)
    {
        Debug.Log($"[PacketDispatcher] CreateCharacter success={packet.Success}");
        CreateCharacterResponseReceived?.Invoke(packet);
    }

    /// 캐릭터 삭제 결과를 캐릭터 선택 화면으로 전달합니다.
    private void HandleDeleteCharacterResponse(DeleteCharacterResponsePacket packet)
    {
        Debug.Log($"[PacketDispatcher] DeleteCharacter success={packet.Success} character={packet.CharacterId}");
        DeleteCharacterResponseReceived?.Invoke(packet);
    }

    // 서버 또는 로컬 시뮬레이션에서 받은 이동 동기화 패킷을 처리합니다.
    private void HandleMove(MovePacket packet)
    {
        knownRemoteSpawns[packet.ActorId] = new SpawnPacket(packet.ActorId, "Player", packet.Position, packet.Yaw);
        Debug.Log($"[PacketDispatcher] Move actor={packet.ActorId} pos={packet.Position} yaw={packet.Yaw}");
        MoveReceived?.Invoke(packet);
    }

    // 서버 또는 로컬 시뮬레이션에서 받은 정지 패킷을 처리합니다.
    private void HandleStop(StopPacket packet)
    {
        knownRemoteSpawns[packet.ActorId] = new SpawnPacket(packet.ActorId, "Player", packet.Position, packet.Yaw);
        Debug.Log($"[PacketDispatcher] Stop actor={packet.ActorId} pos={packet.Position} yaw={packet.Yaw}");
        StopReceived?.Invoke(packet);
    }

    // 원격 플레이어 같은 다른 엔티티 생성 패킷을 처리합니다.
    private void HandleSpawn(SpawnPacket packet)
    {
        knownRemoteSpawns[packet.ActorId] = packet;
        Debug.Log($"[PacketDispatcher] Spawn actor={packet.ActorId} type={packet.EntityType}");
        SpawnReceived?.Invoke(packet);
    }

    private void HandleDespawn(DespawnPacket packet)
    {
        knownRemoteSpawns.Remove(packet.ActorId);
        Debug.Log($"[PacketDispatcher] Despawn actor={packet.ActorId}");
        DespawnReceived?.Invoke(packet);
    }

    // 서버가 최종 판정한 데미지와 HP 변경 내용을 처리합니다.
    private void HandleDamage(DamagePacket packet)
    {
        Debug.Log($"[PacketDispatcher] Damage target={packet.TargetId} hp={packet.CurrentHp}/{packet.MaxHp}");
        DamageReceived?.Invoke(packet);
    }

    // 스킬 사용 패킷을 처리합니다.
    private void HandleSkill(SkillPacket packet)
    {
        Debug.Log($"[PacketDispatcher] Skill caster={packet.CasterId} slot={packet.SkillSlot} skill={packet.SkillId}");
        SkillReceived?.Invoke(packet);
    }

    /// 서버에서 브로드캐스트한 채팅 패킷을 게임 HUD로 전달합니다.
    private void HandleChat(ChatPacket packet)
    {
        Debug.Log($"[PacketDispatcher] Chat actor={packet.ActorId} sender={packet.Sender}");
        ChatReceived?.Invoke(packet);
    }
}
