#pragma once

#include "domain/AccountData.h"

#include <cstdint>
#include <optional>
#include <string>

class IAccountRepository
{
public:
    virtual ~IAccountRepository() = default;

    // 로그인 ID와 토큰을 검증하고 계정 정보를 가져옵니다.
    virtual std::optional<AccountData> FindAccountByLogin(
        const std::string& loginId,
        const std::string& authToken) = 0;

    // 캐릭터 ID로 캐릭터 상세 정보를 가져옵니다.
    virtual std::optional<CharacterData> FindCharacterById(std::int32_t characterId) = 0;
};
