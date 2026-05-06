USE project_morpg;

CREATE TABLE IF NOT EXISTS item_master
(
    item_id INT NOT NULL,
    item_type TINYINT NOT NULL,
    name VARCHAR(64) NOT NULL,
    equip_slot TINYINT NULL,
    base_attack INT NOT NULL DEFAULT 0,
    base_defense INT NOT NULL DEFAULT 0,
    max_stack INT NOT NULL DEFAULT 1,
    PRIMARY KEY (item_id),
    CONSTRAINT chk_item_master_item_type
        CHECK (item_type IN (1, 2, 3, 4)),
    CONSTRAINT chk_item_master_max_stack
        CHECK (max_stack >= 1)
);

INSERT INTO item_master (item_id, item_type, name, equip_slot, base_attack, base_defense, max_stack)
VALUES
    (1001, 1, 'Beginner Sword', 1, 5, 0, 1),
    (1002, 1, 'Beginner Bow', 1, 4, 0, 1),
    (1003, 1, 'Beginner Dagger', 1, 4, 0, 1),
    (2001, 2, 'Cloth Armor', 3, 0, 2, 1),
    (3001, 3, 'Small Potion', NULL, 0, 0, 20)
ON DUPLICATE KEY UPDATE
    item_type = VALUES(item_type),
    name = VALUES(name),
    equip_slot = VALUES(equip_slot),
    base_attack = VALUES(base_attack),
    base_defense = VALUES(base_defense),
    max_stack = VALUES(max_stack);

-- 이미 만들어진 로컬 DB에 적용하는 1회성 마이그레이션입니다.
-- 아래 ALTER는 같은 제약이 이미 있으면 실패할 수 있으므로, DBeaver에서 실행 전 현재 Keys/Foreign Keys를 확인합니다.
ALTER TABLE inventory_items
    ADD UNIQUE KEY uq_inventory_items_character_slot (character_id, slot_index);

ALTER TABLE inventory_items
    ADD CONSTRAINT fk_inventory_items_item_id
        FOREIGN KEY (item_id)
        REFERENCES item_master (item_id);

ALTER TABLE equipment
    ADD UNIQUE KEY uq_equipment_item_uid (item_uid);
