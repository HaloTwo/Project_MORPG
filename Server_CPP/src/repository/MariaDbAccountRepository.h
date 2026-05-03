#pragma once

#include "repository/IAccountRepository.h"

#include <mysql.h>

#include <memory>
#include <string>
#include <vector>

struct MariaDbConfig
{
    std::string host = "127.0.0.1";
    unsigned int port = 3306;
    std::string user = "root";
    std::string password = "1234";
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

private:
    using MysqlHandle = std::unique_ptr<MYSQL, void(*)(MYSQL*)>;
    using MysqlResult = std::unique_ptr<MYSQL_RES, void(*)(MYSQL_RES*)>;

    MariaDbConfig config_;

    MysqlHandle Connect() const;
    MysqlResult StoreResult(MYSQL* connection) const;
    std::optional<AccountData> LoadAccountByLogin(MYSQL* connection, const std::string& loginId) const;
    std::vector<CharacterData> LoadCharacters(MYSQL* connection, std::int32_t accountId) const;
    std::vector<std::int32_t> LoadSkillIds(MYSQL* connection, std::int32_t characterId) const;
    bool Execute(MYSQL* connection, const std::string& sql) const;
    std::string Escape(MYSQL* connection, const std::string& value) const;
    std::string NormalizeLoginId(const std::string& loginId) const;
    std::string BuildCharacterName(ClassType classType, std::int32_t slotIndex) const;
    std::vector<std::int32_t> GetDefaultSkillIds(ClassType classType) const;
};
