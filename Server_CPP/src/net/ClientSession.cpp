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
    SendLine("WELCOME ProjectMORPGServer");

    std::string line;
    while (ReceiveLine(line))
    {
        if (!HandleCommand(line))
        {
            break;
        }
    }

    closesocket(socket_);
    std::cout << "[Session] Disconnected" << std::endl;
}

bool ClientSession::ReceiveLine(std::string& outLine)
{
    while (true)
    {
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
    const std::string payload = line + "\n";
    const int sent = send(socket_, payload.c_str(), static_cast<int>(payload.size()), 0);
    return sent == static_cast<int>(payload.size());
}

bool ClientSession::HandleCommand(const std::string& line)
{
    const ClientCommand command = PacketCodec::DecodeClientCommand(line);
    std::cout << "[Session] Request: " << line << std::endl;

    if (command.name == "LOGIN")
    {
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
