#pragma once

#include "service/AuthService.h"

#include <winsock2.h>

#include <memory>
#include <mutex>
#include <vector>

class ClientSession;

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
    std::mutex sessionsMutex_;
    std::vector<std::weak_ptr<ClientSession>> sessions_;

    bool InitializeWinsock();
    bool CreateListenSocket();
    void AcceptLoop();
    void RegisterSession(const std::shared_ptr<ClientSession>& session);
    void HandleSessionEntered(const std::shared_ptr<ClientSession>& session);
    void BroadcastFromSession(const std::shared_ptr<ClientSession>& sender, const std::string& line);
    void RemoveSession(const std::shared_ptr<ClientSession>& session);
    std::vector<std::shared_ptr<ClientSession>> GetLiveSessions();
};
