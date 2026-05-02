# 🎮 Unity 6 3D Quarter-View MORPG

# ⚔️ 프로젝트 개요

> Unity 6 기반 3D 쿼터뷰 MORPG 클라이언트와  
> C++ TCP 서버 프로토타입을 함께 설계한 온라인 RPG 구조 프로젝트입니다.

> 로그인, 캐릭터 선택, 씬 전환, 패킷 처리, 조이스틱 이동을  
> 단순 싱글 플레이 로직이 아니라 서버 연동을 고려한 구조로 분리했습니다.

> 현재 Unity 클라이언트는 `MockServerSimulator`를 통해 로컬 서버 응답을 시뮬레이션하며,  
> 별도 `Server_CPP` 폴더에는 Winsock 기반 C++ TCP 서버 프로토타입을 구현했습니다.

> 최종 목표는 클라이언트가 입력과 요청만 보내고,  
> C++ 서버가 로그인 검증, 캐릭터 데이터 조회, 이동/전투 검증, DB 저장을 담당하는  
> Server-Driven MORPG 구조입니다.

• 개발 인원: 1인  
• 개발 기간: 2026.05 ~ 진행 중  
• 개발 환경: Unity 6, C# / C++17, Winsock, CMake  
• 주요 기술: uGUI, Scene Flow, Packet Architecture, Virtual Joystick, TCP Server Prototype  
• 서버 구조: Blocking TCP + ClientSession별 Thread 방식 프로토타입  
• 확장 목표: Unity TCP Client 연동, IOCP 서버, MariaDB, 바이너리 패킷 직렬화

---

## 📑 목차

- 🔍 Server-Driven 구조를 목표로 한 이유
- 🎮 Core Systems (핵심 시스템)
  - 🧭 씬 플로우 구조
  - 🌐 클라이언트 패킷 / 네트워크 구조
  - 🖥 C++ TCP 서버 프로토타입
  - 🧪 Local Server Simulation
  - 🕹 조이스틱 기반 쿼터뷰 이동
  - 👤 캐릭터 / 직업 / 스킬 데이터 구조
  - 🧩 Runtime uGUI 구조
- 🗂 프로젝트 폴더 구조
- 🚀 서버 / DB 확장 계획
- ✅ 현재 구현 상태

---

## 🔍 Server-Driven 구조를 목표로 한 이유

> MORPG 구조에서는 클라이언트가 모든 데이터를 직접 판단하면  
> 로그인, 캐릭터 저장, 이동 검증, 전투 판정, 보상 처리에서 신뢰 문제가 발생합니다.

> 따라서 초기 클라이언트 단계부터 UI, 데이터, 패킷, 네트워크 진입점을 분리하여  
> 이후 실제 C++ 서버와 MariaDB가 붙어도 전체 구조를 크게 갈아엎지 않도록 설계했습니다.

현재 Unity 내부에서는 Mock 서버가 응답을 대신 만들지만, 흐름 자체는 서버 요청/응답 구조를 따릅니다.

```text
Unity UI / Player Input
→ Request Packet
→ NetworkManager
→ MockServerSimulator 또는 C++ TCP Server
→ Response Packet
→ PacketQueue
→ PacketDispatcher
→ Scene / UI / Player System
```

<details>
<summary><b>구조 설계 의도 펼치기/닫기</b></summary>

### 1️⃣ 클라이언트 로직과 서버 응답 분리

- 로그인 성공 여부를 UI가 직접 판단하지 않음
- 캐릭터 목록은 응답 패킷으로 수신
- 게임 입장도 `EnterGameRequest / EnterGameResponse` 흐름으로 처리
- 나중에 Mock 계층을 실제 TCP 서버로 교체하기 쉽게 구성

### 2️⃣ Unity 메인 스레드 처리 보장

- 서버에서 받은 패킷을 바로 UI나 GameObject에 반영하지 않고 `PacketQueue`에 적재
- `Update()`에서 일정 개수만 처리해 Unity 메인 스레드에서 안전하게 이벤트 분배
- `PacketDispatcher` 이벤트를 통해 각 씬 컨트롤러가 필요한 패킷만 구독

