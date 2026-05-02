using System;
using UnityEngine;

public sealed class PacketDispatcher
{
    public event Action<LoginResponsePacket> LoginResponseReceived;
    public event Action<CharacterListPacket> CharacterListReceived;
    public event Action<EnterGameResponsePacket> EnterGameResponseReceived;
    public event Action<MovePacket> MoveReceived;
    public event Action<StopPacket> StopReceived;
    public event Action<SpawnPacket> SpawnReceived;
    public event Action<DamagePacket> DamageReceived;
    public event Action<SkillPacket> SkillReceived;

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
            case PacketId.CharacterList:
                HandleCharacterList((CharacterListPacket)packet);
                break;
            case PacketId.EnterGameResponse:
                HandleEnterGameResponse((EnterGameResponsePacket)packet);
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
            case PacketId.Damage:
                HandleDamage((DamagePacket)packet);
                break;
            case PacketId.Skill:
                HandleSkill((SkillPacket)packet);
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

    // 서버 또는 로컬 시뮬레이션에서 받은 이동 동기화 패킷을 처리합니다.
    private void HandleMove(MovePacket packet)
    {
        Debug.Log($"[PacketDispatcher] Move actor={packet.ActorId} pos={packet.Position} yaw={packet.Yaw}");
        MoveReceived?.Invoke(packet);
    }

    // 서버 또는 로컬 시뮬레이션에서 받은 정지 패킷을 처리합니다.
    private void HandleStop(StopPacket packet)
    {
        Debug.Log($"[PacketDispatcher] Stop actor={packet.ActorId} pos={packet.Position} yaw={packet.Yaw}");
        StopReceived?.Invoke(packet);
    }

    // 원격 플레이어 같은 다른 엔티티 생성 패킷을 처리합니다.
    private void HandleSpawn(SpawnPacket packet)
    {
        Debug.Log($"[PacketDispatcher] Spawn actor={packet.ActorId} type={packet.EntityType}");
        SpawnReceived?.Invoke(packet);
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
}
