#include "repository/MariaDbAccountRepository.h"

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <iostream>
#include <sstream>

namespace
{
    // MariaDB C API는 값을 문자열 포인터로 돌려주므로, nullptr 방어 후 int로 변환합니다.
    std::int32_t ToInt(const char* value)
    {
        return value == nullptr ? 0 : std::atoi(value);
    }

    // DB에서 읽은 좌표값을 float으로 변환합니다.
    float ToFloat(const char* value)
    {
        return value == nullptr ? 0.0f : static_cast<float>(std::atof(value));
    }
}

MariaDbAccountRepository::MariaDbAccountRepository(MariaDbConfig config)
    : config_(std::move(config))
{
}

std::optional<AccountData> MariaDbAccountRepository::FindAccountByLogin(
    const std::string& loginId,
    const std::string& authToken)
{
    // 로그인 요청 처리:
    // 1. DB 연결
    // 2. login_id/password_hash 일치 계정 조회
    // 3. 캐릭터 목록 로드
    // 4. 마지막 로그인 시간 갱신
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    // SQL Injection 방지를 위해 loginId/authToken은 Escape 처리 후 쿼리에 넣습니다.
    const std::string normalizedLoginId = NormalizeLoginId(loginId);
    std::ostringstream query;
    query << "SELECT account_id, login_id, password_hash FROM accounts "
          << "WHERE login_id = '" << Escape(connection.get(), normalizedLoginId) << "' "
          << "AND password_hash = '" << Escape(connection.get(), authToken) << "' "
          << "LIMIT 1";

    if (!Execute(connection.get(), query.str()))
    {
        return std::nullopt;
    }

    MysqlResult result = StoreResult(connection.get());
    MYSQL_ROW row = result ? mysql_fetch_row(result.get()) : nullptr;
    if (row == nullptr)
    {
        return std::nullopt;
    }

    AccountData account;
    account.accountId = ToInt(row[0]);
    account.loginId = row[1] == nullptr ? "" : row[1];
    account.password = row[2] == nullptr ? "" : row[2];
    account.displayName = account.loginId;
    account.characters = LoadCharacters(connection.get(), account.accountId);

    Execute(connection.get(), "UPDATE accounts SET last_login_at = NOW() WHERE account_id = " + std::to_string(account.accountId));
    return account;
}

std::optional<AccountData> MariaDbAccountRepository::RegisterAccount(
    const std::string& loginId,
    const std::string& password)
{
    // 회원가입 요청 처리:
    // accounts 테이블에 새 계정을 넣고, 다시 계정 정보를 로드해 반환합니다.
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    const std::string normalizedLoginId = NormalizeLoginId(loginId);
    if (normalizedLoginId.empty() || password.empty())
    {
        return std::nullopt;
    }

    std::ostringstream insert;
    insert << "INSERT INTO accounts (login_id, password_hash) VALUES ('"
           << Escape(connection.get(), normalizedLoginId) << "', '"
           << Escape(connection.get(), password) << "')";

    if (!Execute(connection.get(), insert.str()))
    {
        return std::nullopt;
    }

    return LoadAccountByLogin(connection.get(), normalizedLoginId);
}

std::optional<CharacterData> MariaDbAccountRepository::FindCharacterById(std::int32_t characterId)
{
    // 게임 입장 시 선택한 캐릭터의 상세 정보를 다시 조회합니다.
    // 캐릭터 기본 정보와 퀵슬롯 스킬 정보를 함께 구성합니다.
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    std::ostringstream query;
    query << "SELECT character_id, account_id, slot_index, name, class_type, level, exp, gold, current_map_id, pos_x, pos_y, pos_z "
          << "FROM characters WHERE character_id = " << characterId << " LIMIT 1";

    if (!Execute(connection.get(), query.str()))
    {
        return std::nullopt;
    }

    MysqlResult result = StoreResult(connection.get());
    MYSQL_ROW row = result ? mysql_fetch_row(result.get()) : nullptr;
    if (row == nullptr)
    {
        return std::nullopt;
    }

    CharacterData character;
    character.characterId = ToInt(row[0]);
    character.accountId = ToInt(row[1]);
    character.slotIndex = ToInt(row[2]);
    character.name = row[3] == nullptr ? "" : row[3];
    character.classType = static_cast<ClassType>(ToInt(row[4]));
    character.level = ToInt(row[5]);
    character.exp = ToInt(row[6]);
    character.gold = ToInt(row[7]);
    character.currentMapId = ToInt(row[8]);
    character.posX = ToFloat(row[9]);
    character.posY = ToFloat(row[10]);
    character.posZ = ToFloat(row[11]);
    character.quickSlotSkillIds = LoadSkillIds(connection.get(), character.characterId);
    return character;
}

