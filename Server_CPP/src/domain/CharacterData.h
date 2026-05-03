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
    std::int32_t characterId = 0;
    std::int32_t accountId = 0;
    std::string name;
    ClassType classType = ClassType::None;
    std::int32_t level = 1;
    std::int32_t exp = 0;
    std::int32_t gold = 0;
    std::int32_t currentMapId = 1;
    float posX = 0.0f;
    float posY = 1.0f;
    float posZ = 0.0f;
    std::vector<std::int32_t> quickSlotSkillIds;
};

inline const char* ToString(ClassType classType)
{
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

/// <summary>
/// Unity 또는 텍스트 프로토콜에서 받은 직업 문자열을 서버 enum으로 변환합니다.
/// 잘못된 값은 None으로 반환해서 서비스 계층에서 실패 처리할 수 있게 합니다.
/// </summary>
inline ClassType ClassTypeFromString(const std::string& value)
{
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
