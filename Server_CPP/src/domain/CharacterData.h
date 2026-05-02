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
