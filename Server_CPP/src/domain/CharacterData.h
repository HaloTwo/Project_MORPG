#pragma once

#include <cstdint>
#include <string>
#include <vector>

enum class ClassType
{
    None = 0,
    Warrior = 1,
    Archer = 2,
    Rogue = 3
};

struct CharacterData
{
    // DB characters.character_id
    std::int32_t characterId = 0;

    // 이 캐릭터를 소유한 계정 ID입니다.
    std::int32_t accountId = 0;

    // 캐릭터 선택창 슬롯 번호입니다. 현재는 0~2, 총 3칸을 사용합니다.
    std::int32_t slotIndex = 0;

    // 캐릭터 이름입니다.
    std::string name;

    // 전사/궁수/도적 직업 구분입니다.
    ClassType classType = ClassType::None;

    // 성장/재화 정보입니다.
    std::int32_t level = 1;
    std::int32_t exp = 0;
    std::int32_t gold = 0;

    // 마지막으로 있었던 맵과 위치입니다.
    std::int32_t currentMapId = 1;
    float posX = 0.0f;
    float posY = 1.0f;
    float posZ = 0.0f;

    // 1, 2, 3번 슬롯에 들어갈 스킬 ID 목록입니다.
    std::vector<std::int32_t> quickSlotSkillIds;
};

inline const char* ToString(ClassType classType)
{
    // 서버 enum 값을 텍스트 프로토콜에서 사용할 문자열로 변환합니다.
    switch (classType)
    {
    case ClassType::Warrior:
        return "Warrior";
    case ClassType::Archer:
        return "Archer";
    case ClassType::Rogue:
        return "Rogue";
    default:
        return "None";
    }
}

inline ClassType ClassTypeFromString(const std::string& value)
{
    // Unity 또는 텍스트 프로토콜에서 받은 직업 문자열을 서버 enum으로 변환합니다.
    // 잘못된 값은 None으로 반환해서 서비스 계층에서 실패 처리할 수 있게 합니다.
    if (value == "Warrior")
    {
        return ClassType::Warrior;
    }

    if (value == "Archer")
    {
        return ClassType::Archer;
    }

    if (value == "Rogue")
    {
        return ClassType::Rogue;
    }

    return ClassType::None;
}
