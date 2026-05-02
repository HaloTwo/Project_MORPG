#pragma once

#include "service/AuthService.h"

#include <winsock2.h>

#include <memory>

class TcpServer
{
public:
    TcpServer(unsigned short port, std::shared_ptr<AuthService> authService);
    ~TcpServer();

    // 서버 소켓을 열고 accept 루프를 시작합니다.
    bool Start();

private:
    unsigned short port_ = 0;
    SOCKET listenSocket_ = INVALID_SOCKET;
    std::shared_ptr<AuthService> authService_;

    bool InitializeWinsock();
    bool CreateListenSocket();
    void AcceptLoop();
};
