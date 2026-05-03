#pragma once

#include "domain/CharacterData.h"

#include <cstdint>
#include <string>
#include <vector>

struct AccountData
{
    std::int32_t accountId = 0;
    std::string loginId;
    std::string password;
    std::string displayName;
    std::vector<CharacterData> characters;
};
