#pragma once

#include "protocol/PacketCodec.h"
#include "service/AuthService.h"

#include <winsock2.h>

#include <cstdint>
#include <cstddef>
#include <functional>
#include <memory>
#include <mutex>
#include <string>

class ClientSession : public std::enable_shared_from_this<ClientSession>
{
public:
    using BroadcastCallback = std::function<void(std::shared_ptr<ClientSession>, const std::string&)>;
    using EnterCallback = std::function<void(std::shared_ptr<ClientSession>)>;
    using LeaveCallback = std::function<void(std::shared_ptr<ClientSession>)>;

    ClientSession(SOCKET socket, std::shared_ptr<AuthService> authService);

    void SetCallbacks(BroadcastCallback broadcastCallback, EnterCallback enterCallback, LeaveCallback leaveCallback);

    // 클라이언트 세션 루프를 실행합니다. 각 세션은 현재 구조에서 accept thread 하나가 소유합니다.
    void Run();

    // 서버의 다른 세션 스레드에서 이 세션으로 브로드캐스트를 보낼 때 사용합니다.
    bool SendFromServer(const std::string& line);

    bool IsInGame() const;
    std::int32_t GetActorId() const;
    std::string MakeSpawnLine() const;

private:
    SOCKET socket_ = INVALID_SOCKET;
    std::shared_ptr<AuthService> authService_;
    std::string receiveBuffer_;
    mutable std::mutex sendMutex_;

    BroadcastCallback broadcastCallback_;
    EnterCallback enterCallback_;
    LeaveCallback leaveCallback_;

    std::int32_t actorId_ = 0;
    bool inGame_ = false;
    float posX_ = 0.0f;
    float posY_ = 1.0f;
    float posZ_ = 0.0f;
    float yaw_ = 0.0f;

    bool ReceiveLine(std::string& outLine);
    bool SendLine(const std::string& line);
    bool HandleCommand(const std::string& line);
    void HandleEnterGame(const ClientCommand& command);
    bool EnsureInGameFromRealtimeCommand(const ClientCommand& command);
    void HandleMove(const ClientCommand& command);
    void HandleStop(const ClientCommand& command);
    void HandleSkill(const ClientCommand& command);
    void HandleChat(const ClientCommand& command);
    void BroadcastToOtherSessions(const std::string& line);
    static float ReadFloatArg(const ClientCommand& command, std::size_t index, float fallback);
};
