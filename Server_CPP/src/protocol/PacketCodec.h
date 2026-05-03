#pragma once

#include "domain/AccountData.h"

#include <string>
#include <vector>

struct ClientCommand
{
    std::string name;
    std::vector<std::string> args;
};

class PacketCodec
{
public:
    // 줄 단위 텍스트 요청을 명령 이름과 인자로 분리합니다.
    static ClientCommand DecodeClientCommand(const std::string& line);

    // 로그인 성공 응답을 텍스트 패킷으로 만듭니다.
    static std::string EncodeLoginOk(const AccountData& account);

    // 로그인 실패 응답을 텍스트 패킷으로 만듭니다.
    static std::string EncodeLoginFail(const std::string& message);

    /// <summary>
    /// 회원가입 성공 응답을 텍스트 패킷으로 만듭니다.
    /// </summary>
    static std::string EncodeRegisterOk(const AccountData& account);

    /// <summary>
    /// 회원가입 실패 응답을 텍스트 패킷으로 만듭니다.
    /// </summary>
    static std::string EncodeRegisterFail(const std::string& message);

    // 캐릭터 목록 응답을 텍스트 패킷 묶음으로 만듭니다.
    static std::vector<std::string> EncodeCharacterList(const std::vector<CharacterData>& characters);

    // 게임 입장 응답을 텍스트 패킷으로 만듭니다.
    static std::string EncodeEnterGameOk(const CharacterData& character);

    // 게임 입장 실패 응답을 텍스트 패킷으로 만듭니다.
    static std::string EncodeEnterGameFail(const std::string& message);

    /// <summary>
    /// 캐릭터 생성 성공 응답을 텍스트 패킷으로 만듭니다.
    /// </summary>
    static std::string EncodeCreateCharacterOk(const CharacterData& character);

    /// <summary>
    /// 캐릭터 생성 실패 응답을 텍스트 패킷으로 만듭니다.
    /// </summary>
    static std::string EncodeCreateCharacterFail(const std::string& message);

    /// 캐릭터 삭제 성공 응답을 텍스트 프로토콜로 만듭니다.
    static std::string EncodeDeleteCharacterOk(std::int32_t characterId);

    /// 캐릭터 삭제 실패 응답을 텍스트 프로토콜로 만듭니다.
    static std::string EncodeDeleteCharacterFail(std::int32_t characterId, const std::string& message);

private:
    static std::string EncodeCharacter(const CharacterData& character);
    static std::string JoinSkillIds(const std::vector<std::int32_t>& skillIds);
};
