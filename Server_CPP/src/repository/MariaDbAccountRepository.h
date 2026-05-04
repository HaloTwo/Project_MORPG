#pragma once

#include "repository/IAccountRepository.h"

#include <mysql.h>

#include <memory>
#include <string>
#include <vector>

struct MariaDbConfig
{
    // 로컬 MariaDB 접속 기본값입니다.
    // 실제 실행 시 환경변수나 실행 인자로 비밀번호를 주입할 수 있습니다.
    std::string host = "127.0.0.1";
    unsigned int port = 3306;
    std::string user = "root";
    std::string password;
    std::string database = "project_morpg";
};

class MariaDbAccountRepository final : public IAccountRepository
{
public:
    explicit MariaDbAccountRepository(MariaDbConfig config);

    std::optional<AccountData> FindAccountByLogin(
        const std::string& loginId,
        const std::string& authToken) override;

    std::optional<AccountData> RegisterAccount(
        const std::string& loginId,
        const std::string& password) override;

    std::optional<CharacterData> FindCharacterById(std::int32_t characterId) override;

    std::optional<CharacterData> CreateCharacter(
        std::int32_t accountId,
        std::int32_t slotIndex,
        ClassType classType) override;

    bool DeleteCharacter(std::int32_t accountId, std::int32_t characterId) override;

private:
    // MYSQL/MYSQL_RES를 unique_ptr로 감싸서 함수 종료 시 자동으로 mysql_close/mysql_free_result가 호출되게 합니다.
    using MysqlHandle = std::unique_ptr<MYSQL, void(*)(MYSQL*)>;
    using MysqlResult = std::unique_ptr<MYSQL_RES, void(*)(MYSQL_RES*)>;

    MariaDbConfig config_;

    // MariaDB에 새 연결을 엽니다.
    MysqlHandle Connect() const;

    // SELECT 결과를 RAII 핸들로 감싸서 반환합니다.
    MysqlResult StoreResult(MYSQL* connection) const;

    // loginId로 계정 1개와 캐릭터 목록을 다시 로드합니다.
    std::optional<AccountData> LoadAccountByLogin(MYSQL* connection, const std::string& loginId) const;

    // 계정 소유 캐릭터 목록을 slot_index 순서로 로드합니다.
    std::vector<CharacterData> LoadCharacters(MYSQL* connection, std::int32_t accountId) const;

    // 캐릭터의 퀵슬롯 스킬 ID 목록을 로드합니다.
    std::vector<std::int32_t> LoadSkillIds(MYSQL* connection, std::int32_t characterId) const;

    // SQL을 실행하고 실패 시 에러 로그를 출력합니다.
    bool Execute(MYSQL* connection, const std::string& sql) const;

    // SQL 문자열에 직접 들어가는 값을 escape 처리합니다.
    std::string Escape(MYSQL* connection, const std::string& value) const;

    // 로그인 ID 앞뒤 공백 제거 및 소문자 변환을 수행합니다.
    std::string NormalizeLoginId(const std::string& loginId) const;

    // 임시 캐릭터 이름을 직업/슬롯 기반으로 생성합니다.
    std::string BuildCharacterName(ClassType classType, std::int32_t slotIndex) const;

    // 직업별 기본 스킬 3개를 반환합니다.
    std::vector<std::int32_t> GetDefaultSkillIds(ClassType classType) const;
};
