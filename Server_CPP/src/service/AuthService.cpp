#include "service/AuthService.h"

#include "protocol/PacketCodec.h"

#include <utility>

AuthService::AuthService(std::shared_ptr<IAccountRepository> accountRepository)
    : accountRepository_(std::move(accountRepository))
{
}

std::vector<std::string> AuthService::HandleLogin(const std::string& loginId, const std::string& authToken)
{
    // 로그인 요청의 중심 흐름입니다.
    // Repository가 DB에서 계정과 캐릭터 목록을 가져오고, Service가 응답 패킷 순서를 구성합니다.
    std::vector<std::string> responses;
    const std::optional<AccountData> account = accountRepository_->FindAccountByLogin(loginId, authToken);
    if (!account.has_value())
    {
        responses.emplace_back(PacketCodec::EncodeLoginFail("InvalidAccount"));
        return responses;
    }

    // 로그인 성공 응답을 먼저 보내고, 이어서 캐릭터 목록을 보냅니다.
    responses.emplace_back(PacketCodec::EncodeLoginOk(account.value()));

    std::vector<std::string> characterLines = PacketCodec::EncodeCharacterList(account->characters);
    responses.insert(responses.end(), characterLines.begin(), characterLines.end());
    return responses;
}

std::vector<std::string> AuthService::HandleRegister(const std::string& loginId, const std::string& password)
{
    // 회원가입은 Repository에 위임합니다.
    // 중복 계정이거나 입력값이 유효하지 않으면 실패 응답을 반환합니다.
    std::vector<std::string> responses;
    const std::optional<AccountData> account = accountRepository_->RegisterAccount(loginId, password);
    if (!account.has_value())
    {
        responses.emplace_back(PacketCodec::EncodeRegisterFail("DuplicatedOrInvalidAccount"));
        return responses;
    }

    responses.emplace_back(PacketCodec::EncodeRegisterOk(account.value()));

    std::vector<std::string> characterLines = PacketCodec::EncodeCharacterList(account->characters);
    responses.insert(responses.end(), characterLines.begin(), characterLines.end());
    return responses;
}

std::string AuthService::HandleEnterGame(std::int32_t characterId)
{
    // 캐릭터 선택 후 게임 씬에 들어갈 때 호출됩니다.
    // 캐릭터 상세 정보와 스킬 슬롯을 DB에서 다시 조회합니다.
    const std::optional<CharacterData> character = accountRepository_->FindCharacterById(characterId);
    if (!character.has_value())
    {
        return PacketCodec::EncodeEnterGameFail("CharacterNotFound");
    }

    return PacketCodec::EncodeEnterGameOk(character.value());
}

std::string AuthService::HandleCreateCharacter(std::int32_t accountId, std::int32_t slotIndex, ClassType classType)
{
    // 캐릭터 생성 규칙은 Repository/DB 계층에서 검증합니다.
    // 현재는 슬롯 0~2, 계정당 최대 3개 캐릭터를 기준으로 처리합니다.
    const std::optional<CharacterData> character = accountRepository_->CreateCharacter(accountId, slotIndex, classType);
    if (!character.has_value())
    {
        return PacketCodec::EncodeCreateCharacterFail("CreateCharacterFailed");
    }

    return PacketCodec::EncodeCreateCharacterOk(character.value());
}

std::string AuthService::HandleDeleteCharacter(std::int32_t accountId, std::int32_t characterId)
{
    // 클라이언트가 임의 캐릭터를 지우지 못하도록 accountId와 characterId를 함께 넘깁니다.
    if (!accountRepository_->DeleteCharacter(accountId, characterId))
    {
        return PacketCodec::EncodeDeleteCharacterFail(characterId, "DeleteCharacterFailed");
    }

    return PacketCodec::EncodeDeleteCharacterOk(characterId);
}
