#include "net/TcpServer.h"
#include "repository/MariaDbAccountRepository.h"
#include "service/AuthService.h"

#include <cstdlib>
#include <iostream>
#include <memory>
#include <string>

namespace
{
    void ApplyEnvironmentValue(const char* name, std::string& target)
    {
        char* value = nullptr;
        std::size_t valueLength = 0;
        if (_dupenv_s(&value, &valueLength, name) == 0 && value != nullptr && value[0] != '\0')
        {
            target = value;
        }

        std::free(value);
    }
}

int main(int argc, char* argv[])
{
    unsigned short port = 7777;
    if (argc >= 2)
    {
        port = static_cast<unsigned short>(std::atoi(argv[1]));
    }

    MariaDbConfig dbConfig;
    ApplyEnvironmentValue("MORPG_DB_HOST", dbConfig.host);
    ApplyEnvironmentValue("MORPG_DB_USER", dbConfig.user);
    ApplyEnvironmentValue("MORPG_DB_PASSWORD", dbConfig.password);
    ApplyEnvironmentValue("MORPG_DB_NAME", dbConfig.database);

    if (argc >= 3)
    {
        dbConfig.password = argv[2];
    }

    std::shared_ptr<IAccountRepository> repository = std::make_shared<MariaDbAccountRepository>(dbConfig);
    std::shared_ptr<AuthService> authService = std::make_shared<AuthService>(repository);

    TcpServer server(port, authService);
    if (!server.Start())
    {
        std::cerr << "[Server] Failed to start." << std::endl;
        return 1;
    }

    return 0;
}
