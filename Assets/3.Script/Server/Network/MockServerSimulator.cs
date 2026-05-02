using System.Collections.Generic;
using UnityEngine;

public sealed class MockServerSimulator
{
    private readonly Dictionary<int, CharacterData> charactersById = new Dictionary<int, CharacterData>();
    private readonly int accountId = 1;

    public MockServerSimulator()
    {
        CreateMockCharacters();
    }

    // 실제 서버가 붙기 전까지 클라이언트 요청에 대한 가짜 서버 응답을 만듭니다.
    public void HandleClientPacket(PacketBase packet, PacketQueue receiveQueue)
    {
        if (packet == null || receiveQueue == null)
        {
            return;
        }

        switch (packet.Id)
        {
            case PacketId.LoginRequest:
                HandleLogin((LoginRequestPacket)packet, receiveQueue);
                break;
            case PacketId.EnterGameRequest:
                HandleEnterGame((EnterGameRequestPacket)packet, receiveQueue);
                break;
            case PacketId.Move:
            case PacketId.Stop:
            case PacketId.Skill:
                receiveQueue.Enqueue(packet);
                break;
        }
    }

    // 로그인 요청을 받으면 성공 응답과 캐릭터 3개 목록을 내려줍니다.
    private void HandleLogin(LoginRequestPacket packet, PacketQueue receiveQueue)
    {
        Debug.Log($"[MockServer] Login request id={packet.LoginId}");
        receiveQueue.Enqueue(new LoginResponsePacket(true, accountId, "Mock login success"));
        receiveQueue.Enqueue(new CharacterListPacket(accountId, new List<CharacterData>(charactersById.Values)));
    }

    // 캐릭터 입장 요청을 받으면 해당 캐릭터 데이터를 내려줍니다.
    private void HandleEnterGame(EnterGameRequestPacket packet, PacketQueue receiveQueue)
    {
        if (!charactersById.TryGetValue(packet.CharacterId, out CharacterData character))
        {
            receiveQueue.Enqueue(new EnterGameResponsePacket(false, null, "Character not found"));
            return;
        }

        receiveQueue.Enqueue(new EnterGameResponsePacket(true, character, "Enter game success"));
    }

    // 전사, 궁수, 도적 Mock 캐릭터와 기본 장비/스킬을 준비합니다.
    private void CreateMockCharacters()
    {
        AddCharacter(101, "Leon", ClassType.Warrior, new Vector3(-2.0f, 1.0f, 0.0f));
        AddCharacter(102, "Rena", ClassType.Archer, new Vector3(0.0f, 1.0f, 0.0f));
        AddCharacter(103, "Kain", ClassType.Rogue, new Vector3(2.0f, 1.0f, 0.0f));
    }

    // 캐릭터 하나의 저장 데이터를 구성합니다.
    private void AddCharacter(int characterId, string name, ClassType classType, Vector3 position)
    {
        CharacterData character = new CharacterData
        {
            CharacterId = characterId,
            AccountId = accountId,
            Name = name,
            ClassType = classType,
            Level = 1,
            Exp = 0,
            Gold = 100,
            CurrentMapId = 1,
            Position = position,
            QuickSlotSkillIds = SkillDatabase.GetDefaultSkillIds(classType)
        };

        foreach (int skillId in character.QuickSlotSkillIds)
        {
            if (skillId != 0)
            {
                character.LearnedSkillIds.Add(skillId);
            }
        }

        character.Inventory.Add(new InventoryItemData { ItemUid = characterId * 1000L + 1, ItemId = GetStarterWeaponItemId(classType), ItemType = ItemType.Weapon, Count = 1, SlotIndex = 0, EnhancementLevel = 0 });
        character.Inventory.Add(new InventoryItemData { ItemUid = characterId * 1000L + 2, ItemId = 5001, ItemType = ItemType.Consumable, Count = 5, SlotIndex = 1, EnhancementLevel = 0 });
        character.Equipment.Equip(EquipSlot.Weapon, character.Inventory[0].ItemUid);

        charactersById.Add(character.CharacterId, character);
    }

    // 직업별 임시 시작 무기 ID를 반환합니다.
    private int GetStarterWeaponItemId(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Warrior:
                return 1101;
            case ClassType.Archer:
                return 1201;
            case ClassType.Rogue:
                return 1301;
            default:
                return 0;
        }
    }
}
