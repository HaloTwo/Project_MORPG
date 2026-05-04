#pragma once

#include "domain/CharacterData.h"

#include <cstdint>
#include <string>
#include <vector>

struct AccountData
{
    // DB accounts.account_id
    std::int32_t accountId = 0;

    // 로그인에 사용하는 ID입니다.
    std::string loginId;

    // 현재는 학습용으로 password_hash 값을 그대로 담습니다.
    // 실제 서비스에서는 평문 비밀번호를 서버 메모리/패킷에 오래 들고 있지 않는 구조가 필요합니다.
    std::string password;

    // UI 표시용 계정 이름입니다.
    std::string displayName;

    // 로그인 성공 후 캐릭터 선택창에 보여줄 캐릭터 목록입니다.
    std::vector<CharacterData> characters;
};
