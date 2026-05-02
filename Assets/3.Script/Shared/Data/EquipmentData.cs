using System.Collections.Generic;

public sealed class EquipmentData
{
    private readonly Dictionary<EquipSlot, long> equippedItemUids = new Dictionary<EquipSlot, long>();

    // 장비 슬롯에 실제 소유 아이템 UID를 장착합니다.
    public void Equip(EquipSlot slot, long itemUid)
    {
        if (slot == EquipSlot.None)
        {
            return;
        }

        equippedItemUids[slot] = itemUid;
    }

    // 특정 슬롯에 장착된 아이템 UID를 가져옵니다.
    public long GetEquippedItemUid(EquipSlot slot)
    {
        return equippedItemUids.TryGetValue(slot, out long itemUid) ? itemUid : 0;
    }

    // UI 표시나 저장 처리를 위해 현재 장비 목록을 읽습니다.
    public IReadOnlyDictionary<EquipSlot, long> GetAllEquippedItems()
    {
        return equippedItemUids;
    }
}
