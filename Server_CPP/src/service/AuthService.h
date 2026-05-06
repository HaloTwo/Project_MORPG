#pragma once

#include "repository/IAccountRepository.h"

#include <memory>
#include <string>
#include <vector>

class AuthService
{
public:
    explicit AuthService(std::shared_ptr<IAccountRepository> accountRepository);

    // LOGIN 요청을 처리하고 응답 패킷 목록을 반환합니다.
    // 성공 시 LOGIN_OK와 CHARACTER_LIST 묶음을 함께 반환합니다.
    std::vector<std::string> HandleLogin(const std::string& loginId, const std::string& authToken);

    // REGISTER 요청을 처리하고 회원가입 결과와 캐릭터 목록 응답을 반환합니다.
    std::vector<std::string> HandleRegister(const std::string& loginId, const std::string& password);

    // ENTER_GAME 요청을 처리하고 선택 캐릭터 입장 결과를 반환합니다.
    std::string HandleEnterGame(std::int32_t characterId);

    // CREATE_CHARACTER 요청을 처리하고 생성 결과를 반환합니다.
    std::string HandleCreateCharacter(std::int32_t accountId, std::int32_t slotIndex, ClassType classType, const std::string& characterName);

    // DELETE_CHARACTER 요청을 처리하고 삭제 결과를 반환합니다.
    std::string HandleDeleteCharacter(std::int32_t accountId, std::int32_t characterId);

private:
    // AuthService는 DB를 직접 알지 않고 Repository 인터페이스에만 의존합니다.
    // 그래서 Mock DB, MariaDB 구현체를 교체해도 서비스 로직은 유지됩니다.
    std::shared_ptr<IAccountRepository> accountRepository_;

    // 원문 비밀번호를 DB에 저장하지 않도록 SHA-256 해시 문자열로 변환합니다.
    std::string HashPassword(const std::string& password) const;
};
