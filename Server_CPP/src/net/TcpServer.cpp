#include "net/TcpServer.h"

#include "net/ClientSession.h"

#include <ws2tcpip.h>

#include <iostream>
#include <thread>

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
        std::thread([clientSocket, authService = authService_]()
        {
            ClientSession session(clientSocket, authService);
            session.Run();
        }).detach();
    }
}
