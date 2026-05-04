#include "net/TcpServer.h"
#include "repository/MariaDbAccountRepository.h"
#include "service/AuthService.h"

#include <cstdlib>
#include <iostream>
#include <memory>
#include <string>

namespace
{
    // 환경변수에 값이 있으면 DB 설정값을 덮어씁니다.
    // 예: MORPG_DB_HOST, MORPG_DB_USER, MORPG_DB_PASSWORD, MORPG_DB_NAME
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

    // 비밀번호가 환경변수나 실행 인자로 들어오지 않았으면 콘솔에서 직접 입력받습니다.
    // DB 비밀번호를 코드에 하드코딩하지 않기 위한 최소한의 처리입니다.
    void PromptPasswordIfNeeded(MariaDbConfig& dbConfig)
    {
        if (!dbConfig.password.empty())
        {
            return;
        }

        std::cout << "[MariaDB] Enter password for user '" << dbConfig.user << "': ";
        std::getline(std::cin, dbConfig.password);
    }
}

int main(int argc, char* argv[])
{
    // 첫 번째 실행 인자는 서버 포트입니다. 생략하면 7777을 사용합니다.
    // 예: ProjectMORPGServer.exe 7777
    unsigned short port = 7777;
    if (argc >= 2)
    {
        port = static_cast<unsigned short>(std::atoi(argv[1]));
    }

    // MariaDB 접속 기본값을 만들고, 환경변수로 로컬 PC별 설정을 주입합니다.
    // 노트북/데스크톱마다 DB 비밀번호나 DB 주소가 달라도 코드 수정 없이 실행하기 위한 구조입니다.
    MariaDbConfig dbConfig;
    ApplyEnvironmentValue("MORPG_DB_HOST", dbConfig.host);
    ApplyEnvironmentValue("MORPG_DB_USER", dbConfig.user);
    ApplyEnvironmentValue("MORPG_DB_PASSWORD", dbConfig.password);
    ApplyEnvironmentValue("MORPG_DB_NAME", dbConfig.database);

    if (argc >= 3)
    {
        // 두 번째 실행 인자를 DB 비밀번호로 사용할 수 있게 합니다.
        // 예: ProjectMORPGServer.exe 7777 my_password
        dbConfig.password = argv[2];
    }

    PromptPasswordIfNeeded(dbConfig);

    // Repository는 DB 접근 계층입니다.
    // 지금은 MariaDB 구현체를 사용하지만, 테스트용 Mock 구현체로 교체해도 AuthService 코드는 그대로 유지됩니다.
    std::shared_ptr<IAccountRepository> repository = std::make_shared<MariaDbAccountRepository>(dbConfig);

    // AuthService는 로그인/회원가입/캐릭터 생성 같은 게임 서버 비즈니스 흐름을 담당합니다.
    std::shared_ptr<AuthService> authService = std::make_shared<AuthService>(repository);

    // TcpServer는 네트워크 접속을 받고, 각 클라이언트 세션을 생성합니다.
    TcpServer server(port, authService);
    if (!server.Start())
    {
        std::cerr << "[Server] Failed to start." << std::endl;
        return 1;
    }

    return 0;
}