### 3️⃣ 서버 확장 대비

- C# 데이터 구조와 C++ 도메인 구조를 비슷하게 유지
- `IAccountRepository`를 통해 Mock Data와 DB 구현체를 교체할 수 있게 설계
- 현재 텍스트 명령 기반 서버를 이후 바이너리 패킷 / Protobuf 구조로 확장 예정

</details>

---

# 🎮 Core Systems (핵심 시스템)

## 🧭 씬 플로우 구조

> 로그인부터 캐릭터 선택, 게임 입장까지의 흐름을  
> 실제 온라인 게임 진입 구조처럼 씬 단위로 분리했습니다.

```text
LoginScene
→ LoadingScene
→ CharacterSelectScene
→ LoadingScene
→ GameScene
```

### 구성

- `LoginScene`  
  로그인 UI 생성 및 `LoginRequestPacket` 전송

- `LoadingScene`  
  다음 씬 비동기 로딩 처리

- `CharacterSelectScene`  
  서버 응답으로 받은 캐릭터 목록 표시 및 입장 요청

- `GameScene`  
  선택된 캐릭터 데이터 기반 월드 진입

> 씬 이름과 전환 흐름은 `SceneNames`, `SceneFlow`로 분리하여  
> 화면 로직에서 직접 문자열을 관리하지 않도록 구성했습니다.

🔗 Scene Flow 코드: `Assets/3.Script/Client/Scene/SceneFlow.cs`  
🔗 Login Controller 코드: `Assets/3.Script/Client/Login/LoginSceneController.cs`  
🔗 Character Select 코드: `Assets/3.Script/Client/Character/CharacterSelectSceneController.cs`

---

## 🌐 클라이언트 패킷 / 네트워크 구조

> 실제 TCP 서버 연동을 고려해  
> 송신 진입점, 수신 큐, 패킷 분배 구조를 분리했습니다.

### 구현 방식

- `NetworkManager`를 전역 네트워크 진입점으로 사용
- `SendPacket(PacketBase packet)`으로 모든 요청 패킷 송신
- 현재는 `useLocalSimulation` 값에 따라 Mock 서버로 전달
- 수신 패킷은 `PacketQueue`에 적재
- `PacketDispatcher`가 `PacketId`에 따라 이벤트 분배
- 프레임당 처리 개수를 제한해 패킷 폭주 상황 대비

```text
PacketBase
→ PacketId
→ NetworkManager.SendPacket()
→ PacketQueue
→ PacketDispatcher
→ Login / Character / Game System
```

> 현재 `NetworkManager` 내부에는 실제 TCP Socket 송신을 넣을 TODO 지점이 있으며,  
> 이 부분만 교체하면 기존 UI와 씬 흐름은 그대로 유지할 수 있도록 설계했습니다.

🔗 NetworkManager 코드: `Assets/3.Script/Server/Network/NetworkManager.cs`  
🔗 PacketQueue 코드: `Assets/3.Script/Server/Network/PacketQueue.cs`  
🔗 PacketDispatcher 코드: `Assets/3.Script/Server/Network/PacketDispatcher.cs`  
🔗 PacketId 코드: `Assets/3.Script/Server/Packet/PacketId.cs`

---

## 🖥 C++ TCP 서버 프로토타입

> Unity 클라이언트와 실제 TCP Socket으로 연결할 수 있는  
> C++ 서버 프로토타입을 `Server_CPP` 폴더에 별도로 구성했습니다.

현재 서버는 Windows Winsock 기반이며, 학습과 구조 검증을 위해  
Blocking TCP + ClientSession별 Thread 방식으로 구현했습니다.

### 현재 처리 명령

```text
LOGIN test_user mock-token
ENTER_GAME 101
PING
QUIT
```

### 서버 응답 예시

```text
WELCOME ProjectMORPGServer
LOGIN_OK accountId=1 message=MockLoginSuccess
CHARACTER_LIST count=3
CHARACTER id=101 name=Leon class=Warrior level=1 gold=100 pos=-2,1,0 skills=1001,1002,1003
CHARACTER id=102 name=Rena class=Archer level=1 gold=100 pos=0,1,0 skills=2001,2002,2003
CHARACTER id=103 name=Kain class=Rogue level=1 gold=100 pos=2,1,0 skills=3001,3002,3003
CHARACTER_LIST_END
```

