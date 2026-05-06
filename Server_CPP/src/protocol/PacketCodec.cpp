#include "protocol/PacketCodec.h"

#include <sstream>

ClientCommand PacketCodec::DecodeClientCommand(const std::string& line)
{
    // 공백 기준으로 첫 단어는 명령 이름, 이후 단어들은 인자로 분리합니다.
    // 예: "LOGIN test_user 1234" -> name=LOGIN, args=[test_user, 1234]
    std::istringstream stream(line);
    ClientCommand command;
    stream >> command.name;

    std::string arg;
    while (stream >> arg)
    {
        command.args.push_back(arg);
    }

    return command;
}

std::string PacketCodec::EncodeLoginOk(const AccountData& account)
{
    // 로그인 성공 후 Unity가 accountId를 세션에 저장할 수 있도록 내려줍니다.
    std::ostringstream stream;
    stream << "LOGIN_OK accountId=" << account.accountId << " message=LoginSuccess";
    return stream.str();
}

std::string PacketCodec::EncodeLoginFail(const std::string& message)
{
    // 로그인 실패 이유는 간단한 message 필드로 내려줍니다.
    return "LOGIN_FAIL message=" + message;
}

std::string PacketCodec::EncodeRegisterOk(const AccountData& account)
{
    // 회원가입 성공 시 새 accountId를 내려줍니다.
    std::ostringstream stream;
    stream << "REGISTER_OK accountId=" << account.accountId << " message=RegisterSuccess";
    return stream.str();
}

std::string PacketCodec::EncodeRegisterFail(const std::string& message)
{
    return "REGISTER_FAIL message=" + message;
}

std::vector<std::string> PacketCodec::EncodeCharacterList(const std::vector<CharacterData>& characters)
{
    // 캐릭터 목록은 여러 줄로 전송합니다.
    // 시작 줄: CHARACTER_LIST count=N
    // 캐릭터 줄들: CHARACTER ...
    // 종료 줄: CHARACTER_LIST_END
    std::vector<std::string> lines;
    lines.emplace_back("CHARACTER_LIST count=" + std::to_string(characters.size()));

    for (const CharacterData& character : characters)
    {
        lines.emplace_back(EncodeCharacter(character));
    }

    lines.emplace_back("CHARACTER_LIST_END");
    return lines;
}

std::string PacketCodec::EncodeEnterGameOk(const CharacterData& character)
{
    // 게임 입장 성공 시 선택 캐릭터 정보를 한 줄에 포함합니다.
    return "ENTER_GAME_OK " + EncodeCharacter(character);
}

std::string PacketCodec::EncodeEnterGameFail(const std::string& message)
{
    return "ENTER_GAME_FAIL message=" + message;
}

std::string PacketCodec::EncodeCreateCharacterOk(const CharacterData& character)
{
    return "CREATE_CHARACTER_OK " + EncodeCharacter(character);
}

std::string PacketCodec::EncodeCreateCharacterFail(const std::string& message)
{
    return "CREATE_CHARACTER_FAIL message=" + message;
}

std::string PacketCodec::EncodeDeleteCharacterOk(std::int32_t characterId)
{
    return "DELETE_CHARACTER_OK characterId=" + std::to_string(characterId) + " message=DeleteCharacterSuccess";
}

std::string PacketCodec::EncodeDeleteCharacterFail(std::int32_t characterId, const std::string& message)
{
    return "DELETE_CHARACTER_FAIL characterId=" + std::to_string(characterId) + " message=" + message;
}

std::string PacketCodec::EncodeCharacter(const CharacterData& character)
{
    // CharacterData를 현재 텍스트 프로토콜 포맷으로 직렬화합니다.
    // 나중에 바이너리 패킷/Protobuf로 바꿀 때 이 계층이 교체 대상입니다.
    std::ostringstream stream;
    stream << "CHARACTER"
        << " id=" << character.characterId
        << " slot=" << character.slotIndex
        << " name=" << character.name
        << " class=" << ToString(character.classType)
        << " level=" << character.level
        << " gold=" << character.gold
        << " pos=" << character.posX << "," << character.posY << "," << character.posZ
        << " skills=" << JoinSkillIds(character.quickSlotSkillIds)
        << " inventory=" << JoinInventoryItems(character.inventoryItems)
        << " equipment=" << JoinEquipmentItems(character.equipmentItems);
    return stream.str();
}

std::string PacketCodec::JoinSkillIds(const std::vector<std::int32_t>& skillIds)
{
    // 스킬 슬롯 배열을 콤마 문자열로 변환합니다.
    // 예: [1001,1002,1003] -> "1001,1002,1003"
    std::ostringstream stream;
    for (std::size_t i = 0; i < skillIds.size(); ++i)
    {
        if (i > 0)
        {
            stream << ",";
        }

        stream << skillIds[i];
    }

    return stream.str();
}

std::string PacketCodec::JoinInventoryItems(const std::vector<InventoryItemData>& items)
{
    // itemUid:itemId:itemType:count:slotIndex:enhancement 형식입니다.
    // 공백을 쓰지 않아 현재 텍스트 프로토콜의 key=value 파서와 충돌하지 않게 합니다.
    std::ostringstream stream;
    for (std::size_t i = 0; i < items.size(); ++i)
    {
        if (i > 0)
        {
            stream << ";";
        }

        const InventoryItemData& item = items[i];
        stream << item.itemUid << ":"
               << item.itemId << ":"
               << item.itemType << ":"
               << item.count << ":"
               << item.slotIndex << ":"
               << item.enhancementLevel;
    }

    return stream.str();
}

std::string PacketCodec::JoinEquipmentItems(const std::vector<EquipmentEntryData>& items)
{
    // equipSlot:itemUid 형식입니다. 실제 아이템 상세 정보는 inventory 토큰의 itemUid와 매칭합니다.
    std::ostringstream stream;
    for (std::size_t i = 0; i < items.size(); ++i)
    {
        if (i > 0)
        {
            stream << ";";
        }

        const EquipmentEntryData& item = items[i];
        stream << item.equipSlot << ":" << item.itemUid;
    }

    return stream.str();
}
