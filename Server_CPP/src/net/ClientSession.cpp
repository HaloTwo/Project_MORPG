#include "net/ClientSession.h"

#include "protocol/PacketCodec.h"

#include <iostream>
#include <sstream>
#include <utility>

ClientSession::ClientSession(SOCKET socket, std::shared_ptr<AuthService> authService)
    : socket_(socket),
      authService_(std::move(authService))
{
}

void ClientSession::SetCallbacks(BroadcastCallback broadcastCallback, EnterCallback enterCallback, LeaveCallback leaveCallback)
{
    broadcastCallback_ = std::move(broadcastCallback);
    enterCallback_ = std::move(enterCallback);
    leaveCallback_ = std::move(leaveCallback);
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

    if (leaveCallback_ != nullptr)
    {
        leaveCallback_(shared_from_this());
    }

    closesocket(socket_);
    std::cout << "[Session] Disconnected" << std::endl;
}

bool ClientSession::SendFromServer(const std::string& line)
{
    return SendLine(line);
}

bool ClientSession::IsInGame() const
{
    return inGame_ && actorId_ > 0;
}

std::int32_t ClientSession::GetActorId() const
{
    return actorId_;
}

std::string ClientSession::MakeSpawnLine() const
{
    std::ostringstream stream;
    stream << "SPAWN actorId=" << actorId_
        << " type=Player"
        << " pos=" << posX_ << "," << posY_ << "," << posZ_
        << " yaw=" << yaw_;
    return stream.str();
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
    std::lock_guard<std::mutex> lock(sendMutex_);
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
        HandleEnterGame(command);
        return true;
    }

    if (command.name == "MOVE")
    {
        HandleMove(command);
        return true;
    }

    if (command.name == "STOP")
    {
        HandleStop(command);
        return true;
    }

    if (command.name == "SKILL")
    {
        HandleSkill(command);
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

void ClientSession::HandleEnterGame(const ClientCommand& command)
{
    if (command.args.empty())
    {
        SendLine(PacketCodec::EncodeEnterGameFail("InvalidEnterGameFormat"));
        return;
    }

    actorId_ = std::stoi(command.args[0]);
    const std::string response = authService_->HandleEnterGame(actorId_);
    SendLine(response);

    if (response.rfind("ENTER_GAME_OK", 0) != 0)
    {
        actorId_ = 0;
        inGame_ = false;
        return;
    }

    inGame_ = true;
    posX_ = 0.0f;
    posY_ = 1.0f;
    posZ_ = 0.0f;
    yaw_ = 0.0f;

    if (enterCallback_ != nullptr)
    {
        enterCallback_(shared_from_this());
    }
}

bool ClientSession::EnsureInGameFromRealtimeCommand(const ClientCommand& command)
{
    if (IsInGame())
    {
        return true;
    }

    if (command.args.empty())
    {
        return false;
    }

    actorId_ = std::stoi(command.args[0]);
    inGame_ = actorId_ > 0;

    if (!inGame_)
    {
        return false;
    }

    std::cout << "[Session] Recovered in-game session from realtime packet actor=" << actorId_ << std::endl;
    if (enterCallback_ != nullptr)
    {
        enterCallback_(shared_from_this());
    }

    return true;
}

void ClientSession::HandleMove(const ClientCommand& command)
{
    if (!EnsureInGameFromRealtimeCommand(command) || command.args.size() < 9)
    {
        return;
    }

    posX_ = ReadFloatArg(command, 1, posX_);
    posY_ = ReadFloatArg(command, 2, posY_);
    posZ_ = ReadFloatArg(command, 3, posZ_);
    yaw_ = ReadFloatArg(command, 7, yaw_);

    std::ostringstream stream;
    stream << "MOVE actorId=" << actorId_
        << " pos=" << posX_ << "," << posY_ << "," << posZ_
        << " dir=" << ReadFloatArg(command, 4, 0.0f) << "," << ReadFloatArg(command, 5, 0.0f) << "," << ReadFloatArg(command, 6, 0.0f)
        << " yaw=" << yaw_
        << " speed=" << ReadFloatArg(command, 8, 6.0f);
    BroadcastToOtherSessions(stream.str());
}

void ClientSession::HandleStop(const ClientCommand& command)
{
    if (!EnsureInGameFromRealtimeCommand(command) || command.args.size() < 5)
    {
        return;
    }

    posX_ = ReadFloatArg(command, 1, posX_);
    posY_ = ReadFloatArg(command, 2, posY_);
    posZ_ = ReadFloatArg(command, 3, posZ_);
    yaw_ = ReadFloatArg(command, 4, yaw_);

    std::ostringstream stream;
    stream << "STOP actorId=" << actorId_
        << " pos=" << posX_ << "," << posY_ << "," << posZ_
        << " yaw=" << yaw_;
    BroadcastToOtherSessions(stream.str());
}

void ClientSession::HandleSkill(const ClientCommand& command)
{
    if (!EnsureInGameFromRealtimeCommand(command) || command.args.size() < 9)
    {
        return;
    }

    std::ostringstream stream;
    stream << "SKILL casterId=" << actorId_
        << " slot=" << command.args[1]
        << " skillId=" << command.args[2]
        << " pos=" << command.args[3] << "," << command.args[4] << "," << command.args[5]
        << " dir=" << command.args[6] << "," << command.args[7] << "," << command.args[8];
    BroadcastToOtherSessions(stream.str());
}

void ClientSession::BroadcastToOtherSessions(const std::string& line)
{
    std::cout << "[Session] Broadcast request actor=" << actorId_
        << " line=" << line << std::endl;

    if (broadcastCallback_ != nullptr)
    {
        broadcastCallback_(shared_from_this(), line);
        return;
    }

    std::cout << "[Session] Broadcast callback is null actor=" << actorId_ << std::endl;
}

float ClientSession::ReadFloatArg(const ClientCommand& command, std::size_t index, float fallback)
{
    if (index >= command.args.size())
    {
        return fallback;
    }

    try
    {
        return std::stof(command.args[index]);
    }
    catch (...)
    {
        return fallback;
    }
}
