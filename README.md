# 🎮 Unity 6 3D Quarter-View MORPG

# ⚔️ 프로젝트 개요

> Unity 6 기반 3D 쿼터뷰 MORPG 클라이언트 프로토타입입니다.  
> 로그인, 캐릭터 선택, 씬 전환, 패킷 처리, 조이스틱 이동을 중심으로  
> 온라인 RPG 클라이언트의 기본 구조를 설계하고 있습니다.

> 본 프로젝트는 단순한 싱글 플레이 데모가 아니라,  
> 추후 직접 구현할 C++ IOCP 서버와 MariaDB 연동을 전제로 한  
> Server-Driven Client 구조를 목표로 합니다.

> 현재는 실제 서버 개발 전 단계이므로  
> MockServerSimulator를 통해 서버 응답을 로컬에서 시뮬레이션하고 있으며,  
> 이후 Mock 계층을 C++ 서버 + MariaDB 기반 구조로 교체할 예정입니다.

• 개발 인원: 1인  
• 개발 기간: 2026.05 ~ 진행 중  
• 개발 환경: Unity 6, C#  
• 프로젝트 성격: 온라인 MORPG 클라이언트 구조 설계 및 서버 연동 기반 구축  
• 주요 기술: uGUI, Scene Flow, Packet Architecture, Server-Driven Client, Virtual Joystick, Character Data Structure  
• 서버 구조: C++ TCP 로그인 서버 프로토타입 구현, 추후 IOCP 구조로 확장 예정  
• 확장 목표: C++ IOCP 서버 직접 구현, TCP Socket 통신, MariaDB 기반 계정 / 캐릭터 / 인벤토리 저장 구조

---

## 📑 목차

- 🎮 프로젝트 목표
- 🧭 씬 플로우 구조
- 🕹 조이스틱 기반 쿼터뷰 이동
- 🌐 서버 연동을 고려한 패킷 구조
- 🖥 C++ 서버 프로토타입
- 🧪 Local Server Simulation
- 👤 캐릭터 / 직업 데이터 구조
- 🧩 Unity UI 구조
- 🗂 폴더 구조
- 🚀 서버 / DB 연동 계획
- ✅ 현재 구현 상태

---

# 🎮 프로젝트 목표

> 온라인 MORPG에서 필요한 로그인, 캐릭터 선택, 게임 입장, 이동, 스킬 사용 흐름을  
> 서버 연동을 고려한 클라이언트 구조로 먼저 설계하는 것을 목표로 했습니다.

초기 단계부터 UI 로직, 패킷 구조, 캐릭터 데이터, 네트워크 진입점을 분리하여  
나중에 실제 C++ 서버와 MariaDB가 붙더라도 클라이언트 구조를 크게 갈아엎지 않도록 구성하고 있습니다.

현재는 로컬 시뮬레이션 방식으로 서버 응답을 흉내 내지만,  
최종 목표는 아래 구조입니다.

```text
Unity Client
→ C++ IOCP Server
→ MariaDB
```

이 구조를 통해 클라이언트는 요청만 보내고,  
로그인 검증, 캐릭터 데이터 조회, 장비 / 인벤토리 저장, 멀티플레이 동기화는  
서버가 담당하도록 확장할 예정입니다.

---

# 🧭 씬 플로우 구조

> 로그인부터 게임 입장까지의 흐름을 실제 온라인 게임 구조처럼 분리했습니다.

```text
LoginScene
→ LoadingScene
→ CharacterSelectScene
→ LoadingScene
→ GameScene
```

## 구성

- `LoginScene`  
  로그인 UI 표시 및 LoginRequest 전송

- `LoadingScene`  
  다음 씬 비동기 로딩 처리

- `CharacterSelectScene`  
  서버 응답으로 받은 캐릭터 목록 표시

- `GameScene`  
  선택한 캐릭터 데이터 기반으로 게임 월드 입장

## 관련 코드

```text
Assets/3.Script/Client/Login/LoginSceneController.cs
Assets/3.Script/Client/Scene/LoadingSceneController.cs
Assets/3.Script/Client/Character/CharacterSelectSceneController.cs
Assets/3.Script/Client/Scene/SceneFlow.cs
Assets/3.Script/Client/Scene/SceneNames.cs
```

---

# 🕹 조이스틱 기반 쿼터뷰 이동

> 모바일 쿼터뷰 MORPG 조작감을 목표로  
> 가상 조이스틱 기반 캐릭터 이동을 구현했습니다.

초기에는 클릭 이동 방식을 테스트했지만,  
모바일 MORPG 방향성에 맞춰 가상 조이스틱 방식으로 변경했습니다.

## 구현 방식

