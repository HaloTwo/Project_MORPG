#include "service/AuthService.h"

#include "protocol/PacketCodec.h"

AuthService::AuthService(std::shared_ptr<IAccountRepository> accountRepository)
    : accountRepository_(std::move(accountRepository))
{
}

std::vector<std::string> AuthService::HandleLogin(const std::string& loginId, const std::string& authToken)
{
    std::vector<std::string> responses;
    const std::optional<AccountData> account = accountRepository_->FindAccountByLogin(loginId, authToken);
    if (!account.has_value())
    {
        responses.emplace_back(PacketCodec::EncodeLoginFail("InvalidAccount"));
        return responses;
    }

    responses.emplace_back(PacketCodec::EncodeLoginOk(account.value()));

    std::vector<std::string> characterLines = PacketCodec::EncodeCharacterList(account->characters);
    responses.insert(responses.end(), characterLines.begin(), characterLines.end());
    return responses;
}

std::string AuthService::HandleEnterGame(std::int32_t characterId)
{
    const std::optional<CharacterData> character = accountRepository_->FindCharacterById(characterId);
    if (!character.has_value())
    {
        return PacketCodec::EncodeEnterGameFail("CharacterNotFound");
    }

    return PacketCodec::EncodeEnterGameOk(character.value());
}