std::optional<CharacterData> MariaDbAccountRepository::CreateCharacter(
    std::int32_t accountId,
    std::int32_t slotIndex,
    ClassType classType)
{
    // 캐릭터 생성 요청 처리:
    // 슬롯 범위와 직업 유효성을 먼저 확인합니다.
    if (slotIndex < 0 || slotIndex >= 3 || classType == ClassType::None)
    {
        return std::nullopt;
    }

    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    // 캐릭터 생성과 기본 스킬 추가는 하나의 트랜잭션으로 처리합니다.
    // 중간에 실패하면 ROLLBACK하여 불완전한 캐릭터가 남지 않게 합니다.
    Execute(connection.get(), "START TRANSACTION");

    // 계정당 캐릭터 최대 3개 제한입니다.
    std::ostringstream countQuery;
    countQuery << "SELECT COUNT(*) FROM characters WHERE account_id = " << accountId;
    if (!Execute(connection.get(), countQuery.str()))
    {
        Execute(connection.get(), "ROLLBACK");
        return std::nullopt;
    }

    MysqlResult countResult = StoreResult(connection.get());
    MYSQL_ROW countRow = countResult ? mysql_fetch_row(countResult.get()) : nullptr;
    if (countRow == nullptr || ToInt(countRow[0]) >= 3)
    {
        Execute(connection.get(), "ROLLBACK");
        return std::nullopt;
    }

    // 현재는 직업/슬롯 기준으로 임시 이름과 시작 위치를 정합니다.
    // 나중에 클라이언트에서 이름 입력을 받게 되면 이 부분을 교체하면 됩니다.
    const float x = slotIndex == 0 ? -2.0f : slotIndex == 1 ? 0.0f : 2.0f;
    const std::string name = BuildCharacterName(classType, slotIndex);

    std::ostringstream insertCharacter;
    insertCharacter << "INSERT INTO characters "
                    << "(account_id, slot_index, name, class_type, level, exp, gold, current_map_id, pos_x, pos_y, pos_z) VALUES ("
                    << accountId << ", "
                    << slotIndex << ", '"
                    << Escape(connection.get(), name) << "', "
                    << static_cast<int>(classType) << ", 1, 0, 100, 1, "
                    << x << ", 1.0, 0.0)";

    if (!Execute(connection.get(), insertCharacter.str()))
    {
        Execute(connection.get(), "ROLLBACK");
        return std::nullopt;
    }

    // 캐릭터 생성 후 방금 생성된 character_id를 가져옵니다.
    const std::int32_t characterId = static_cast<std::int32_t>(mysql_insert_id(connection.get()));

    // 직업별 기본 스킬 3개를 character_skills 테이블에 넣습니다.
    const std::vector<std::int32_t> skillIds = GetDefaultSkillIds(classType);
    for (std::size_t i = 0; i < skillIds.size(); ++i)
    {
        std::ostringstream insertSkill;
        insertSkill << "INSERT INTO character_skills (character_id, slot_index, skill_id) VALUES ("
                    << characterId << ", " << i << ", " << skillIds[i] << ")";

        if (!Execute(connection.get(), insertSkill.str()))
        {
            Execute(connection.get(), "ROLLBACK");
            return std::nullopt;
        }
    }

    Execute(connection.get(), "COMMIT");
    return FindCharacterById(characterId);
}

