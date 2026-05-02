#include "repository/MockAccountRepository.h"

MockAccountRepository::MockAccountRepository()
{
    CreateMockData();
}

std::optional<AccountData> MockAccountRepository::FindAccountByLogin(
    const std::string& loginId,
    const std::string& authToken)
{
    if (loginId != "test_user" || authToken.empty())
    {
        return std::nullopt;
    }

    return account_;
}

std::optional<CharacterData> MockAccountRepository::FindCharacterById(std::int32_t characterId)
{
    const auto iter = charactersById_.find(characterId);
    if (iter == charactersById_.end())
    {
        return std::nullopt;
    }

    return iter->second;
}

void MockAccountRepository::CreateMockData()
{
    account_.accountId = 1;
    account_.loginId = "test_user";
    account_.displayName = "TestAccount";

    AddCharacter(101, "Leon", ClassType::Warrior, -2.0f, 1.0f, 0.0f);
    AddCharacter(102, "Rena", ClassType::Archer, 0.0f, 1.0f, 0.0f);
    AddCharacter(103, "Kain", ClassType::Rogue, 2.0f, 1.0f, 0.0f);
}

void MockAccountRepository::AddCharacter(std::int32_t characterId, const std::string& name, ClassType classType, float x, float y, float z)
{
    CharacterData character;
    character.characterId = characterId;
    character.accountId = account_.accountId;
    character.name = name;
    character.classType = classType;
    character.level = 1;
    character.exp = 0;
    character.gold = 100;
    character.currentMapId = 1;
    character.posX = x;
    character.posY = y;
    character.posZ = z;
    character.quickSlotSkillIds = GetDefaultSkillIds(classType);

    account_.characters.push_back(character);
    charactersById_.emplace(character.characterId, character);
}

std::vector<std::int32_t> MockAccountRepository::GetDefaultSkillIds(ClassType classType) const
{
    switch (classType)
    {
    case ClassType::Warrior:
        return {1001, 1002, 1003};
    case ClassType::Archer:
        return {2001, 2002, 2003};
    case ClassType::Rogue:
        return {3001, 3002, 3003};
    default:
        return {};
    }
}
