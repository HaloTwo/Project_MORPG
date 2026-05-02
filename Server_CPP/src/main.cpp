#include "net/TcpServer.h"
#include "repository/MockAccountRepository.h"
#include "service/AuthService.h"

#include <cstdlib>
#include <iostream>
#include <memory>

int main(int argc, char* argv[])
{
    unsigned short port = 7777;
    if (argc >= 2)
    {
        port = static_cast<unsigned short>(std::atoi(argv[1]));
    }

    std::shared_ptr<IAccountRepository> repository = std::make_shared<MockAccountRepository>();
    std::shared_ptr<AuthService> authService = std::make_shared<AuthService>(repository);

    TcpServer server(port, authService);
    if (!server.Start())
    {
        std::cerr << "[Server] Failed to start." << std::endl;
        return 1;
    }

    return 0;
}
