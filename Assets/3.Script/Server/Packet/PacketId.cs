public enum PacketId
{
    None = 0,

    LoginRequest = 101,
    LoginResponse = 102,
    CharacterList = 103,
    EnterGameRequest = 104,
    EnterGameResponse = 105,

    Move = 1001,
    Stop = 1002,
    Skill = 2001,
    Damage = 3001,
    Spawn = 4001,
    Despawn = 4002
}
