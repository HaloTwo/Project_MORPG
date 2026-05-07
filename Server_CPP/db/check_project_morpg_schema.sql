USE project_morpg;

-- Schema check script for the current C++ server.
-- Run this in DBeaver. If tables/columns/items are missing, apply schema.sql again.

SHOW TABLES;

DESCRIBE accounts;
DESCRIBE characters;
DESCRIBE character_skills;
DESCRIBE item_master;
DESCRIBE inventory_items;
DESCRIBE equipment;

SELECT item_id, item_type, name, equip_slot, base_attack, base_defense, max_stack
FROM item_master
ORDER BY item_id;

SELECT
    character_id,
    account_id,
    slot_index,
    name,
    class_type,
    level,
    gold
FROM characters
ORDER BY character_id DESC
LIMIT 10;

SELECT
    item_uid,
    character_id,
    item_id,
    item_type,
    count,
    slot_index,
    enhancement_level
FROM inventory_items
ORDER BY item_uid DESC
LIMIT 20;

SELECT
    character_id,
    equip_slot,
    item_uid
FROM equipment
ORDER BY character_id DESC, equip_slot;
