# 🎮 Project MORPG

## C++ TCP Server 기반 Unity 3D Quarter-View MORPG

> Unity 클라이언트가 모든 데이터를 직접 판단하는 구조가 아니라,  
> **C++ TCP 서버가 로그인, 회원가입, 캐릭터 생성, 캐릭터 입장 흐름을 검증하고,  
> MariaDB에 계정/캐릭터 데이터를 저장하는 구조**로 설계한 MORPG 프로젝트입니다.  
>
> 현재는 계정/캐릭터 생성 흐름까지 구현했으며,  
> 이후 인벤토리, 장비, 전투, 스킬 사용 구조를 서버 검증 방식으로 확장할 예정입니다.

- 개발 인원: 1인
- 개발 기간: 2026.05.01 ~ 진행 중
- 현재 진행 상황: 개발 3일차
- 개발 환경: Unity 6, C#, C++17, Winsock, MariaDB, DBeaver, GitHub, Codex
- 핵심 키워드: TCP Socket, Server-Driven Flow, Packet Dispatcher, Repository Pattern, MariaDB, Character Persistence, Inventory/Combat Expansion

---

## 🎯 프로젝트 목표

이 프로젝트의 목표는 단순히 Unity에서 로그인 UI를 만드는 것이 아니라,  
**온라인 RPG에서 필요한 서버 중심 구조를 작은 단위부터 직접 구성해보는 것**입니다.

먼저 로그인, 회원가입, 캐릭터 생성, 캐릭터 입장 흐름을 서버와 DB 기반으로 구성하고,  
이후 인벤토리, 장비, 전투, 스킬 사용 흐름까지 서버 검증 구조로 확장하는 것을 목표로 합니다.

현재는 Blocking TCP 기반 서버와 텍스트 프로토콜을 사용하지만, 구조는 이후 IOCP 서버와 바이너리 패킷으로 확장할 수 있도록 분리했습니다.

```text
Unity Client
→ C++ TCP Server
→ MariaDB
```

Unity는 DB에 직접 접근하지 않고, 서버에 요청만 보냅니다.  
계정 검증과 캐릭터 저장은 서버가 담당합니다.

---

## 🧱 현재 구현 범위

- 로그인 / 회원가입 UI
- 계정당 캐릭터 3슬롯 구조
- Warrior / Archer / Rogue 캐릭터 생성
- Unity TCP Client 연결
- C++ Winsock TCP Server
- MariaDB 계정 / 캐릭터 저장
- 캐릭터 기본 스킬 저장 구조
- 서버 연결 실패 시 종료 팝업 처리
- Mock 서버 제거 후 실제 서버 흐름으로 전환

---

## 🧩 시스템 구조

```text
LoginScene
→ LoginRequest / RegisterRequest
→ NetworkManager
→ TcpServerConnection
→ C++ ClientSession
→ AuthService
→ MariaDbAccountRepository
→ MariaDB
```

### Unity Client

Unity 쪽은 화면과 입력을 담당하고, 서버 응답을 받은 뒤 씬 흐름을 전환합니다.

- `NetworkManager`: 서버 연결과 송신 진입점
- `TcpServerConnection`: 실제 TCP 송수신
- `ServerTextProtocol`: 서버 텍스트 응답을 Unity 패킷으로 변환
- `PacketDispatcher`: 패킷 ID 기준 이벤트 분배
- `CharacterSession`: 로그인 계정과 선택 캐릭터 상태 저장

### C++ Server

C++ 서버는 클라이언트 요청을 받아 계정/캐릭터 로직을 처리합니다.

- `TcpServer`: 포트 Listen 및 클라이언트 Accept
- `ClientSession`: 클라이언트별 요청 처리
- `PacketCodec`: 텍스트 명령 파싱 및 응답 생성
- `AuthService`: 로그인, 회원가입, 캐릭터 생성 흐름 제어
- `IAccountRepository`: 저장소 추상화
- `MariaDbAccountRepository`: MariaDB 실제 조회/저장

---

## 🗄 DB 설계

현재 DB는 계정과 캐릭터 생성을 중심으로 구성했습니다.

```text
accounts
characters
character_skills
inventory_items
equipment
```

핵심 설계는 다음과 같습니다.

- `accounts.login_id`는 UNIQUE로 중복 가입 방지
- `characters`는 `(account_id, slot_index)` UNIQUE로 계정당 슬롯 중복 방지
- `slot_index`는 0~2까지만 허용
- `class_type`은 Warrior / Archer / Rogue 값만 허용
- 캐릭터 생성 시 기본 스킬을 `character_skills`에 함께 저장

