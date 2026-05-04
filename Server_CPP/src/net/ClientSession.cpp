#include "net/ClientSession.h"

#include "protocol/PacketCodec.h"

#include <iostream>
#include <utility>

ClientSession::ClientSession(SOCKET socket, std::shared_ptr<AuthService> authService)
    : socket_(socket),
      authService_(std::move(authService))
{
}

void ClientSession::Run()
{
    // 클라이언트가 접속하면 첫 줄로 서버 식별 메시지를 보냅니다.
    SendLine("WELCOME ProjectMORPGServer");

    // 클라이언트가 보낸 줄 단위 명령을 계속 읽고 처리합니다.
    std::string line;
    while (ReceiveLine(line))
    {
        if (!HandleCommand(line))
        {
            break;
        }
    }

    // QUIT 명령을 받거나 연결이 끊기면 소켓을 닫습니다.
    closesocket(socket_);
    std::cout << "[Session] Disconnected" << std::endl;
}

bool ClientSession::ReceiveLine(std::string& outLine)
{
    while (true)
    {
        // TCP는 메시지 경계를 보장하지 않으므로, 내부 버퍼에서 '\n'을 찾아 한 줄씩 잘라냅니다.
        const std::size_t newline = receiveBuffer_.find('\n');
        if (newline != std::string::npos)
        {
            outLine = receiveBuffer_.substr(0, newline);
            if (!outLine.empty() && outLine.back() == '\r')
            {
                outLine.pop_back();
            }

            receiveBuffer_.erase(0, newline + 1);
            return true;
        }

        // 아직 한 줄이 완성되지 않았다면 소켓에서 데이터를 더 받습니다.
        char buffer[512] = {};
        const int received = recv(socket_, buffer, static_cast<int>(sizeof(buffer)), 0);
        if (received <= 0)
        {
            return false;
        }

        receiveBuffer_.append(buffer, received);
    }
}

bool ClientSession::SendLine(const std::string& line)
{
    // 현재 프로토콜은 사람이 읽기 쉬운 줄 단위 텍스트 패킷입니다.
    // Unity/C++ 연동이 안정되면 바이너리 패킷이나 Protobuf로 교체할 수 있습니다.
    const std::string payload = line + "\n";
    const int sent = send(socket_, payload.c_str(), static_cast<int>(payload.size()), 0);
    return sent == static_cast<int>(payload.size());
}

bool ClientSession::HandleCommand(const std::string& line)
{
    // "LOGIN test_user password" 같은 한 줄 문자열을 명령과 인자로 분리합니다.
    const ClientCommand command = PacketCodec::DecodeClientCommand(line);
    std::cout << "[Session] Request: " << line << std::endl;

    if (command.name == "LOGIN")
    {
        // LOGIN loginId password
        // 서버가 DB에서 계정을 조회하고 캐릭터 목록까지 내려줍니다.
        if (command.args.size() < 2)
        {
            SendLine(PacketCodec::EncodeLoginFail("InvalidLoginFormat"));
            return true;
        }

        const std::vector<std::string> responses = authService_->HandleLogin(command.args[0], command.args[1]);
        for (const std::string& response : responses)
        {
            SendLine(response);
        }

        return true;
    }

    if (command.name == "REGISTER")
    {
        // REGISTER loginId password
        // 서버가 새 계정을 DB에 생성하고 빈 캐릭터 목록을 반환합니다.
        if (command.args.size() < 2)
        {
            SendLine(PacketCodec::EncodeRegisterFail("InvalidRegisterFormat"));
            return true;
        }

        const std::vector<std::string> responses = authService_->HandleRegister(command.args[0], command.args[1]);
        for (const std::string& response : responses)
        {
            SendLine(response);
        }

        return true;
    }

    if (command.name == "CREATE_CHARACTER")
    {
        // CREATE_CHARACTER accountId slotIndex classType
        // 예: CREATE_CHARACTER 1 0 Warrior
        if (command.args.size() < 3)
        {
            SendLine(PacketCodec::EncodeCreateCharacterFail("InvalidCreateCharacterFormat"));
            return true;
        }

        const std::int32_t accountId = std::stoi(command.args[0]);
        const std::int32_t slotIndex = std::stoi(command.args[1]);
        const ClassType classType = ClassTypeFromString(command.args[2]);
        SendLine(authService_->HandleCreateCharacter(accountId, slotIndex, classType));
        return true;
    }

    if (command.name == "DELETE_CHARACTER")
    {
        // DELETE_CHARACTER accountId characterId
        // 계정 소유 캐릭터인지 확인 가능한 조건으로 삭제합니다.
        if (command.args.size() < 2)
        {
            SendLine(PacketCodec::EncodeDeleteCharacterFail(0, "InvalidDeleteCharacterFormat"));
            return true;
        }

        const std::int32_t accountId = std::stoi(command.args[0]);
        const std::int32_t characterId = std::stoi(command.args[1]);
        SendLine(authService_->HandleDeleteCharacter(accountId, characterId));
        return true;
    }

    if (command.name == "ENTER_GAME")
    {
        // ENTER_GAME characterId
        // 선택한 캐릭터의 상세 정보를 가져와 게임 입장 응답을 보냅니다.
        if (command.args.empty())
        {
            SendLine(PacketCodec::EncodeEnterGameFail("InvalidEnterGameFormat"));
            return true;
        }

        SendLine(authService_->HandleEnterGame(std::stoi(command.args[0])));
        return true;
    }

    if (command.name == "PING")
    {
        SendLine("PONG");
        return true;
    }

    if (command.name == "QUIT")
    {
        SendLine("BYE");
        return false;
    }

    SendLine("ERROR message=UnknownCommand");
    return true;
}
