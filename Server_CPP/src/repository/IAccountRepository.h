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

    /// <summary>
    /// 회원가입 요청을 처리해 새 계정을 저장합니다.
    /// 같은 로그인 ID가 이미 있으면 std::nullopt를 반환합니다.
    /// </summary>
    virtual std::optional<AccountData> RegisterAccount(
        const std::string& loginId,
        const std::string& password) = 0;

    // 캐릭터 ID로 캐릭터 상세 정보를 가져옵니다.
    virtual std::optional<CharacterData> FindCharacterById(std::int32_t characterId) = 0;

    /// <summary>
    /// 계정에 새 캐릭터를 생성합니다.
    /// 서버는 계정 존재 여부, 3개 제한, 직업 유효성을 검사한 뒤 저장합니다.
    /// </summary>
    virtual std::optional<CharacterData> CreateCharacter(
        std::int32_t accountId,
        std::int32_t slotIndex,
        ClassType classType) = 0;
};
