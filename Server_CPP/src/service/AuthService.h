#pragma once

#include "repository/IAccountRepository.h"

#include <memory>
#include <string>
#include <vector>

class AuthService
{
public:
    explicit AuthService(std::shared_ptr<IAccountRepository> accountRepository);

    // LOGIN 요청을 처리하고 응답 패킷들을 반환합니다.
    std::vector<std::string> HandleLogin(const std::string& loginId, const std::string& authToken);

    /// <summary>
    /// REGISTER 요청을 처리하고 회원가입 결과와 빈 캐릭터 목록 응답을 반환합니다.
    /// </summary>
    std::vector<std::string> HandleRegister(const std::string& loginId, const std::string& password);

    // ENTER_GAME 요청을 처리하고 응답 패킷을 반환합니다.
    std::string HandleEnterGame(std::int32_t characterId);

    /// <summary>
    /// CREATE_CHARACTER 요청을 처리하고 생성 결과 응답을 반환합니다.
    /// </summary>
    std::string HandleCreateCharacter(std::int32_t accountId, std::int32_t slotIndex, ClassType classType);

    /// DELETE_CHARACTER 요청을 처리하고 삭제 결과 응답을 반환합니다.
    std::string HandleDeleteCharacter(std::int32_t accountId, std::int32_t characterId);

private:
    std::shared_ptr<IAccountRepository> accountRepository_;
};