- `CharacterController` 기반 이동
- 가상 조이스틱 입력 지원
- 에디터 테스트용 WASD / 방향키 입력 지원
- 카메라 방향 기준 이동 벡터 변환
- 이동 시작 / 이동 중 / 정지 시점 감지
- MovePacket / StopPacket 전송 구조 준비

## 관련 코드

```text
Assets/3.Script/Client/Player/QuarterViewPlayerController.cs
Assets/3.Script/Client/UI/VirtualJoystick.cs
Assets/3.Script/Client/UI/GameHudController.cs
Assets/3.Script/Client/Camera/QuarterViewCameraController.cs
```

---

# 🌐 서버 연동을 고려한 패킷 구조

> 실제 TCP 서버 연동을 고려해  
> 네트워크 송신, 수신 큐, 패킷 분배 구조를 분리했습니다.

클라이언트는 직접 DB에 접근하지 않고,  
항상 서버로 요청 패킷을 보내는 구조를 목표로 합니다.

```text
Unity Client
→ Request Packet
→ C++ Server
→ DB 조회 / 게임 로직 처리
→ Response Packet
→ Unity Client
```

현재는 실제 서버가 없기 때문에 `MockServerSimulator`가 응답을 생성하지만,  
향후 `NetworkManager.SendPacket()` 내부를 TCP Socket 송신 코드로 교체하면  
기존 UI와 게임 로직은 그대로 사용할 수 있도록 설계했습니다.

## 패킷 흐름

```text
Unity UI / Player Input
→ Packet 생성
→ NetworkManager.SendPacket()
→ Local Server Simulation 또는 실제 C++ 서버
→ PacketQueue
→ PacketDispatcher
→ 각 시스템으로 이벤트 전달
```

## 구성

- `NetworkManager`  
  서버 통신 진입점

- `PacketQueue`  
  수신 패킷을 Unity 메인 스레드에서 처리하기 위한 큐

- `PacketDispatcher`  
  PacketId에 따라 처리 이벤트 분배

- `PacketBase / PacketId`  
  모든 패킷의 기본 구조

## 관련 코드

```text
Assets/3.Script/Server/Network/NetworkManager.cs
Assets/3.Script/Server/Network/PacketQueue.cs
Assets/3.Script/Server/Network/PacketDispatcher.cs
Assets/3.Script/Server/Packet/PacketBase.cs
Assets/3.Script/Server/Packet/PacketId.cs
```

---

# 🖥 C++ 서버 프로토타입

> Unity 클라이언트와 실제 TCP Socket으로 연결할 수 있는  
> C++ 서버 프로토타입을 별도 폴더로 구성했습니다.

현재 서버는 학습과 구조 검증을 위해 블로킹 TCP + 세션별 thread 방식으로 작성했으며,  
로그인 / 캐릭터 목록 / 게임 입장 요청을 처리합니다.

추후 이 구조를 기반으로 IOCP 비동기 세션 처리, MariaDB 조회, 바이너리 패킷 직렬화 구조로 확장할 예정입니다.

## 현재 서버 구조

```text
Server_CPP
├─ src
│  ├─ net
│  │  ├─ TcpServer
│  │  └─ ClientSession
│  ├─ protocol
│  │  └─ PacketCodec
│  ├─ service
│  │  └─ AuthService
│  ├─ repository
│  │  ├─ IAccountRepository
│  │  └─ MockAccountRepository
│  └─ domain
│     ├─ AccountData
│     └─ CharacterData
└─ CMakeLists.txt
```

## 현재 지원 명령

```text
LOGIN test_user mock-token
ENTER_GAME 101
PING
QUIT
```

## 서버 실행 목표

```text
Unity Client
→ C++ TCP Server
→ IAccountRepository
→ Mock Data
```

이후 DB 연동 단계에서는 `MockAccountRepository`를 `MariaDbAccountRepository`로 교체해  
실제 계정 / 캐릭터 데이터를 MariaDB에서 조회하도록 확장할 예정입니다.

## 관련 코드

```text
Server_CPP/src/main.cpp
Server_CPP/src/net/TcpServer.cpp
Server_CPP/src/net/ClientSession.cpp
Server_CPP/src/service/AuthService.cpp
Server_CPP/src/repository/MockAccountRepository.cpp
```

---

# 🧪 Local Server Simulation

> 실제 C++ 서버 개발 전에도  
> 로그인, 캐릭터 목록, 게임 입장 흐름을 테스트할 수 있도록  
> 로컬 서버 시뮬레이션 구조를 구현했습니다.

현재 `MockServerSimulator`는 DB 없이 코드 내부에서 임시 캐릭터 데이터를 생성하고,  
Unity가 보낸 요청 패킷에 대해 서버처럼 응답합니다.

이 구조는 최종 서버 구조를 대체하려는 목적이 아니라,  
클라이언트 UI / 패킷 / 씬 흐름을 먼저 검증하기 위한 임시 계층입니다.

## 현재 지원 흐름

