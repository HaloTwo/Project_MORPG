# Project MORPG C++ TCP Server

Unity 클라이언트의 로그인, 회원가입, 캐릭터 생성, 캐릭터 입장 요청을 처리하는 C++ TCP 서버입니다.

현재 구조는 아래 흐름을 기준으로 동작합니다.

```text
Unity Client
-> C++ TCP Server
-> MariaDB
```

Unity는 DB에 직접 접속하지 않습니다. Unity는 TCP 서버에 요청만 보내고, DB 조회와 저장은 서버가 담당합니다.

## 현재 지원 기능

- 로그인: `accounts` 테이블에서 아이디와 비밀번호 확인
- 회원가입: 중복 아이디 검사 후 새 계정 추가
- 캐릭터 목록: 로그인/회원가입 성공 후 해당 계정의 캐릭터 목록 반환
- 캐릭터 생성: 계정당 최대 3개, 슬롯 0~2, 직업 Warrior/Archer/Rogue 저장
- 캐릭터 입장: 캐릭터 ID로 상세 데이터 조회

## DB 스키마

초기 DB 생성 파일:

```text
Server_CPP/db/schema.sql
```

MariaDB 클라이언트에서 실행:

```sql
source C:/Users/user/Documents/GitHub/Project_MORPG/Server_CPP/db/schema.sql
```

확인:

```sql
USE project_morpg;
SHOW TABLES;
SELECT account_id, login_id, password_hash FROM accounts;
```

## 실행 방법

Visual Studio에서:

```text
Server_CPP/ProjectMORPGServer.sln
```

설정:

```text
Configuration: Debug
Platform: x64
```

빌드 후 실행 파일:

```text
Server_CPP/x64/Debug/ProjectMORPGServer.exe
```

기본 포트는 `7777`입니다.

DB root 비밀번호를 실행 인자로 넘길 수 있습니다.

```powershell
cd C:/Users/user/Documents/GitHub/Project_MORPG/Server_CPP/x64/Debug
./ProjectMORPGServer.exe 7777 DB_ROOT_PASSWORD
```

예를 들어 MariaDB root 비밀번호를 실행 인자로 넘기려면:

```powershell
./ProjectMORPGServer.exe 7777 DB_ROOT_PASSWORD
```

환경 변수로도 DB 접속 정보를 바꿀 수 있습니다.

```powershell
$env:MORPG_DB_HOST="127.0.0.1"
$env:MORPG_DB_USER="root"
$env:MORPG_DB_PASSWORD="DB_ROOT_PASSWORD"
$env:MORPG_DB_NAME="project_morpg"
./ProjectMORPGServer.exe
```

정상 실행 로그:

```text
[Server] Listening on port 7777
```

## 테스트 명령

TCP로 서버에 연결하면 아래 텍스트 명령을 보낼 수 있습니다.

```text
REGISTER new_user 1234
LOGIN test_user 1234
CREATE_CHARACTER 1 0 Warrior
ENTER_GAME 1
PING
QUIT
```

응답 예시:

```text
LOGIN_OK accountId=1 message=LoginSuccess
CHARACTER_LIST count=0
CHARACTER_LIST_END
CREATE_CHARACTER_OK CHARACTER id=1 name=Warrior_1 class=Warrior level=1 gold=100 pos=-2,1,0 skills=1001,1002,1003
```

## Unity 연결

Unity의 `NetworkManager`는 기본값으로 실제 TCP 서버를 사용하도록 변경되어 있습니다.

```text
Host: 127.0.0.1
Port: 7777
```

테스트 순서:

1. MariaDB 실행 상태 확인
2. C++ 서버 실행
3. Unity 실행
4. 로그인 화면에서 `test_user / 1234` 입력
5. 캐릭터 선택 화면에서 빈 슬롯에 직업 선택
6. DBeaver의 `characters` 테이블에서 생성 데이터 확인

## 주의

- `accounts.password_hash`는 현재 공부용으로 평문 비밀번호를 저장합니다.
- 실서비스 구조에서는 반드시 BCrypt/Argon2 같은 해시 알고리즘을 사용해야 합니다.
- 서버의 DB 접속 비밀번호는 “게임 로그인 비밀번호”가 아니라 “MariaDB root 비밀번호”입니다.