bool MariaDbAccountRepository::DeleteCharacter(std::int32_t accountId, std::int32_t characterId)
{
    // 삭제 요청은 account_id와 character_id를 함께 조건으로 사용합니다.
    // 다른 계정의 캐릭터를 삭제하지 못하게 하기 위한 최소한의 서버 측 검증입니다.
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return false;
    }

    std::ostringstream query;
    query << "DELETE FROM characters WHERE account_id = "
          << accountId << " AND character_id = " << characterId << " LIMIT 1";

    if (!Execute(connection.get(), query.str()))
    {
        return false;
    }

    return mysql_affected_rows(connection.get()) == 1;
}

MariaDbAccountRepository::MysqlHandle MariaDbAccountRepository::Connect() const
{
    // mysql_init으로 핸들을 만들고 mysql_real_connect로 실제 DB에 접속합니다.
    // 실패하면 nullptr 핸들을 반환해 호출자가 실패 처리하도록 합니다.
    MysqlHandle connection(mysql_init(nullptr), mysql_close);
    if (!connection)
    {
        return {nullptr, mysql_close};
    }

    if (mysql_real_connect(
        connection.get(),
        config_.host.c_str(),
        config_.user.c_str(),
        config_.password.c_str(),
        config_.database.c_str(),
        config_.port,
        nullptr,
        0) == nullptr)
    {
        std::cerr << "[MariaDB] connect failed: " << mysql_error(connection.get()) << std::endl;
        return {nullptr, mysql_close};
    }

    // 한글 캐릭터 이름/텍스트 저장을 위해 utf8mb4를 사용합니다.
    mysql_set_character_set(connection.get(), "utf8mb4");
    return connection;
}

MariaDbAccountRepository::MysqlResult MariaDbAccountRepository::StoreResult(MYSQL* connection) const
{
    // SELECT 결과를 MYSQL_RES로 가져옵니다.
    // unique_ptr deleter가 mysql_free_result를 호출합니다.
    return MysqlResult(mysql_store_result(connection), mysql_free_result);
}

std::optional<AccountData> MariaDbAccountRepository::LoadAccountByLogin(MYSQL* connection, const std::string& loginId) const
{
    // 회원가입 직후 또는 재조회가 필요할 때 계정 정보를 다시 로드합니다.
    std::ostringstream query;
    query << "SELECT account_id, login_id, password_hash FROM accounts "
          << "WHERE login_id = '" << Escape(connection, loginId) << "' LIMIT 1";

    if (!Execute(connection, query.str()))
    {
        return std::nullopt;
    }

    MysqlResult result = StoreResult(connection);
    MYSQL_ROW row = result ? mysql_fetch_row(result.get()) : nullptr;
    if (row == nullptr)
    {
        return std::nullopt;
    }

    AccountData account;
    account.accountId = ToInt(row[0]);
    account.loginId = row[1] == nullptr ? "" : row[1];
    account.password = row[2] == nullptr ? "" : row[2];
    account.displayName = account.loginId;
    account.characters = LoadCharacters(connection, account.accountId);
    return account;
}

std::vector<CharacterData> MariaDbAccountRepository::LoadCharacters(MYSQL* connection, std::int32_t accountId) const
{
    // 캐릭터 선택창에 보여줄 계정의 캐릭터 목록을 로드합니다.
    std::ostringstream query;
    query << "SELECT character_id, account_id, slot_index, name, class_type, level, exp, gold, current_map_id, pos_x, pos_y, pos_z "
          << "FROM characters WHERE account_id = " << accountId << " ORDER BY slot_index";

    if (!Execute(connection, query.str()))
    {
        return {};
    }

    MysqlResult result = StoreResult(connection);
    std::vector<CharacterData> characters;
    MYSQL_ROW row = nullptr;
    while (result && (row = mysql_fetch_row(result.get())) != nullptr)
    {
        CharacterData character;
        character.characterId = ToInt(row[0]);
        character.accountId = ToInt(row[1]);
        character.slotIndex = ToInt(row[2]);
        character.name = row[3] == nullptr ? "" : row[3];
        character.classType = static_cast<ClassType>(ToInt(row[4]));
        character.level = ToInt(row[5]);
        character.exp = ToInt(row[6]);
        character.gold = ToInt(row[7]);
        character.currentMapId = ToInt(row[8]);
        character.posX = ToFloat(row[9]);
        character.posY = ToFloat(row[10]);
        character.posZ = ToFloat(row[11]);
        characters.push_back(character);
    }

    // 같은 connection에서 다음 SELECT를 실행하기 전에 결과를 명시적으로 해제합니다.
    result.reset();

    // 각 캐릭터별 퀵슬롯 스킬 목록을 별도 테이블에서 읽습니다.
    for (CharacterData& character : characters)
    {
        character.quickSlotSkillIds = LoadSkillIds(connection, character.characterId);
    }

    return characters;
}