---

## 📅 개발 로그

<details>
<summary><b>1일차 - 개발 환경 세팅과 Unity 자동화 기반 구성</b></summary>

### 구현

- Unity 6 프로젝트 기본 구조 세팅
- Unity MCP와 LLM/Codex 작업 흐름 연결
- Codex를 통한 로컬 코드 분석 및 자동 수정 환경 구성
- 기본 씬 구조와 스크립트 폴더 구조 정리
- Runtime uGUI 기반으로 UI를 코드에서 생성하는 방향 결정

### 설계 판단

초기에는 Unity 에디터에서 UI를 직접 배치할 수도 있었지만, 로그인/캐릭터 선택 UI는 서버 흐름 검증용으로 빠르게 바뀔 가능성이 높았습니다.

그래서 프리팹 기반 UI보다 코드 기반 Runtime UI를 먼저 사용했습니다.  
반복 수정이 빠르고, LLM/Codex로 구조를 읽고 수정하기 쉬운 장점이 있었기 때문입니다.

</details>

<details>
<summary><b>2일차 - Unity 클라이언트 흐름과 C++ TCP 서버 프로토타입</b></summary>

### 구현

- `LoginScene → CharacterSelectScene → GameScene` 흐름 구성
- `PacketBase`, `PacketId`, 요청/응답 패킷 구조 작성
- `PacketQueue`, `PacketDispatcher` 기반 수신 패킷 분배 구조 구성
- C++ Winsock TCP 서버 생성
- `LOGIN`, `ENTER_GAME`, `PING`, `QUIT` 명령 처리
- Unity 클라이언트가 서버 응답 흐름을 따라가도록 구조 분리

### 설계 판단

처음부터 IOCP나 바이너리 패킷을 적용하면 네트워크 구조보다 디버깅 비용이 더 커질 수 있다고 판단했습니다.

그래서 1차 서버는 Blocking TCP + 텍스트 프로토콜로 만들었습니다.  
목표는 성능 서버가 아니라, **Unity와 C++ 서버가 실제 요청/응답 흐름으로 연결되는 구조를 먼저 검증하는 것**이었습니다.

</details>

<details>
<summary><b>3일차 - MariaDB 연동, Mock 제거, 실제 서버 흐름 전환</b></summary>

### 구현

- MariaDB 스키마 작성
- DBeaver로 DB 구조 확인
- `MariaDbAccountRepository` 추가
- 로그인 / 회원가입 / 캐릭터 생성 DB 연동
- Unity `NetworkManager`를 실제 TCP 서버 전용 구조로 변경
- Mock 서버 및 Mock Repository 제거
- 서버 미실행 또는 연결 끊김 시 종료 팝업 추가

### 설계 판단

Unity에서 DB에 직접 접속하는 방식도 가능은 하지만, 클라이언트에 DB 접속 정보가 노출되고 데이터 검증 책임이 클라이언트로 이동하는 문제가 있습니다.

따라서 DB 접근은 C++ 서버 내부로 제한했습니다.  
Unity는 요청만 보내고, 서버가 계정 중복 검사와 캐릭터 슬롯 제한을 검증하도록 만들었습니다.

</details>

---

## ✅ 현재 상태

```text
Unity 로그인 UI
→ C++ TCP 서버 접속
→ MariaDB 계정 검증
→ 캐릭터 목록 수신
→ 빈 슬롯 캐릭터 생성
→ DB 저장
```


## 🛠 다음 개발 예정

현재는 로그인, 계정, 캐릭터 생성까지의 서버 기반 흐름을 먼저 구현한 상태입니다.  
이후에는 실제 RPG 플레이에 필요한 전투와 인벤토리 시스템을 서버 흐름과 연결해 확장할 예정입니다.

- 캐릭터 이름 입력 기능
- 캐릭터 삭제 기능
- 인벤토리 아이템 획득 / 사용 / 장착 구조
- 장비 슬롯 및 스탯 반영 구조
- 몬스터 기본 AI 및 전투 타겟팅
- 일반 공격 / 스킬 사용 패킷 구조
- 데미지 계산과 HP 변경 서버 검증
- 이동 패킷 서버 검증
- 비밀번호 해시 저장 구조 적용
- Blocking TCP 서버를 IOCP 기반으로 개선


