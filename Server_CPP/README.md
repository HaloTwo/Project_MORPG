# Project MORPG C++ TCP Server

Unity 클라이언트의 로그인, 회원가입, 캐릭터 목록, 생성, 삭제, 입장, 실시간 이동/채팅 요청을 처리하는 C++ TCP 서버입니다.

```text
Unity Client
-> C++ TCP Server
-> MariaDB
```

Unity는 DB에 직접 접근하지 않습니다. 클라이언트는 TCP 명령만 보내고, 계정 검증과 캐릭터 저장은 서버가 MariaDB를 통해 처리합니다.

## 현재 지원 기능

- 로그인: `accounts.login_id` 조회 후 비밀번호 해시 검증
- 회원가입: 신규 계정 생성 및 `password_hash` 저장
- 기존 평문 테스트 계정은 첫 로그인 성공 시 해시 값으로 자동 전환
- 캐릭터 목록: 계정 소유 캐릭터 목록 반환
- 캐릭터 생성: 계정당 최대 3개, 슬롯 0~2, 이름/직업 검증 후 저장
- 캐릭터 삭제: `account_id + character_id` 조건으로 소유권 확인 후 삭제
- 캐릭터 입장: 선택 캐릭터 상세 데이터 반환
- 실시간 패킷: `SPAWN`, `MOVE`, `STOP`, `DESPAWN`, `CHAT` 브로드캐스트

## 주요 구조

```text
ClientSession
-> AuthService
-> IAccountRepository
-> MariaDbAccountRepository
-> MariaDB
```

- `ClientSession`: TCP 라인 명령 수신과 세션별 실시간 브로드캐스트 처리
- `AuthService`: 로그인, 회원가입, 캐릭터 생성/삭제 흐름 제어
- `IAccountRepository`: 저장소 인터페이스
- `MariaDbAccountRepository`: MariaDB C API 기반 계정/캐릭터 저장 구현

## DB 스키마

초기 DB 생성 파일:

```text
Server_CPP/db/schema.sql
```

확인용 SQL:

```sql
USE project_morpg;
SHOW TABLES;
SELECT account_id, login_id, password_hash FROM accounts;
SELECT character_id, account_id, slot_index, name, class_type FROM characters;
```

## 텍스트 명령 예시

```text
REGISTER new_user 1234
LOGIN test_user 1234
CREATE_CHARACTER 1 0 Warrior Sumin
ENTER_GAME 1
CHAT 1 senderBase64 messageBase64
PING
QUIT
```

응답 예시:

```text
LOGIN_OK accountId=1 message=LoginSuccess
CHARACTER_LIST count=1
CHARACTER id=1 slot=0 name=Sumin class=Warrior level=1 gold=100 pos=-2,1,0 skills=1001,1002,1003
CHARACTER_LIST_END
```

## 주의

- DB 접속 비밀번호는 게임 계정 비밀번호가 아니라 MariaDB 접속 비밀번호입니다.
- `accounts.password_hash`에는 원문 비밀번호가 아니라 서버에서 계산한 SHA-256 해시가 저장됩니다.
- 현재 SHA-256은 학습용 1차 개선입니다. 운영 수준에서는 salt 기반 PBKDF2 / bcrypt / Argon2 구조로 교체해야 합니다.
- MariaDB 포트 `3306`은 외부 공개하지 않고, 외부 테스트가 필요할 때는 게임 서버 TCP 포트만 열어야 합니다.
