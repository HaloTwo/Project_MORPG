#include "protocol/PacketCodec.h"

#include <sstream>

ClientCommand PacketCodec::DecodeClientCommand(const std::string& line)
{
    std::istringstream stream(line);
    ClientCommand command;
    stream >> command.name;

    std::string arg;
    while (stream >> arg)
    {
        command.args.push_back(arg);
    }

    return command;
}

std::string PacketCodec::EncodeLoginOk(const AccountData& account)
{
    std::ostringstream stream;
    stream << "LOGIN_OK accountId=" << account.accountId << " message=LoginSuccess";
    return stream.str();
}

std::string PacketCodec::EncodeLoginFail(const std::string& message)
{
    return "LOGIN_FAIL message=" + message;
}

std::string PacketCodec::EncodeRegisterOk(const AccountData& account)
{
    std::ostringstream stream;
    stream << "REGISTER_OK accountId=" << account.accountId << " message=RegisterSuccess";
    return stream.str();
}

std::string PacketCodec::EncodeRegisterFail(const std::string& message)
{
    return "REGISTER_FAIL message=" + message;
}

std::vector<std::string> PacketCodec::EncodeCharacterList(const std::vector<CharacterData>& characters)
{
    std::vector<std::string> lines;
    lines.emplace_back("CHARACTER_LIST count=" + std::to_string(characters.size()));

    for (const CharacterData& character : characters)
    {
        lines.emplace_back(EncodeCharacter(character));
    }

    lines.emplace_back("CHARACTER_LIST_END");
    return lines;
}

std::string PacketCodec::EncodeEnterGameOk(const CharacterData& character)
{
    return "ENTER_GAME_OK " + EncodeCharacter(character);
}

std::string PacketCodec::EncodeEnterGameFail(const std::string& message)
{
    return "ENTER_GAME_FAIL message=" + message;
}

std::string PacketCodec::EncodeCreateCharacterOk(const CharacterData& character)
{
    return "CREATE_CHARACTER_OK " + EncodeCharacter(character);
}

std::string PacketCodec::EncodeCreateCharacterFail(const std::string& message)
{
    return "CREATE_CHARACTER_FAIL message=" + message;
}

std::string PacketCodec::EncodeCharacter(const CharacterData& character)
{
    std::ostringstream stream;
    stream << "CHARACTER"
        << " id=" << character.characterId
        << " name=" << character.name
        << " class=" << ToString(character.classType)
        << " level=" << character.level
        << " gold=" << character.gold
        << " pos=" << character.posX << "," << character.posY << "," << character.posZ
        << " skills=" << JoinSkillIds(character.quickSlotSkillIds);
    return stream.str();
}

std::string PacketCodec::JoinSkillIds(const std::vector<std::int32_t>& skillIds)
{
    std::ostringstream stream;
    for (std::size_t i = 0; i < skillIds.size(); ++i)
    {
        if (i > 0)
        {
            stream << ",";
        }

        stream << skillIds[i];
    }

    return stream.str();
}
