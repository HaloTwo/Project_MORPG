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
    CONSTRAINT fk_inventory_items_character_id
        FOREIGN KEY (character_id)
        REFERENCES characters (character_id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS equipment
(
    character_id INT NOT NULL,
    equip_slot TINYINT NOT NULL,
    item_uid BIGINT NOT NULL,
    PRIMARY KEY (character_id, equip_slot),
    CONSTRAINT fk_equipment_character_id
        FOREIGN KEY (character_id)
        REFERENCES characters (character_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_equipment_item_uid
        FOREIGN KEY (item_uid)
        REFERENCES inventory_items (item_uid)
        ON DELETE CASCADE
);

INSERT INTO accounts (login_id, password_hash)
VALUES ('test_user', '1234')
ON DUPLICATE KEY UPDATE login_id = login_id;