### 서버 구조

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

### 빌드 방법

```powershell
cd Server_CPP
cmake -S . -B build
cmake --build build --config Debug
.\build\Debug\ProjectMORPGServer.exe
```

기본 포트는 `7777`이며 실행 인자로 변경할 수 있습니다.

```powershell
.\build\Debug\ProjectMORPGServer.exe 7777
```

🔗 TcpServer 코드: `Server_CPP/src/net/TcpServer.cpp`  
🔗 ClientSession 코드: `Server_CPP/src/net/ClientSession.cpp`  
🔗 AuthService 코드: `Server_CPP/src/service/AuthService.cpp`  
🔗 MockAccountRepository 코드: `Server_CPP/src/repository/MockAccountRepository.cpp`

---

## 🧪 Local Server Simulation

> 실제 TCP 연동 전에도 클라이언트의 로그인, 캐릭터 선택, 게임 입장 흐름을  
> 검증할 수 있도록 Unity 내부에 Mock 서버 계층을 구현했습니다.

### 지원 흐름

```text
LoginRequest
→ LoginResponse
→ CharacterList

EnterGameRequest
→ EnterGameResponse
```

### Mock 캐릭터

```text
Leon / Warrior
Rena / Archer
Kain / Rogue
```

Mock 캐릭터에는 기본 위치, 골드, 장비, 소비 아이템, 직업별 기본 스킬이 포함됩니다.

> 이 구조는 최종 서버를 대체하기 위한 것이 아니라,  
> UI / 패킷 / 씬 흐름을 먼저 검증하기 위한 임시 서버 계층입니다.

🔗 MockServerSimulator 코드: `Assets/3.Script/Server/Network/MockServerSimulator.cs`

---

## 🕹 조이스틱 기반 쿼터뷰 이동

> 모바일 MORPG 조작감을 목표로  
> 가상 조이스틱 기반 쿼터뷰 이동을 구현했습니다.

### 구현 방식

- `CharacterController` 기반 이동
- 가상 조이스틱 입력 지원
- 에디터 테스트용 WASD / 방향키 입력 지원
- 카메라 방향 기준 이동 벡터 변환
- 이동 방향 기반 캐릭터 회전
- 이동 시작 / 이동 중 / 정지 시점 감지
- 일정 간격으로 `MovePacket` 생성
- 정지 시 `StopPacket` 생성

> 이동 입력은 클라이언트에서 즉시 반영하되,  
> 서버 연동 단계에서는 이동 패킷을 통해 검증 및 동기화할 수 있도록 이벤트 구조를 준비했습니다.

🔗 Player Controller 코드: `Assets/3.Script/Client/Player/QuarterViewPlayerController.cs`  
🔗 VirtualJoystick 코드: `Assets/3.Script/Client/UI/VirtualJoystick.cs`  
🔗 Camera Controller 코드: `Assets/3.Script/Client/Camera/QuarterViewCameraController.cs`

---

## 👤 캐릭터 / 직업 / 스킬 데이터 구조

> 계정, 캐릭터, 장비, 인벤토리, 스킬 데이터를  
> 서버와 DB 연동을 고려해 분리했습니다.

### 직업 구성

```text
Warrior / 전사
Archer / 궁수
Rogue / 도적
```

### 직업별 기본 스킬

```text
전사
- Slash
- Shield Bash
- Whirlwind

궁수
- Arrow Shot
- Power Shot
- Rain of Arrows

도적
- Stab
- Dash Attack
- Backstab
```

### 데이터 구성

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

> Unity의 Mock 데이터와 C++ 서버의 도메인 데이터를 비슷한 형태로 맞춰  
> 이후 DB 테이블과 서버 응답 구조로 확장하기 쉽게 구성했습니다.

