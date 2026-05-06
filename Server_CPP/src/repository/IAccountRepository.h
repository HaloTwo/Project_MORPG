#pragma once

#include "domain/AccountData.h"

#include <cstdint>
#include <optional>
#include <string>

class IAccountRepository
{
public:
    virtual ~IAccountRepository() = default;

    // 로그인 ID와 인증 값을 검증하고 계정 정보와 캐릭터 목록을 가져옵니다.
    virtual std::optional<AccountData> FindAccountByLogin(
        const std::string& loginId,
        const std::string& authToken) = 0;

    // 회원가입 요청을 처리하고 새 계정 정보를 반환합니다.
    // 같은 loginId가 이미 있거나 입력값이 잘못되면 std::nullopt를 반환합니다.
    virtual std::optional<AccountData> RegisterAccount(
        const std::string& loginId,
        const std::string& password) = 0;

    // 기존 평문 비밀번호 계정을 해시 저장 방식으로 전환할 때 사용합니다.
    virtual bool UpdatePasswordHash(
        const std::string& loginId,
        const std::string& passwordHash) = 0;

    // 캐릭터 ID로 캐릭터 상세 정보와 스킬 슬롯 정보를 가져옵니다.
    virtual std::optional<CharacterData> FindCharacterById(std::int32_t characterId) = 0;

    // 특정 계정에 새 캐릭터를 생성합니다.
    // 서버가 슬롯 범위, 최대 캐릭터 수, 직업 유효성을 검증한 뒤 저장합니다.
    virtual std::optional<CharacterData> CreateCharacter(
        std::int32_t accountId,
        std::int32_t slotIndex,
        ClassType classType,
        const std::string& characterName) = 0;

    // 계정 소유 캐릭터인지 확인 가능한 조건으로 캐릭터를 삭제합니다.
    virtual bool DeleteCharacter(std::int32_t accountId, std::int32_t characterId) = 0;
};
