# Project MORPG C++ Server Prototype

Unity 클라이언트와 연동하기 위한 C++ 서버 프로토타입입니다.

현재 단계에서는 MariaDB를 바로 연결하지 않고, `MockAccountRepository`로 로그인 / 캐릭터 목록 응답을 테스트합니다.  
이후 `IAccountRepository` 구현체를 MariaDB 기반으로 교체하면 같은 서버 흐름에서 DB 조회로 확장할 수 있습니다.

## 목표 구조

```text
Unity Client
→ C++ TCP Server
→ AccountRepository
→ Mock Data 또는 MariaDB
```

## 현재 지원 명령

클라이언트는 줄 단위 텍스트 패킷을 보냅니다.

```text
LOGIN test_user mock-token
ENTER_GAME 101
PING
QUIT
```

서버 응답 예시:

```text
LOGIN_OK accountId=1 message=MockLoginSuccess
CHARACTER_LIST count=3
CHARACTER id=101 name=Leon class=Warrior level=1 gold=100 pos=-2,1,0 skills=1001,1002,1003
CHARACTER id=102 name=Rena class=Archer level=1 gold=100 pos=0,1,0 skills=2001,2002,2003
CHARACTER id=103 name=Kain class=Rogue level=1 gold=100 pos=2,1,0 skills=3001,3002,3003
CHARACTER_LIST_END
```

## 빌드

### Visual Studio에서 실행

가장 쉬운 방법은 서버 전용 솔루션을 여는 것입니다.

```text
Server_CPP/ProjectMORPGServer.sln
```

Visual Studio에서 위 솔루션을 열고:

```text
구성: Debug
플랫폼: x64
시작 프로젝트: ProjectMORPGServer
```

로 맞춘 뒤 `F5` 또는 `Ctrl + F5`로 실행합니다.

정상 실행되면 콘솔에 아래 로그가 출력됩니다.

```text
[Server] Listening on port 7777
```

### CMake로 빌드

Windows PowerShell 기준:

```powershell
cd Server_CPP
cmake -S . -B build
cmake --build build --config Debug
```

실행:

```powershell
.\build\Debug\ProjectMORPGServer.exe
```

기본 포트는 `7777`입니다.

## 다음 단계

- Unity `NetworkManager`의 TODO 영역을 실제 TCP Socket 송신으로 교체
- `MockAccountRepository` 대신 `MariaDbAccountRepository` 구현
- 바이너리 패킷 또는 Protobuf 기반 직렬화 구조로 교체
- IOCP 기반 비동기 세션 처리로 확장