std::vector<std::int32_t> MariaDbAccountRepository::LoadSkillIds(MYSQL* connection, std::int32_t characterId) const
{
    // character_skills 테이블에서 슬롯 순서대로 스킬 ID를 읽습니다.
    std::ostringstream query;
    query << "SELECT skill_id FROM character_skills WHERE character_id = "
          << characterId << " ORDER BY slot_index";

    if (!Execute(connection, query.str()))
    {
        return {};
    }

    MysqlResult result = StoreResult(connection);
    std::vector<std::int32_t> skillIds;
    MYSQL_ROW row = nullptr;
    while (result && (row = mysql_fetch_row(result.get())) != nullptr)
    {
        skillIds.push_back(ToInt(row[0]));
    }

    return skillIds;
}

bool MariaDbAccountRepository::Execute(MYSQL* connection, const std::string& sql) const
{
    // 모든 SQL 실행은 이 함수로 모아 에러 로그 형식을 통일합니다.
    if (mysql_query(connection, sql.c_str()) != 0)
    {
        std::cerr << "[MariaDB] query failed: " << mysql_error(connection) << std::endl;
        std::cerr << "[MariaDB] sql: " << sql << std::endl;
        return false;
    }

    return true;
}

std::string MariaDbAccountRepository::Escape(MYSQL* connection, const std::string& value) const
{
    // 문자열 값을 SQL에 넣기 전에 MariaDB API로 escape 처리합니다.
    // 현재는 직접 SQL 문자열을 만들고 있으므로 반드시 필요한 단계입니다.
    std::string escaped;
    escaped.resize(value.size() * 2 + 1);
    const unsigned long length = mysql_real_escape_string(
        connection,
        escaped.data(),
        value.c_str(),
        static_cast<unsigned long>(value.size()));
    escaped.resize(length);
    return escaped;
}

std::string MariaDbAccountRepository::NormalizeLoginId(const std::string& loginId) const
{
    // 로그인 ID는 앞뒤 공백 제거 후 소문자로 통일합니다.
    // 같은 계정을 Test_User/test_user처럼 중복 생성하지 않기 위한 처리입니다.
    std::string normalized = loginId;
    normalized.erase(normalized.begin(), std::find_if(normalized.begin(), normalized.end(), [](unsigned char ch)
    {
        return !std::isspace(ch);
    }));
    normalized.erase(std::find_if(normalized.rbegin(), normalized.rend(), [](unsigned char ch)
    {
        return !std::isspace(ch);
    }).base(), normalized.end());
    std::transform(normalized.begin(), normalized.end(), normalized.begin(), [](unsigned char ch)
    {
        return static_cast<char>(std::tolower(ch));
    });
    return normalized;
}

std::string MariaDbAccountRepository::BuildCharacterName(ClassType classType, std::int32_t slotIndex) const
{
    // 현재는 캐릭터 이름 입력 UI가 없으므로 임시 이름을 서버에서 생성합니다.
    return std::string(ToString(classType)) + "_" + std::to_string(slotIndex + 1);
}

std::vector<std::int32_t> MariaDbAccountRepository::GetDefaultSkillIds(ClassType classType) const
{
    // Unity 클라이언트 SkillDatabase와 맞춘 직업별 기본 스킬 ID입니다.
    switch (classType)
    {
    case ClassType::Warrior:
        return {1001, 1002, 1003};
    case ClassType::Archer:
        return {2001, 2002, 2003};
    case ClassType::Rogue:
        return {3001, 3002, 3003};
    default:
        return {};
    }
}
