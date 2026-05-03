#include "repository/MariaDbAccountRepository.h"

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <iostream>
#include <sstream>

namespace
{
    std::int32_t ToInt(const char* value)
    {
        return value == nullptr ? 0 : std::atoi(value);
    }

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
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

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
    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    std::ostringstream query;
    query << "SELECT character_id, account_id, name, class_type, level, exp, gold, current_map_id, pos_x, pos_y, pos_z "
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
    character.name = row[2] == nullptr ? "" : row[2];
    character.classType = static_cast<ClassType>(ToInt(row[3]));
    character.level = ToInt(row[4]);
    character.exp = ToInt(row[5]);
    character.gold = ToInt(row[6]);
    character.currentMapId = ToInt(row[7]);
    character.posX = ToFloat(row[8]);
    character.posY = ToFloat(row[9]);
    character.posZ = ToFloat(row[10]);
    character.quickSlotSkillIds = LoadSkillIds(connection.get(), character.characterId);
    return character;
}

std::optional<CharacterData> MariaDbAccountRepository::CreateCharacter(
    std::int32_t accountId,
    std::int32_t slotIndex,
    ClassType classType)
{
    if (slotIndex < 0 || slotIndex >= 3 || classType == ClassType::None)
    {
        return std::nullopt;
    }

    MysqlHandle connection = Connect();
    if (!connection)
    {
        return std::nullopt;
    }

    Execute(connection.get(), "START TRANSACTION");

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

    const std::int32_t characterId = static_cast<std::int32_t>(mysql_insert_id(connection.get()));
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

MariaDbAccountRepository::MysqlHandle MariaDbAccountRepository::Connect() const
{
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

    mysql_set_character_set(connection.get(), "utf8mb4");
    return connection;
}

MariaDbAccountRepository::MysqlResult MariaDbAccountRepository::StoreResult(MYSQL* connection) const
{
    return MysqlResult(mysql_store_result(connection), mysql_free_result);
}

std::optional<AccountData> MariaDbAccountRepository::LoadAccountByLogin(MYSQL* connection, const std::string& loginId) const
{
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
    std::ostringstream query;
    query << "SELECT character_id, account_id, name, class_type, level, exp, gold, current_map_id, pos_x, pos_y, pos_z "
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
        character.name = row[2] == nullptr ? "" : row[2];
        character.classType = static_cast<ClassType>(ToInt(row[3]));
        character.level = ToInt(row[4]);
        character.exp = ToInt(row[5]);
        character.gold = ToInt(row[6]);
        character.currentMapId = ToInt(row[7]);
        character.posX = ToFloat(row[8]);
        character.posY = ToFloat(row[9]);
        character.posZ = ToFloat(row[10]);
        characters.push_back(character);
    }

    result.reset();

    for (CharacterData& character : characters)
    {
        character.quickSlotSkillIds = LoadSkillIds(connection, character.characterId);
    }

    return characters;
}

std::vector<std::int32_t> MariaDbAccountRepository::LoadSkillIds(MYSQL* connection, std::int32_t characterId) const
{
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
    return std::string(ToString(classType)) + "_" + std::to_string(slotIndex + 1);
}

std::vector<std::int32_t> MariaDbAccountRepository::GetDefaultSkillIds(ClassType classType) const
{
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
