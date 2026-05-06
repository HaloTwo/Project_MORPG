CREATE DATABASE IF NOT EXISTS project_morpg
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE project_morpg;

CREATE TABLE IF NOT EXISTS accounts
(
    account_id INT NOT NULL AUTO_INCREMENT,
    login_id VARCHAR(32) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at DATETIME NULL,
    PRIMARY KEY (account_id),
    UNIQUE KEY uq_accounts_login_id (login_id)
);

CREATE TABLE IF NOT EXISTS characters
(
    character_id INT NOT NULL AUTO_INCREMENT,
    account_id INT NOT NULL,
    slot_index TINYINT NOT NULL,
    name VARCHAR(32) NOT NULL,
    class_type TINYINT NOT NULL,
    level INT NOT NULL DEFAULT 1,
    exp INT NOT NULL DEFAULT 0,
    gold INT NOT NULL DEFAULT 100,
    current_map_id INT NOT NULL DEFAULT 1,
    pos_x FLOAT NOT NULL DEFAULT 0,
    pos_y FLOAT NOT NULL DEFAULT 1,
    pos_z FLOAT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (character_id),
    UNIQUE KEY uq_characters_account_slot (account_id, slot_index),
    KEY ix_characters_account_id (account_id),
    CONSTRAINT fk_characters_account_id
        FOREIGN KEY (account_id)
        REFERENCES accounts (account_id)
        ON DELETE CASCADE,
    CONSTRAINT chk_characters_slot_index
        CHECK (slot_index BETWEEN 0 AND 2),
    CONSTRAINT chk_characters_class_type
        CHECK (class_type IN (1, 2, 3))
);

CREATE TABLE IF NOT EXISTS character_skills
(
    character_id INT NOT NULL,
    slot_index TINYINT NOT NULL,
    skill_id INT NOT NULL,
    PRIMARY KEY (character_id, slot_index),
    CONSTRAINT fk_character_skills_character_id
        FOREIGN KEY (character_id)
        REFERENCES characters (character_id)
        ON DELETE CASCADE,
    CONSTRAINT chk_character_skills_slot_index
        CHECK (slot_index BETWEEN 0 AND 2)
);

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

CREATE TABLE IF NOT EXISTS inventory_items
(
    item_uid BIGINT NOT NULL AUTO_INCREMENT,
    character_id INT NOT NULL,
    item_id INT NOT NULL,
    item_type TINYINT NOT NULL,
    count INT NOT NULL DEFAULT 1,
    slot_index INT NOT NULL,
    enhancement_level INT NOT NULL DEFAULT 0,
    PRIMARY KEY (item_uid),
    KEY ix_inventory_items_character_id (character_id),
    UNIQUE KEY uq_inventory_items_character_slot (character_id, slot_index),
    CONSTRAINT fk_inventory_items_character_id
        FOREIGN KEY (character_id)
        REFERENCES characters (character_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_inventory_items_item_id
        FOREIGN KEY (item_id)
        REFERENCES item_master (item_id),
    CONSTRAINT chk_inventory_items_count
        CHECK (count > 0),
    CONSTRAINT chk_inventory_items_slot_index
        CHECK (slot_index BETWEEN 0 AND 19),
    CONSTRAINT chk_inventory_items_enhancement_level
        CHECK (enhancement_level >= 0)
);

CREATE TABLE IF NOT EXISTS equipment
(
    character_id INT NOT NULL,
    equip_slot TINYINT NOT NULL,
    item_uid BIGINT NOT NULL,
    PRIMARY KEY (character_id, equip_slot),
    UNIQUE KEY uq_equipment_item_uid (item_uid),
    CONSTRAINT fk_equipment_character_id
        FOREIGN KEY (character_id)
        REFERENCES characters (character_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_equipment_item_uid
        FOREIGN KEY (item_uid)
        REFERENCES inventory_items (item_uid)
        ON DELETE CASCADE
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

INSERT INTO accounts (login_id, password_hash)
VALUES ('test_user', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4')
ON DUPLICATE KEY UPDATE login_id = login_id;
