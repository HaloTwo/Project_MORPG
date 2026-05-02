#pragma once

#include "service/AuthService.h"

#include <winsock2.h>

#include <memory>
#include <string>

class ClientSession
{
public:
    ClientSession(SOCKET socket, std::shared_ptr<AuthService> authService);

    // 클라이언트 세션 루프를 실행합니다.
    void Run();

private:
    SOCKET socket_ = INVALID_SOCKET;
    std::shared_ptr<AuthService> authService_;
    std::string receiveBuffer_;

    bool ReceiveLine(std::string& outLine);
    bool SendLine(const std::string& line);
    bool HandleCommand(const std::string& line);
};