🔗 CharacterData 코드: `Assets/3.Script/Shared/Data/CharacterData.cs`  
🔗 SkillDatabase 코드: `Assets/3.Script/Client/Data/SkillDatabase.cs`  
🔗 C++ CharacterData 코드: `Server_CPP/src/domain/CharacterData.h`

---

## 🧩 Runtime uGUI 구조

> 로그인, 캐릭터 선택, 게임 HUD를 런타임 uGUI 생성 방식으로 구성했습니다.

초기 테스트용 `OnGUI` 방식에서 벗어나,  
`Canvas`, `Panel`, `Text`, `Button`, `Slider`를 코드에서 생성하는 구조로 정리했습니다.

### 구현 UI

- 로그인 화면
- 로딩 화면
- 캐릭터 선택 카드 UI
- 게임 HUD
- HP / MP 표시
- 가상 조이스틱
- 스킬 슬롯 UI

> UI Prefab이 없어도 씬 진입 시 필요한 화면을 생성할 수 있어,  
> 현재처럼 구조를 빠르게 검증하는 단계에서 씬별 UI 의존성을 줄일 수 있었습니다.

🔗 RuntimeUiFactory 코드: `Assets/3.Script/Client/UI/RuntimeUiFactory.cs`  
🔗 GameHudController 코드: `Assets/3.Script/Client/UI/GameHudController.cs`

---

# 🗂 프로젝트 폴더 구조

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

---

# 🚀 서버 / DB 확장 계획

## 1. Unity TCP Client 연동

현재 `NetworkManager`의 Mock 송신 경로를 실제 TCP Socket 송수신 구조로 교체할 예정입니다.

```text
현재
Unity Client
→ MockServerSimulator

목표
Unity Client
→ C++ TCP Server
```

## 2. 패킷 직렬화 구조 교체

현재 C++ 서버는 텍스트 명령 기반으로 동작합니다.  
이후 Unity C# 패킷 구조와 맞춰 바이너리 직렬화 또는 Protobuf 기반 구조로 교체할 예정입니다.

## 3. IOCP 서버 확장

현재 서버는 Blocking TCP + Thread 방식입니다.  
다음 단계에서는 IOCP 기반 비동기 세션 처리로 확장하여 다중 접속 처리 구조를 개선할 예정입니다.

## 4. MariaDB 연동

`IAccountRepository` 구현체를 `MockAccountRepository`에서 `MariaDbAccountRepository`로 교체하여  
실제 계정, 캐릭터, 장비, 인벤토리, 스킬 데이터를 DB에서 조회 / 저장하도록 확장할 예정입니다.

```text
accounts
characters
items
equipment
character_skills
```

## 5. 서버 권위 구조 확장

클라이언트는 입력과 요청만 보내고,  
서버가 이동 검증, 스킬 사용 가능 여부, 데미지 판정, 보상 지급을 담당하는 구조로 확장할 예정입니다.

## 6. 멀티플레이 동기화

원격 플레이어 생성, 이동 보간, 스킬 사용 브로드캐스트 구조를 구현할 예정입니다.

```text
Player A 이동
→ C++ Server
→ 주변 Player B, C에게 이동 패킷 전송
→ RemotePlayerController에서 보간 이동
```

---

# ✅ 현재 구현 상태

- Unity 6 프로젝트 구조 정리
- 로그인 / 로딩 / 캐릭터 선택 / 게임 씬 분리
- Runtime uGUI 기반 UI 생성
- Local Server Simulation 기반 로그인 / 캐릭터 선택 / 게임 입장 흐름 구현
- 전사 / 궁수 / 도적 캐릭터 데이터 구성
- 장비 / 인벤토리 / 스킬 데이터 구조 구성
- 조이스틱 기반 쿼터뷰 이동 구현
- Move / Stop / Skill / Damage / Spawn 패킷 구조 정의
- PacketQueue / PacketDispatcher 기반 패킷 처리 구조 분리
- C++ TCP 서버 프로토타입 구현
- C++ 서버 LOGIN / ENTER_GAME / PING / QUIT 명령 처리
- `IAccountRepository` 기반 Repository 교체 구조 준비
- IOCP 서버 및 MariaDB 연동을 위한 기반 구조 설계
