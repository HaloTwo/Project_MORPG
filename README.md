# 🎮 Unity 6 3D Quarter-View MORPG Client Prototype

# ⚔️ 프로젝트 개요

> Unity 6 기반 3D 쿼터뷰 MORPG 클라이언트 프로토타입입니다.  
> 실제 서버 연동 전 단계에서 로그인, 캐릭터 선택, 씬 전환, 패킷 처리, 조이스틱 이동 구조를 먼저 설계했습니다.

> 추후 C++ IOCP 서버와 MariaDB 연동을 고려하여  
> NetworkManager, PacketDispatcher, PacketQueue, Packet 구조를 분리하고,  
> 현재는 MockServerSimulator를 통해 서버 응답을 로컬 시뮬레이션합니다.

• 개발 인원: 1인  
• 개발 환경: Unity 6, C#  
• 프로젝트 성격: 모바일 쿼터뷰 MORPG 클라이언트 구조 설계  
• 주요 기술: uGUI, Scene Flow, Mock Server, Packet Architecture, Virtual Joystick, Character Data Structure

---

## 📑 목차

- 🎮 프로젝트 목표
- 🧭 씬 플로우 구조
- 🕹 조이스틱 기반 쿼터뷰 이동
- 🌐 네트워크 / 패킷 구조
- 🧪 Mock Server Simulation
- 👤 캐릭터 / 직업 데이터 구조
- 🧩 Unity UI 구조
- 🗂 폴더 구조
- 🚀 향후 개발 계획

---

# 🎮 프로젝트 목표

> 본 프로젝트는 단순한 싱글 플레이 데모가 아니라,  
> 추후 온라인 MORPG로 확장 가능한 클라이언트 구조를 설계하는 것을 목표로 했습니다.

초기 단계에서는 실제 서버와 DB를 바로 연결하지 않고,  
클라이언트 내부에 Mock 서버를 두어 로그인, 캐릭터 선택, 게임 입장 흐름을 먼저 검증했습니다.

이를 통해 이후 C++ IOCP 서버와 MariaDB를 연결할 때  
Unity 클라이언트의 UI 및 패킷 처리 구조를 크게 변경하지 않도록 구성했습니다.

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
  다음 씬 로딩 처리

- `CharacterSelectScene`  
  서버에서 받은 캐릭터 목록 표시

- `GameScene`  
  선택한 캐릭터로 게임 월드 입장

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

> 마비노기 모바일과 유사한 쿼터뷰 시점에서  
> 가상 조이스틱을 사용해 캐릭터를 이동하도록 구현했습니다.

초기에는 디아블로 / 로스트아크 방식의 클릭 이동을 구현했지만,  
모바일 MORPG 방향성에 맞춰 조이스틱 이동 방식으로 변경했습니다.

## 구현 방식

- `CharacterController` 기반 이동
- 가상 조이스틱 입력 지원
- 에디터 테스트용 WASD / 방향키 입력 지원
- 카메라 방향 기준 이동 벡터 변환
- 이동 시작 / 이동 중 / 정지 시점 감지
- 이동 패킷 전송 구조 준비

## 관련 코드

```text
Assets/3.Script/Client/Player/QuarterViewPlayerController.cs
Assets/3.Script/Client/UI/VirtualJoystick.cs
Assets/3.Script/Client/UI/GameHudController.cs
Assets/3.Script/Client/Camera/QuarterViewCameraController.cs
```

---

# 🌐 네트워크 / 패킷 구조

> 실제 TCP 서버 연동을 고려해  
> 네트워크 송신, 수신 큐, 패킷 분배 구조를 분리했습니다.

현재는 실제 서버가 없기 때문에 `MockServerSimulator`를 사용하지만,  
나중에 `NetworkManager.SendPacket()` 내부를 TCP Socket 송신 코드로 교체하면  
기존 UI와 게임 로직은 그대로 사용할 수 있도록 설계했습니다.

## 패킷 흐름

```text
Unity UI / Player Input
→ Packet 생성
→ NetworkManager.SendPacket()
→ MockServerSimulator 또는 실제 서버
→ PacketQueue
→ PacketDispatcher
→ 각 시스템으로 이벤트 전달
```

## 구성

- `NetworkManager`  
  패킷 송신 입구

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

# 🧪 Mock Server Simulation

> 실제 C++ 서버 개발 전에도  
> 로그인, 캐릭터 목록, 게임 입장 흐름을 테스트할 수 있도록 Mock 서버를 구현했습니다.

Mock 서버는 DB 없이 코드 내부에서 임시 캐릭터 데이터를 생성하고,  
Unity가 보낸 요청 패킷에 대해 서버처럼 응답합니다.

## 현재 지원 흐름

```text
LoginRequest
→ LoginResponse
→ CharacterList

EnterGameRequest
→ EnterGameResponse
```

## Mock 캐릭터

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
> 서버 연동을 고려해 분리했습니다.

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
```

---

# 🚀 향후 개발 계획

## 1. C++ 서버 연동

현재 `MockServerSimulator`가 담당하는 부분을  
C++ 서버와 TCP Socket 통신으로 교체할 예정입니다.

```text
현재
Unity → MockServerSimulator

목표
Unity → C++ IOCP Server → MariaDB
```

## 2. MariaDB 연동

계정, 캐릭터, 장비, 인벤토리, 스킬 데이터를 DB에 저장하고  
서버가 로그인 시 DB에서 데이터를 조회하도록 확장할 예정입니다.

## 3. 캐릭터 모델 / 애니메이션 적용

현재는 임시 Capsule 기반이며,  
추후 직업별 모델과 Idle / Move / Skill 애니메이션을 적용할 예정입니다.

## 4. 멀티플레이 동기화

원격 플레이어 생성, 이동 보간, 스킬 사용 브로드캐스트 구조를 확장할 예정입니다.

```text
Player A 이동
→ Server
→ 주변 Player B, C에게 이동 패킷 전송
→ RemotePlayerController에서 보간 이동
```

---

# ✅ 현재 구현 상태

- Unity 6 프로젝트 구조 정리
- 로그인 / 로딩 / 캐릭터 선택 / 게임 씬 분리
- uGUI 기반 UI 생성
- Mock 로그인 흐름 구현
- 전사 / 궁수 / 도적 캐릭터 데이터 구성
- 조이스틱 기반 쿼터뷰 이동 구현
- 네트워크 패킷 구조 분리
- Mock 서버 시뮬레이션 구현
