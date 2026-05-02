#pragma once

#include "repository/IAccountRepository.h"

#include <unordered_map>

class MockAccountRepository final : public IAccountRepository
{
public:
    MockAccountRepository();

    std::optional<AccountData> FindAccountByLogin(
        const std::string& loginId,
        const std::string& authToken) override;

    std::optional<CharacterData> FindCharacterById(std::int32_t characterId) override;

private:
    AccountData account_;
    std::unordered_map<std::int32_t, CharacterData> charactersById_;

    void CreateMockData();
    void AddCharacter(std::int32_t characterId, const std::string& name, ClassType classType, float x, float y, float z);
    std::vector<std::int32_t> GetDefaultSkillIds(ClassType classType) const;
};
