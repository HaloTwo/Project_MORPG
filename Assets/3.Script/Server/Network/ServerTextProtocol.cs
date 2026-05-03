using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class ServerTextProtocol
{
    private readonly List<CharacterData> pendingCharacters = new List<CharacterData>();
    private int currentAccountId;
    private bool receivingCharacterList;

    /// 서버로 보낼 패킷을 현재 C++ 서버가 이해하는 한 줄짜리 명령어로 바꿉니다.
    public string Encode(PacketBase packet)
    {
        switch (packet)
        {
            case LoginRequestPacket login:
                return $"LOGIN {login.LoginId} {login.AuthToken}";
            case RegisterRequestPacket register:
                return $"REGISTER {register.LoginId} {register.Password}";
            case CreateCharacterRequestPacket create:
                return $"CREATE_CHARACTER {create.AccountId} {create.SlotIndex} {create.ClassType}";
            case DeleteCharacterRequestPacket delete:
                return $"DELETE_CHARACTER {delete.AccountId} {delete.CharacterId}";
            case EnterGameRequestPacket enter:
                return $"ENTER_GAME {enter.CharacterId}";
            default:
                return string.Empty;
        }
    }

    /// 서버에서 받은 텍스트 응답 한 줄을 Unity 내부 패킷으로 변환합니다.
    public IEnumerable<PacketBase> DecodeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("WELCOME", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (line.StartsWith("LOGIN_OK", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            currentAccountId = ReadInt(values, "accountId");
            yield return new LoginResponsePacket(true, currentAccountId, ReadString(values, "message", "LoginSuccess"));
            yield break;
        }

        if (line.StartsWith("LOGIN_FAIL", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new LoginResponsePacket(false, 0, ReadString(values, "message", "LoginFailed"));
            yield break;
        }

        if (line.StartsWith("REGISTER_OK", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            currentAccountId = ReadInt(values, "accountId");
            yield return new RegisterResponsePacket(true, currentAccountId, ReadString(values, "message", "RegisterSuccess"));
            yield break;
        }

        if (line.StartsWith("REGISTER_FAIL", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new RegisterResponsePacket(false, 0, ReadString(values, "message", "RegisterFailed"));
            yield break;
        }

        if (line.StartsWith("CHARACTER_LIST_END", StringComparison.OrdinalIgnoreCase))
        {
            receivingCharacterList = false;
            yield return new CharacterListPacket(currentAccountId, new List<CharacterData>(pendingCharacters));
            pendingCharacters.Clear();
            yield break;
        }

        if (line.StartsWith("CHARACTER_LIST", StringComparison.OrdinalIgnoreCase))
        {
            receivingCharacterList = true;
            pendingCharacters.Clear();
            yield break;
        }

        if (line.StartsWith("CREATE_CHARACTER_OK", StringComparison.OrdinalIgnoreCase))
        {
            CharacterData character = ParseCharacter(line.Replace("CREATE_CHARACTER_OK ", string.Empty));
            yield return new CreateCharacterResponsePacket(true, character, "CreateCharacterSuccess");
            yield break;
        }

        if (line.StartsWith("CREATE_CHARACTER_FAIL", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new CreateCharacterResponsePacket(false, null, ReadString(values, "message", "CreateCharacterFailed"));
            yield break;
        }

        if (line.StartsWith("DELETE_CHARACTER_OK", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new DeleteCharacterResponsePacket(true, ReadInt(values, "characterId"), ReadString(values, "message", "DeleteCharacterSuccess"));
            yield break;
        }

        if (line.StartsWith("DELETE_CHARACTER_FAIL", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new DeleteCharacterResponsePacket(false, ReadInt(values, "characterId"), ReadString(values, "message", "DeleteCharacterFailed"));
            yield break;
        }

        if (line.StartsWith("ENTER_GAME_OK", StringComparison.OrdinalIgnoreCase))
        {
            CharacterData character = ParseCharacter(line.Replace("ENTER_GAME_OK ", string.Empty));
            yield return new EnterGameResponsePacket(true, character, "EnterGameSuccess");
            yield break;
        }

        if (line.StartsWith("ENTER_GAME_FAIL", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> values = ParseValues(line);
            yield return new EnterGameResponsePacket(false, null, ReadString(values, "message", "EnterGameFailed"));
            yield break;
        }

        if (receivingCharacterList && line.StartsWith("CHARACTER", StringComparison.OrdinalIgnoreCase))
        {
            pendingCharacters.Add(ParseCharacter(line));
        }
    }

    private CharacterData ParseCharacter(string line)
    {
        Dictionary<string, string> values = ParseValues(line);
        CharacterData character = new CharacterData
        {
            CharacterId = ReadInt(values, "id"),
            AccountId = currentAccountId,
            SlotIndex = ReadInt(values, "slot"),
            Name = ReadString(values, "name", "Character"),
            ClassType = ReadClassType(values),
            Level = ReadInt(values, "level", 1),
            Gold = ReadInt(values, "gold", 100),
            Position = ReadPosition(values)
        };

        character.QuickSlotSkillIds = ReadSkillIds(values);
        return character;
    }

    private Dictionary<string, string> ParseValues(string line)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            int separator = token.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            values[token.Substring(0, separator)] = token.Substring(separator + 1);
        }

        return values;
    }

    private ClassType ReadClassType(Dictionary<string, string> values)
    {
        string className = ReadString(values, "class", "None");
        return Enum.TryParse(className, out ClassType classType) ? classType : ClassType.None;
    }

    private Vector3 ReadPosition(Dictionary<string, string> values)
    {
        string rawPosition = ReadString(values, "pos", "0,1,0");
        string[] parts = rawPosition.Split(',');
        if (parts.Length != 3)
        {
            return new Vector3(0.0f, 1.0f, 0.0f);
        }

        return new Vector3(ReadFloat(parts[0]), ReadFloat(parts[1], 1.0f), ReadFloat(parts[2]));
    }

    private int[] ReadSkillIds(Dictionary<string, string> values)
    {
        int[] skillIds = new int[3];
        string rawSkills = ReadString(values, "skills", string.Empty);
        string[] parts = rawSkills.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < skillIds.Length && i < parts.Length; i++)
        {
            skillIds[i] = ReadInt(parts[i]);
        }

        return skillIds;
    }

    private string ReadString(Dictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out string value) ? value : fallback;
    }

    private int ReadInt(Dictionary<string, string> values, string key, int fallback = 0)
    {
        return values.TryGetValue(key, out string value) ? ReadInt(value, fallback) : fallback;
    }

    private int ReadInt(string value, int fallback = 0)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
    }

    private float ReadFloat(string value, float fallback = 0.0f)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : fallback;
    }
}
