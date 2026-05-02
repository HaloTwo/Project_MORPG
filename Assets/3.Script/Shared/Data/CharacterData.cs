using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterData
{
    public int CharacterId;
    public int AccountId;
    public string Name;
    public ClassType ClassType;
    public int Level;
    public int Exp;
    public int Gold;
    public int CurrentMapId;
    public Vector3 Position;
    public EquipmentData Equipment = new EquipmentData();
    public List<InventoryItemData> Inventory = new List<InventoryItemData>();
    public List<int> LearnedSkillIds = new List<int>();
    public int[] QuickSlotSkillIds = new int[3];

    // 현재 선택된 직업 이름을 로그나 UI에 보여줄 때 사용합니다.
    public string GetClassNameKr()
    {
        switch (ClassType)
        {
            case ClassType.Warrior:
                return "전사";
            case ClassType.Archer:
                return "궁수";
            case ClassType.Rogue:
                return "도적";
            default:
                return "없음";
        }
    }
}
