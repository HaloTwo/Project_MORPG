#include "net/TcpServer.h"

#include "net/ClientSession.h"

#include <ws2tcpip.h>

#include <algorithm>
#include <iostream>
#include <thread>
#include <utility>

TcpServer::TcpServer(unsigned short port, std::shared_ptr<AuthService> authService)
    : port_(port),
      authService_(std::move(authService))
{
}

TcpServer::~TcpServer()
{
    if (listenSocket_ != INVALID_SOCKET)
    {
        closesocket(listenSocket_);
    }

    WSACleanup();
}

bool TcpServer::Start()
{
    if (!InitializeWinsock())
    {
        return false;
    }

    if (!CreateListenSocket())
    {
        return false;
    }

    std::cout << "[Server] Listening on port " << port_ << std::endl;
    AcceptLoop();
    return true;
}

bool TcpServer::InitializeWinsock()
{
    WSADATA data;
    const int result = WSAStartup(MAKEWORD(2, 2), &data);
    if (result != 0)
    {
        std::cerr << "[Server] WSAStartup failed: " << result << std::endl;
        return false;
    }

    return true;
}

bool TcpServer::CreateListenSocket()
{
    listenSocket_ = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSocket_ == INVALID_SOCKET)
    {
        std::cerr << "[Server] socket failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    sockaddr_in address = {};
    address.sin_family = AF_INET;
    address.sin_addr.s_addr = htonl(INADDR_ANY);
    address.sin_port = htons(port_);

    if (bind(listenSocket_, reinterpret_cast<sockaddr*>(&address), sizeof(address)) == SOCKET_ERROR)
    {
        std::cerr << "[Server] bind failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    if (listen(listenSocket_, SOMAXCONN) == SOCKET_ERROR)
    {
        std::cerr << "[Server] listen failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    return true;
}

void TcpServer::AcceptLoop()
{
    while (true)
    {
        SOCKET clientSocket = accept(listenSocket_, nullptr, nullptr);
        if (clientSocket == INVALID_SOCKET)
        {
            std::cerr << "[Server] accept failed: " << WSAGetLastError() << std::endl;
            continue;
        }

        std::cout << "[Server] Client connected" << std::endl;

        auto session = std::make_shared<ClientSession>(clientSocket, authService_);
        session->SetCallbacks(
            [this](std::shared_ptr<ClientSession> sender, const std::string& line)
            {
                BroadcastFromSession(sender, line);
            },
            [this](std::shared_ptr<ClientSession> enteredSession)
            {
                HandleSessionEntered(enteredSession);
            },
            [this](std::shared_ptr<ClientSession> closedSession)
            {
                RemoveSession(closedSession);
            });

        RegisterSession(session);

        std::thread([session]()
        {
            session->Run();
        }).detach();
    }
}

void TcpServer::RegisterSession(const std::shared_ptr<ClientSession>& session)
{
    std::lock_guard<std::mutex> lock(sessionsMutex_);
    sessions_.push_back(session);
}

void TcpServer::HandleSessionEntered(const std::shared_ptr<ClientSession>& session)
{
    std::vector<std::shared_ptr<ClientSession>> sessions = GetLiveSessions();

    for (const std::shared_ptr<ClientSession>& other : sessions)
    {
        if (other == session || !other->IsInGame())
        {
            continue;
        }

        std::cout << "[Server] Send existing spawn actor=" << other->GetActorId()
            << " to actor=" << session->GetActorId() << std::endl;
        session->SendFromServer(other->MakeSpawnLine());
    }

    std::cout << "[Server] Broadcast spawn actor=" << session->GetActorId() << std::endl;
    BroadcastFromSession(session, session->MakeSpawnLine());
}

void TcpServer::BroadcastFromSession(const std::shared_ptr<ClientSession>& sender, const std::string& line)
{
    std::vector<std::shared_ptr<ClientSession>> sessions = GetLiveSessions();
    for (const std::shared_ptr<ClientSession>& session : sessions)
    {
        if (session == sender || !session->IsInGame())
        {
            continue;
        }

        std::cout << "[Server] Broadcast to actor=" << session->GetActorId()
            << " line=" << line << std::endl;
        session->SendFromServer(line);
    }
}

void TcpServer::RemoveSession(const std::shared_ptr<ClientSession>& session)
{
    const std::int32_t actorId = session != nullptr ? session->GetActorId() : 0;
    {
        std::lock_guard<std::mutex> lock(sessionsMutex_);
        sessions_.erase(
            std::remove_if(sessions_.begin(), sessions_.end(),
                [&session](const std::weak_ptr<ClientSession>& weakSession)
                {
                    std::shared_ptr<ClientSession> locked = weakSession.lock();
                    return locked == nullptr || locked == session;
                }),
            sessions_.end());
    }

    if (actorId > 0)
    {
        BroadcastFromSession(session, "DESPAWN actorId=" + std::to_string(actorId));
    }
}

std::vector<std::shared_ptr<ClientSession>> TcpServer::GetLiveSessions()
{
    std::vector<std::shared_ptr<ClientSession>> liveSessions;
    std::lock_guard<std::mutex> lock(sessionsMutex_);

    sessions_.erase(
        std::remove_if(sessions_.begin(), sessions_.end(),
            [&liveSessions](const std::weak_ptr<ClientSession>& weakSession)
            {
                std::shared_ptr<ClientSession> locked = weakSession.lock();
                if (locked == nullptr)
                {
                    return true;
                }

                liveSessions.push_back(locked);
                return false;
            }),
        sessions_.end());

    return liveSessions;
}
