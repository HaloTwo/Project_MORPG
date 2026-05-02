# Unity 6 3D Quarter-View MORPG

Unity 6 기반 3D 쿼터뷰 MORPG 클라이언트와 C++ TCP 서버 프로토타입을 함께 구성한 개인 프로젝트입니다.

현재 Unity 클라이언트는 로그인, 캐릭터 선택, 로딩, 게임 진입, 쿼터뷰 이동, 가상 조이스틱, 패킷 분배 구조를 갖추고 있습니다. 서버 쪽은 `Server_CPP`에 Winsock 기반 C++ TCP 서버 프로토타입을 별도로 두었고, 로그인 / 캐릭터 목록 / 게임 입장 요청을 Mock Repository 데이터로 처리합니다.

최종 목표는 Unity 클라이언트가 직접 데이터를 판단하지 않고 서버에 요청을 보내며, C++ 서버가 계정, 캐릭터, 이동, 스킬, 전투, 저장 처리를 담당하는 Server-Driven 구조입니다.

```text
Unity Client
-> C++ TCP Server
-> Repository Layer
-> Mock Data / MariaDB
```

## Project Info

- 개발 인원: 1인
- 개발 기간: 2026.05 ~ 진행 중
- 클라이언트: Unity 6, C#
- 서버: C++17, Winsock TCP, CMake / Visual Studio
- 렌더링: URP
- UI: Unity uGUI
- 입력: Unity Input System, Virtual Joystick
- 현재 서버 상태: Blocking TCP + ClientSession별 thread 방식 프로토타입
- 확장 목표: IOCP 기반 비동기 서버, MariaDB 연동, 바이너리 패킷 직렬화, 서버 권위 구조

## Current Features

- Unity 6 프로젝트 구조 정리
- LoginScene / LoadingScene / CharacterSelectScene / GameScene 분리
- Runtime uGUI 기반 로그인, 캐릭터 선택, 게임 HUD 생성
- Mock 서버 시뮬레이터 기반 로그인 흐름 구현
- 계정 / 캐릭터 / 직업 / 장비 / 인벤토리 / 스킬 데이터 구조 분리
- Warrior / Archer / Rogue 캐릭터 데이터 구성
- CharacterController 기반 쿼터뷰 이동
- 가상 조이스틱 및 에디터 테스트용 WASD / 방향키 입력 지원
- Move / Stop / Skill / Damage / Spawn 계열 패킷 타입 정의
- PacketQueue / PacketDispatcher 기반 수신 패킷 처리 흐름 분리
- 원격 플레이어 보간 이동을 위한 `RemotePlayerController` 준비
- C++ TCP 서버 프로토타입 추가
- C++ 서버에서 LOGIN / ENTER_GAME / PING / QUIT 명령 처리

## Scene Flow

로그인부터 게임 진입까지의 흐름을 실제 온라인 게임 구조처럼 씬 단위로 분리했습니다.

```text
LoginScene
-> LoadingScene
-> CharacterSelectScene
-> LoadingScene
-> GameScene
```

### Related Code

```text
Assets/3.Script/Client/Login/LoginSceneController.cs
Assets/3.Script/Client/Scene/LoadingSceneController.cs
Assets/3.Script/Client/Character/CharacterSelectSceneController.cs
Assets/3.Script/Client/Scene/SceneFlow.cs
Assets/3.Script/Client/Scene/SceneNames.cs
```

## Client Network Architecture

Unity 클라이언트는 네트워크 진입점을 `NetworkManager`로 모으고, 수신 패킷은 `PacketQueue`에 넣은 뒤 Unity 메인 스레드에서 `PacketDispatcher`가 이벤트로 분배합니다.

현재 기본 경로는 로컬 시뮬레이션입니다.

```text
Unity UI / Player Input
-> Packet 생성
-> NetworkManager.SendPacket()
-> MockServerSimulator
-> PacketQueue
-> PacketDispatcher
-> Scene / UI / Player system
```

실제 TCP 서버 연동 단계에서는 `NetworkManager.SendPacket()`의 TODO 영역을 Socket 송신 코드로 교체하고, 서버에서 받은 응답을 `PacketQueue`에 넣는 방식으로 확장할 예정입니다.

