#include "net/TcpServer.h"

#include "net/ClientSession.h"

#include <ws2tcpip.h>

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
    // 서버 종료 시 listen 소켓과 Winsock 자원을 정리합니다.
    if (listenSocket_ != INVALID_SOCKET)
    {
        closesocket(listenSocket_);
    }

    WSACleanup();
}

bool TcpServer::Start()
{
    // Windows 소켓 API를 먼저 초기화합니다.
    if (!InitializeWinsock())
    {
        return false;
    }

    // 클라이언트가 접속할 listen 소켓을 생성하고 포트에 바인딩합니다.
    if (!CreateListenSocket())
    {
        return false;
    }

    std::cout << "[Server] Listening on port " << port_ << std::endl;

    // 여기서부터 서버는 계속 accept 대기 상태로 들어갑니다.
    // 현재는 학습용 구조라 블로킹 accept 루프를 사용합니다.
    AcceptLoop();
    return true;
}

bool TcpServer::InitializeWinsock()
{
    // Winsock은 Windows에서 socket/recv/send를 쓰기 전에 반드시 초기화해야 합니다.
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
    // IPv4 TCP 소켓을 생성합니다.
    listenSocket_ = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSocket_ == INVALID_SOCKET)
    {
        std::cerr << "[Server] socket failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    // 0.0.0.0:port에 바인딩합니다.
    // INADDR_ANY는 현재 PC의 모든 네트워크 인터페이스에서 접속을 받겠다는 뜻입니다.
    sockaddr_in address = {};
    address.sin_family = AF_INET;
    address.sin_addr.s_addr = htonl(INADDR_ANY);
    address.sin_port = htons(port_);

    if (bind(listenSocket_, reinterpret_cast<sockaddr*>(&address), sizeof(address)) == SOCKET_ERROR)
    {
        std::cerr << "[Server] bind failed: " << WSAGetLastError() << std::endl;
        return false;
    }

    // listen 상태로 전환하면 클라이언트 connect 요청을 받을 수 있습니다.
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
        // 클라이언트가 접속할 때까지 여기서 대기합니다.
        SOCKET clientSocket = accept(listenSocket_, nullptr, nullptr);
        if (clientSocket == INVALID_SOCKET)
        {
            std::cerr << "[Server] accept failed: " << WSAGetLastError() << std::endl;
            continue;
        }

        std::cout << "[Server] Client connected" << std::endl;

        // 지금은 접속마다 thread 하나를 만들어 처리합니다.
        // 최종 목표인 IOCP 서버에서는 이 부분이 비동기 I/O 기반 세션 처리로 바뀝니다.
        std::thread([clientSocket, authService = authService_]()
        {
            ClientSession session(clientSocket, authService);
            session.Run();
        }).detach();
    }
}
