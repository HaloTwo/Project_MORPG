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
    // 클라이언트가 보낸 한 줄짜리 텍스트 요청을 명령 이름과 인자 목록으로 분리합니다.
    static ClientCommand DecodeClientCommand(const std::string& line);

    // 로그인 성공 결과를 Unity가 읽을 수 있는 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeLoginOk(const AccountData& account);

    // 로그인 실패 사유를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeLoginFail(const std::string& message);

    // 회원가입 성공 결과를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeRegisterOk(const AccountData& account);

    // 회원가입 실패 사유를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeRegisterFail(const std::string& message);

    // 캐릭터 목록을 여러 줄의 텍스트 응답 패킷으로 만듭니다.
    static std::vector<std::string> EncodeCharacterList(const std::vector<CharacterData>& characters);

    // 게임 입장 성공 시 선택 캐릭터 정보를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeEnterGameOk(const CharacterData& character);

    // 게임 입장 실패 사유를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeEnterGameFail(const std::string& message);

    // 캐릭터 생성 성공 결과를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeCreateCharacterOk(const CharacterData& character);

    // 캐릭터 생성 실패 사유를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeCreateCharacterFail(const std::string& message);

    // 캐릭터 삭제 성공 결과를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeDeleteCharacterOk(std::int32_t characterId);

    // 캐릭터 삭제 실패 사유를 텍스트 응답 패킷으로 만듭니다.
    static std::string EncodeDeleteCharacterFail(std::int32_t characterId, const std::string& message);

private:
    // CharacterData 하나를 공통 CHARACTER 응답 포맷으로 변환합니다.
    static std::string EncodeCharacter(const CharacterData& character);

    // 스킬 ID 배열을 "1001,1002,1003" 같은 문자열로 합칩니다.
    static std::string JoinSkillIds(const std::vector<std::int32_t>& skillIds);
};