### Related Code

```text
Assets/3.Script/Server/Network/NetworkManager.cs
Assets/3.Script/Server/Network/PacketQueue.cs
Assets/3.Script/Server/Network/PacketDispatcher.cs
Assets/3.Script/Server/Network/MockServerSimulator.cs
Assets/3.Script/Server/Packet/PacketBase.cs
Assets/3.Script/Server/Packet/PacketId.cs
```

## C++ TCP Server Prototype

`Server_CPP`에는 Unity 클라이언트와 연결할 수 있는 C++ 서버 프로토타입이 들어 있습니다.

현재 서버는 Windows Winsock 기반 TCP 서버이며, 클라이언트 접속마다 `ClientSession`을 별도 thread로 실행합니다. 아직 IOCP 구조는 아니며, 구조 검증과 흐름 테스트를 위한 1차 서버입니다.

```text
Server_CPP
├─ CMakeLists.txt
├─ ProjectMORPGServer.vcxproj
└─ src
   ├─ main.cpp
   ├─ net
   │  ├─ TcpServer
   │  └─ ClientSession
   ├─ protocol
   │  └─ PacketCodec
   ├─ service
   │  └─ AuthService
   ├─ repository
   │  ├─ IAccountRepository
   │  └─ MockAccountRepository
   └─ domain
      ├─ AccountData
      └─ CharacterData
```

### Server Commands

```text
LOGIN test_user mock-token
ENTER_GAME 101
PING
QUIT
```

### Example Response

```text
WELCOME ProjectMORPGServer
LOGIN_OK accountId=1 message=MockLoginSuccess
CHARACTER_LIST count=3
CHARACTER id=101 name=Leon class=Warrior level=1 gold=100 pos=-2,1,0 skills=1001,1002,1003
CHARACTER id=102 name=Rena class=Archer level=1 gold=100 pos=0,1,0 skills=2001,2002,2003
CHARACTER id=103 name=Kain class=Rogue level=1 gold=100 pos=2,1,0 skills=3001,3002,3003
CHARACTER_LIST_END
```

### Build

Visual Studio에서는 `Server_CPP/ProjectMORPGServer.vcxproj`를 열어 x64 Debug로 실행할 수 있습니다.

CMake 기준:

```powershell
cd Server_CPP
cmake -S . -B build
cmake --build build --config Debug
.\build\Debug\ProjectMORPGServer.exe
```

기본 포트는 `7777`입니다. 실행 인자로 포트를 바꿀 수 있습니다.

```powershell
.\build\Debug\ProjectMORPGServer.exe 7777
```

## Local Server Simulation

Unity 프로젝트 내부에는 실제 TCP 연동 전에도 로그인과 캐릭터 선택 흐름을 테스트할 수 있도록 `MockServerSimulator`가 있습니다.

지원 흐름:

```text
LoginRequest
-> LoginResponse
-> CharacterList

EnterGameRequest
-> EnterGameResponse
```

임시 캐릭터:

```text
Leon / Warrior
Rena / Archer
Kain / Rogue
```

Mock 캐릭터에는 기본 위치, 골드, 장비, 소비 아이템, 직업별 기본 스킬이 포함됩니다.

## Character And Skill Data

온라인 RPG에서 서버와 DB로 넘겨야 할 데이터를 미리 분리했습니다.

```text
AccountData
CharacterData
EquipmentData
InventoryItemData
SkillData
ClassType
EquipSlot
ItemType
```

직업별 기본 스킬:

```text
Warrior
- Slash
- Shield Bash
- Whirlwind

Archer
- Arrow Shot
- Power Shot
- Rain of Arrows

Rogue
- Stab
- Dash Attack
- Backstab
```

### Related Code

```text
Assets/3.Script/Shared/Data/AccountData.cs
Assets/3.Script/Shared/Data/CharacterData.cs
Assets/3.Script/Shared/Data/EquipmentData.cs
Assets/3.Script/Shared/Data/InventoryItemData.cs
Assets/3.Script/Shared/Data/SkillData.cs
Assets/3.Script/Shared/Define/ClassType.cs
Assets/3.Script/Client/Data/SkillDatabase.cs
```

