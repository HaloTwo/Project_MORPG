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

    // ENTER_GAME 요청을 처리하고 응답 패킷을 반환합니다.
    std::string HandleEnterGame(std::int32_t characterId);

private:
    std::shared_ptr<IAccountRepository> accountRepository_;
};
