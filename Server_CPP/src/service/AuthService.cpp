#include "service/AuthService.h"

#include "protocol/PacketCodec.h"

#include <windows.h>
#include <bcrypt.h>

#include <iomanip>
#include <sstream>
#include <stdexcept>
#include <utility>
#include <vector>

AuthService::AuthService(std::shared_ptr<IAccountRepository> accountRepository)
    : accountRepository_(std::move(accountRepository))
{
}

std::vector<std::string> AuthService::HandleLogin(const std::string& loginId, const std::string& authToken)
{
    // 로그인 요청의 중심 흐름입니다.
    // Repository가 DB에서 계정과 캐릭터 목록을 가져오고, Service가 응답 패킷 순서를 구성합니다.
    std::vector<std::string> responses;
    const std::string passwordHash = HashPassword(authToken);
    std::optional<AccountData> account = accountRepository_->FindAccountByLogin(loginId, passwordHash);
    if (!account.has_value())
    {
        account = accountRepository_->FindAccountByLogin(loginId, authToken);
        if (account.has_value())
        {
            accountRepository_->UpdatePasswordHash(loginId, passwordHash);
        }
    }

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
    const std::optional<AccountData> account = accountRepository_->RegisterAccount(loginId, HashPassword(password));
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

std::string AuthService::HandleCreateCharacter(std::int32_t accountId, std::int32_t slotIndex, ClassType classType, const std::string& characterName)
{
    // 캐릭터 생성 규칙은 Repository/DB 계층에서 검증합니다.
    // 현재는 슬롯 0~2, 계정당 최대 3개 캐릭터를 기준으로 처리합니다.
    const std::optional<CharacterData> character = accountRepository_->CreateCharacter(accountId, slotIndex, classType, characterName);
    if (!character.has_value())
    {
        return PacketCodec::EncodeCreateCharacterFail("CreateCharacterFailed");
    }

    return PacketCodec::EncodeCreateCharacterOk(character.value());
}

std::string AuthService::HashPassword(const std::string& password) const
{
    BCRYPT_ALG_HANDLE algorithm = nullptr;
    BCRYPT_HASH_HANDLE hash = nullptr;
    DWORD objectLength = 0;
    DWORD dataLength = 0;
    DWORD hashLength = 0;

    if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_SHA256_ALGORITHM, nullptr, 0) != 0)
    {
        throw std::runtime_error("BCryptOpenAlgorithmProvider failed");
    }

    if (BCryptGetProperty(algorithm, BCRYPT_OBJECT_LENGTH, reinterpret_cast<PUCHAR>(&objectLength), sizeof(objectLength), &dataLength, 0) != 0 ||
        BCryptGetProperty(algorithm, BCRYPT_HASH_LENGTH, reinterpret_cast<PUCHAR>(&hashLength), sizeof(hashLength), &dataLength, 0) != 0)
    {
        BCryptCloseAlgorithmProvider(algorithm, 0);
        throw std::runtime_error("BCryptGetProperty failed");
    }

    std::vector<unsigned char> objectBuffer(objectLength);
    std::vector<unsigned char> hashBuffer(hashLength);
    if (BCryptCreateHash(algorithm, &hash, objectBuffer.data(), objectLength, nullptr, 0, 0) != 0 ||
        BCryptHashData(hash, reinterpret_cast<PUCHAR>(const_cast<char*>(password.data())), static_cast<ULONG>(password.size()), 0) != 0 ||
        BCryptFinishHash(hash, hashBuffer.data(), hashLength, 0) != 0)
    {
        if (hash != nullptr)
        {
            BCryptDestroyHash(hash);
        }

        BCryptCloseAlgorithmProvider(algorithm, 0);
        throw std::runtime_error("BCrypt hash failed");
    }

    BCryptDestroyHash(hash);
    BCryptCloseAlgorithmProvider(algorithm, 0);

    std::ostringstream stream;
    for (unsigned char value : hashBuffer)
    {
        stream << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(value);
    }

    return stream.str();
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