```text
LoginRequest
→ LoginResponse
→ CharacterList

EnterGameRequest
→ EnterGameResponse
```

## 임시 캐릭터

```text
Leon / 전사
Rena / 궁수
Kain / 도적
```

## 관련 코드

```text
Assets/3.Script/Server/Network/MockServerSimulator.cs
Assets/3.Script/Shared/Protocol/LoginRequestPacket.cs
Assets/3.Script/Shared/Protocol/LoginResponsePacket.cs
Assets/3.Script/Shared/Protocol/CharacterListPacket.cs
Assets/3.Script/Shared/Protocol/EnterGameRequestPacket.cs
Assets/3.Script/Shared/Protocol/EnterGameResponsePacket.cs
```

---

# 👤 캐릭터 / 직업 데이터 구조

> 온라인 RPG에서 필요한 계정, 캐릭터, 직업, 장비, 인벤토리, 스킬 데이터를  
> 서버 / DB 연동을 고려해 분리했습니다.

현재 직업은 3개로 구성했습니다.

```text
Warrior / 전사
Archer / 궁수
Rogue / 도적
```

## 직업별 기본 스킬

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

## 데이터 구성

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

## 관련 코드

```text
Assets/3.Script/Shared/Data/AccountData.cs
Assets/3.Script/Shared/Data/CharacterData.cs
Assets/3.Script/Shared/Data/EquipmentData.cs
Assets/3.Script/Shared/Data/InventoryItemData.cs
Assets/3.Script/Shared/Data/SkillData.cs
Assets/3.Script/Shared/Define/ClassType.cs
Assets/3.Script/Client/Data/SkillDatabase.cs
```

---

# 🧩 Unity UI 구조

> 로그인, 로딩, 캐릭터 선택, 게임 HUD를 Unity uGUI 기반으로 구성했습니다.

초기 테스트용 `OnGUI` 방식에서 벗어나,  
`Canvas`, `Button`, `Text`, `Slider` 기반의 실제 Unity UI로 변경했습니다.

## 구현 UI

- 로그인 화면
- 로딩 화면
- 캐릭터 선택 화면
- 게임 HUD
- 가상 조이스틱

## 관련 코드

```text
Assets/3.Script/Client/UI/RuntimeUiFactory.cs
Assets/3.Script/Client/UI/GameHudController.cs
Assets/3.Script/Client/UI/VirtualJoystick.cs
```

---

# 🗂 폴더 구조

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
└─ src
   ├─ domain
   ├─ net
   ├─ protocol
   ├─ repository
   └─ service
```

---

# 🚀 서버 / DB 연동 계획

## 1. C++ IOCP 서버 직접 구현

현재 로컬 시뮬레이션이 담당하는 로그인 / 캐릭터 목록 / 게임 입장 응답을  
직접 구현한 C++ IOCP 서버로 교체할 예정입니다.

```text
현재
Unity Client
→ Local Server Simulation

목표
Unity Client
→ C++ IOCP Server
→ MariaDB
```

## 2. TCP Socket 통신

Unity `NetworkManager`의 송신 구조를 실제 TCP Socket 송신으로 교체하고,  
서버에서 받은 패킷은 `PacketQueue`를 통해 Unity 메인 스레드에서 처리하도록 확장할 예정입니다.

## 3. MariaDB 연동

서버에서 MariaDB를 사용해 계정, 캐릭터, 장비, 인벤토리, 스킬 데이터를 조회 / 저장하도록 구현할 예정입니다.

```text
accounts
characters
items
equipment
character_skills
```

## 4. 서버 권위 구조 확장

클라이언트는 입력과 요청만 보내고,  
이동 검증, 스킬 사용 가능 여부, 데미지 판정, 보상 지급은 서버가 판단하는 구조로 확장할 예정입니다.

## 5. 멀티플레이 동기화

원격 플레이어 생성, 이동 보간, 스킬 사용 브로드캐스트 구조를 확장할 예정입니다.

```text
Player A 이동
→ C++ Server
→ 주변 Player B, C에게 이동 패킷 전송
→ RemotePlayerController에서 보간 이동
```

---

# ✅ 현재 구현 상태

- Unity 6 프로젝트 구조 정리
- C++ TCP 로그인 서버 프로토타입 추가
- 로그인 / 로딩 / 캐릭터 선택 / 게임 씬 분리
- uGUI 기반 UI 생성
- Local Server Simulation 기반 로그인 흐름 구현
- 전사 / 궁수 / 도적 캐릭터 데이터 구성
- 조이스틱 기반 쿼터뷰 이동 구현
- 네트워크 패킷 구조 분리
- Server-Driven Client 구조를 고려한 데이터 / 패킷 / UI 흐름 분리
- C++ IOCP 서버 및 MariaDB 연동을 위한 기반 구조 설계