## Quarter-View Movement

모바일 MORPG 조작감을 목표로 가상 조이스틱 기반 이동을 구현했습니다. 에디터 테스트를 위해 키보드 입력도 함께 지원합니다.

- `CharacterController` 기반 이동
- 카메라 방향 기준 이동 벡터 변환
- 이동 시작 / 이동 중 / 정지 시점 감지
- 일정 간격으로 `MovePacket` 생성
- 정지 시 `StopPacket` 생성
- 선택된 캐릭터 데이터로 ActorId, 직업, 위치 초기화

### Related Code

```text
Assets/3.Script/Client/Player/QuarterViewPlayerController.cs
Assets/3.Script/Client/Player/RemotePlayerController.cs
Assets/3.Script/Client/UI/VirtualJoystick.cs
Assets/3.Script/Client/UI/GameHudController.cs
Assets/3.Script/Client/Camera/QuarterViewCameraController.cs
```

## Unity UI

초기 테스트용 `OnGUI` 방식 대신 런타임 uGUI 생성 구조로 화면을 구성했습니다.

- 로그인 화면
- 로딩 화면
- 캐릭터 선택 화면
- 게임 HUD
- 가상 조이스틱

### Related Code

```text
Assets/3.Script/Client/UI/RuntimeUiFactory.cs
Assets/3.Script/Client/UI/GameHudController.cs
Assets/3.Script/Client/UI/VirtualJoystick.cs
```

## Folder Structure

```text
Assets
├─ 1.Scene
│  ├─ LoginScene.unity
│  ├─ LoadingScene.unity
│  ├─ CharacterSelectScene.unity
│  └─ GameScene.unity
├─ 2.Model
├─ 3.Script
│  ├─ Client
│  │  ├─ Camera
│  │  ├─ Character
│  │  ├─ Combat
│  │  ├─ Core
│  │  ├─ Data
│  │  ├─ Login
│  │  ├─ Monster
│  │  ├─ Player
│  │  ├─ Scene
│  │  └─ UI
│  ├─ Server
│  │  ├─ Network
│  │  └─ Packet
│  └─ Shared
│     ├─ Data
│     ├─ Define
│     └─ Protocol
├─ 5.Animation
├─ 6.Materials
├─ 8.Audio
└─ 9.Font

Server_CPP
├─ CMakeLists.txt
├─ ProjectMORPGServer.vcxproj
└─ src
   ├─ domain
   ├─ net
   ├─ protocol
   ├─ repository
   └─ service
```

## Roadmap

### 1. Unity TCP Client 연결

`NetworkManager`의 Mock 송신 경로를 실제 TCP Socket 송수신 구조로 확장합니다.

```text
현재
Unity Client
-> MockServerSimulator

목표
Unity Client
-> C++ TCP Server
```

### 2. Packet Serialization

현재 C++ 서버 프로토타입은 텍스트 명령 기반입니다. 이후 Unity C# 패킷 구조와 맞춰 바이너리 직렬화 또는 Protobuf 기반 패킷으로 교체할 예정입니다.

### 3. IOCP Server

현재 서버는 blocking socket + session thread 방식입니다. 다음 단계에서는 IOCP 기반 비동기 세션 처리로 확장합니다.

### 4. MariaDB Integration

`IAccountRepository` 구현체를 `MockAccountRepository`에서 `MariaDbAccountRepository`로 교체해 실제 계정 / 캐릭터 / 장비 / 인벤토리 / 스킬 데이터를 DB에서 조회하고 저장하도록 확장합니다.

예상 테이블:

```text
accounts
characters
items
equipment
character_skills
```

### 5. Server Authority

클라이언트는 입력과 요청만 보내고, 서버가 이동 검증, 스킬 사용 가능 여부, 데미지 판정, 보상 지급, 저장을 담당하는 구조로 확장합니다.

### 6. Multiplayer Synchronization

원격 플레이어 생성, 이동 보간, 스킬 사용 브로드캐스트 흐름을 구현합니다.

```text
Player A Move
-> C++ Server
-> Nearby Player B, C
-> RemotePlayerController interpolation
```
