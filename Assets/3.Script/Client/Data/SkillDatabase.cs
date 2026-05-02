using System.Collections.Generic;

public static class SkillDatabase
{
    private static readonly Dictionary<int, SkillData> skills = new Dictionary<int, SkillData>
    {
        { 1001, new SkillData { SkillId = 1001, ClassType = ClassType.Warrior, Name = "Slash", Description = "전사의 기본 베기", Cooldown = 1.0f, Range = 2.0f, Damage = 15 } },
        { 1002, new SkillData { SkillId = 1002, ClassType = ClassType.Warrior, Name = "Shield Bash", Description = "방패로 적을 밀칩니다", Cooldown = 4.0f, Range = 1.8f, Damage = 25 } },
        { 1003, new SkillData { SkillId = 1003, ClassType = ClassType.Warrior, Name = "Whirlwind", Description = "주변을 회전 공격합니다", Cooldown = 8.0f, Range = 3.0f, Damage = 35 } },

        { 2001, new SkillData { SkillId = 2001, ClassType = ClassType.Archer, Name = "Arrow Shot", Description = "궁수의 기본 사격", Cooldown = 0.8f, Range = 9.0f, Damage = 12 } },
        { 2002, new SkillData { SkillId = 2002, ClassType = ClassType.Archer, Name = "Power Shot", Description = "강한 화살을 발사합니다", Cooldown = 4.5f, Range = 10.0f, Damage = 30 } },
        { 2003, new SkillData { SkillId = 2003, ClassType = ClassType.Archer, Name = "Rain of Arrows", Description = "범위에 화살비를 내립니다", Cooldown = 9.0f, Range = 8.0f, Damage = 32 } },

        { 3001, new SkillData { SkillId = 3001, ClassType = ClassType.Rogue, Name = "Stab", Description = "도적의 빠른 찌르기", Cooldown = 0.7f, Range = 1.7f, Damage = 13 } },
        { 3002, new SkillData { SkillId = 3002, ClassType = ClassType.Rogue, Name = "Dash Attack", Description = "전방으로 돌진 공격합니다", Cooldown = 5.0f, Range = 5.0f, Damage = 28 } },
        { 3003, new SkillData { SkillId = 3003, ClassType = ClassType.Rogue, Name = "Backstab", Description = "강한 치명 공격을 가합니다", Cooldown = 7.0f, Range = 1.5f, Damage = 42 } }
    };

    // skillId로 스킬 테이블 데이터를 찾습니다.
    public static bool TryGetSkill(int skillId, out SkillData skillData)
    {
        return skills.TryGetValue(skillId, out skillData);
    }

    // 직업에 맞는 기본 스킬 3개를 퀵슬롯에 넣기 위해 반환합니다.
    public static int[] GetDefaultSkillIds(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Warrior:
                return new[] { 1001, 1002, 1003 };
            case ClassType.Archer:
                return new[] { 2001, 2002, 2003 };
            case ClassType.Rogue:
                return new[] { 3001, 3002, 3003 };
            default:
                return new[] { 0, 0, 0 };
        }
    }
}
