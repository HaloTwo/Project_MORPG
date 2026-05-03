# 🎮 Unity 6 3D Quarter-View MORPG

# ⚔️ 프로젝트 개요

> Unity 6 기반 3D 쿼터뷰 MORPG 클라이언트와  
> C++ TCP 서버, MariaDB를 연동한 서버 주도형 RPG 구조 프로젝트

> 로그인, 회원가입, 캐릭터 선택/생성, 게임 입장 흐름을  
> Unity 클라이언트가 직접 판단하지 않고 서버 요청/응답 기반으로 처리하도록 설계했습니다.

- 개발 인원: 1인
- 개발 기간: 2026.05 ~ 진행 중
- 개발 환경: Unity 6, C# / C++17, Winsock, MariaDB
- 주요 기술: TCP Socket, Packet Dispatcher, Runtime uGUI, Scene Flow, MariaDB Repository

---

## 📑 목차

- 서버 주도형 구조를 선택한 이유
- 현재 실행 흐름
- Unity Client 구조
- C++ TCP Server 구조
- MariaDB 연동 구조
- 서버 연결 실패 처리
- 개발 일지
- 문제 해결 기록
- 실행 방법
- 현재 구현 상태
- 다음 작업

---

## 🔍 서버 주도형 구조를 선택한 이유

MORPG에서는 로그인, 캐릭터 생성, 캐릭터 데이터 저장 같은 핵심 정보를 클라이언트가 직접 처리하면 보안과 데이터 일관성 문제가 생깁니다.

따라서 Unity는 입력과 화면 표시를 담당하고, C++ 서버가 로그인 검증과 DB 조회/저장을 담당하도록 분리했습니다.

```text
Unity Client
→ C++ TCP Server
→ MariaDB
```

Unity는 MariaDB에 직접 접속하지 않습니다.

---

# 🎮 현재 실행 흐름

```text
LoginScene
→ 로그인 / 회원가입 요청
→ C++ TCP 서버
→ MariaDB 계정 조회 또는 생성
→ CharacterSelectScene
→ 캐릭터 3슬롯 표시
→ 빈 슬롯에서 직업 선택 후 캐릭터 생성
→ 서버가 DB에 캐릭터 저장
→ GameScene 입장
```

현재 테스트 계정:

```text
ID: test_user
PW: 1234
```

---

# 🧩 Unity Client 구조

## 네트워크 진입점

- `NetworkManager`가 TCP 서버 연결을 관리
- `SendPacket(PacketBase packet)`으로 로그인/회원가입/캐릭터 요청 전송
- 서버 텍스트 응답은 `ServerTextProtocol`에서 Unity 패킷으로 변환
- 수신 패킷은 `PacketQueue`에 쌓고 `Update()`에서 처리
- `PacketDispatcher`가 패킷 ID별 이벤트를 UI/게임 시스템에 전달

```text
PacketBase
→ NetworkManager
→ TcpServerConnection
→ ServerTextProtocol
→ PacketQueue
→ PacketDispatcher
→ Login / Character / Game System
```

주요 코드:

- `Assets/3.Script/Server/Network/NetworkManager.cs`
- `Assets/3.Script/Server/Network/TcpServerConnection.cs`
- `Assets/3.Script/Server/Network/ServerTextProtocol.cs`
- `Assets/3.Script/Server/Network/PacketDispatcher.cs`

---

# 🖥 C++ TCP Server 구조

서버는 Winsock 기반 Blocking TCP + ClientSession별 Thread 방식으로 구현했습니다.

```text
Server_CPP
├─ src
│  ├─ main.cpp
│  ├─ net
│  │  ├─ TcpServer
│  │  └─ ClientSession
│  ├─ protocol
│  │  └─ PacketCodec
│  ├─ service
│  │  └─ AuthService
│  ├─ repository
│  │  ├─ IAccountRepository
│  │  └─ MariaDbAccountRepository
│  └─ domain
│     ├─ AccountData
│     └─ CharacterData
└─ db
   └─ schema.sql
```

지원 명령:

```text
REGISTER id password
LOGIN id password
CREATE_CHARACTER accountId slotIndex Warrior|Archer|Rogue
ENTER_GAME characterId
PING
QUIT
```

응답 예시:

```text
LOGIN_OK accountId=1 message=LoginSuccess
CHARACTER_LIST count=0
CHARACTER_LIST_END
```

---

# 🗄 MariaDB 연동 구조

`MariaDbAccountRepository`가 실제 DB 조회/저장을 담당합니다.

- 로그인: `accounts` 테이블 조회
- 회원가입: 중복 아이디 검사 후 `accounts` INSERT
- 캐릭터 생성: 계정당 최대 3개, `characters` INSERT
- 기본 스킬: `character_skills` INSERT
- 캐릭터 입장: `characters` + `character_skills` 조회

DB 스키마:

```text
Server_CPP/db/schema.sql
```

---

# ⚠️ 서버 연결 실패 처리

서버가 켜져 있지 않거나 실행 중 연결이 끊기면 Unity에서 팝업을 표시합니다.

```text
서버가 끊겼습니다.
확인을 누르면 게임을 종료합니다.
```

확인 버튼을 누르면:

- 에디터에서는 Play Mode 종료
- 빌드에서는 `Application.Quit()` 실행

---

# 🧭 개발 일지

## 1일차 - Unity 클라이언트 기본 흐름 구성

### 구현 범위

- 로그인 씬, 로딩 씬, 캐릭터 선택 씬, 게임 씬 흐름 구성
- `SceneFlow` 기반 씬 전환 구조 작성
- 런타임 uGUI 기반 로그인 UI 생성
- `PacketBase`, `PacketId`, 로그인/입장 패킷 구조 작성
- `PacketQueue`, `PacketDispatcher`로 수신 패킷 처리 구조 분리

### 생긴 문제

초기에는 UI에서 로그인 성공 여부를 직접 처리하는 방식으로 흐름을 만들 수 있었지만, 나중에 서버가 붙으면 UI 코드와 네트워크 코드가 강하게 엮일 가능성이 있었습니다.

### 선택한 방식

UI는 요청 패킷만 만들고, 결과는 `PacketDispatcher` 이벤트를 통해 받도록 분리했습니다.

### 해결 결과

로그인 화면, 캐릭터 선택 화면, 게임 입장 흐름이 서버 응답 구조를 기준으로 동작하도록 정리되었습니다.

---

## 2일차 - C++ TCP 서버 프로토타입 구성

### 구현 범위

- `Server_CPP` 폴더에 C++ 서버 프로젝트 구성
- Winsock 기반 TCP Listen/Accept 구현
- 클라이언트 접속마다 `ClientSession`을 생성하는 구조 작성
- 텍스트 명령 기반 프로토콜 작성
- `LOGIN`, `ENTER_GAME`, `PING`, `QUIT` 처리

### 생긴 문제

처음부터 바이너리 패킷이나 IOCP 구조로 들어가면 학습 난이도와 디버깅 난이도가 동시에 올라가는 문제가 있었습니다.

### 선택한 방식

1차 목표를 "Unity와 서버가 실제로 대화하는 것"으로 잡고, 텍스트 명령 기반 Blocking TCP 서버를 먼저 구현했습니다.

### 해결 결과

서버 콘솔에서 요청/응답 흐름을 직접 확인할 수 있게 되었고, 이후 MariaDB 연동과 Unity TCP 연결을 붙일 수 있는 기반이 생겼습니다.

---

## 3일차 - 로그인/회원가입/캐릭터 생성 UI 확장

### 구현 범위

- 로그인 화면에 아이디/비밀번호 입력창 추가
- 로그인과 회원가입 버튼 분리
- 회원가입 요청/응답 패킷 추가
- 캐릭터 선택 화면을 3슬롯 구조로 변경
- 빈 슬롯에서 전사/궁수/도적 중 하나를 선택해 캐릭터 생성 요청

### 생긴 문제

테스트 아이디 `test_user` 입력 중 언더바가 입력되지 않는 문제가 있었습니다.

### 선택한 방식

Unity `InputField`의 일반 입력창은 `ContentType.Standard`, 비밀번호 입력창만 `ContentType.Password`를 사용하도록 분리했습니다.

### 해결 결과

아이디 입력에서 언더바를 정상적으로 사용할 수 있게 되었고, 로그인/회원가입/캐릭터 생성 UI가 하나의 흐름으로 이어졌습니다.

---

## 4일차 - MariaDB 스키마 설계 및 DB 연결 준비

### 구현 범위

- MariaDB Community Server 설치
- DBeaver로 DB 시각화 환경 구성
- `project_morpg` 데이터베이스 생성
- `accounts`, `characters`, `character_skills`, `inventory_items`, `equipment` 테이블 설계
- 기본 테스트 계정 `test_user / 1234` 준비

### 생긴 문제

Unity에서 MariaDB로 직접 접속할지, 서버를 거쳐 접속할지 구조 선택이 필요했습니다.

### 선택한 방식

Unity는 DB에 직접 접속하지 않고, C++ 서버만 MariaDB에 접속하는 구조로 결정했습니다.

### 해결 결과

클라이언트는 서버 요청만 담당하고, 계정/캐릭터 저장은 서버가 책임지는 구조로 정리되었습니다.

---

## 5일차 - C++ 서버와 MariaDB 실제 연동

### 구현 범위

- `IAccountRepository` 기반으로 `MariaDbAccountRepository` 추가
- 로그인 시 `accounts` 테이블 조회
- 회원가입 시 중복 아이디 확인 후 계정 추가
- 캐릭터 생성 시 슬롯/직업 검증 후 DB 저장
- 캐릭터 생성 시 기본 스킬을 `character_skills`에 저장
- C++ 서버 빌드 설정에 MariaDB include/lib/DLL 연결

### 생긴 문제

DB 접속 비밀번호와 게임 로그인 비밀번호를 혼동할 수 있었습니다. 또한 MariaDB DLL이 실행 파일 위치에 없으면 서버 실행이 실패할 수 있었습니다.

### 선택한 방식

DB root 비밀번호는 코드에 저장하지 않고 서버 실행 인자나 환경 변수로 받도록 했습니다. 빌드 후 `libmariadb.dll`은 실행 폴더로 복사되도록 설정했습니다.

### 해결 결과

`test_user / 1234` 로그인 요청이 실제 TCP 서버와 MariaDB를 거쳐 성공하는 것을 확인했습니다.

검증 응답:

```text
WELCOME ProjectMORPGServer
LOGIN_OK accountId=1 message=LoginSuccess
CHARACTER_LIST count=0
CHARACTER_LIST_END
BYE
```

---

## 6일차 - Mock 제거 및 서버 연결 실패 처리

### 구현 범위

- Unity `MockServerSimulator` 제거
- C++ `MockAccountRepository` 제거
- `NetworkManager`를 실제 TCP 서버 전용 구조로 정리
- 서버가 켜져 있지 않거나 연결이 끊기면 팝업 표시
- 확인 버튼을 누르면 게임 종료 처리
- README를 현재 실제 구조 기준으로 정리

### 생긴 문제

서버가 꺼져 있을 때 Unity에서 아무 반응 없이 로그인 요청만 실패하면 사용자가 원인을 알기 어렵습니다.

### 선택한 방식

연결 실패나 수신 스레드 종료를 `NetworkManager`가 감지하고, Unity 메인 스레드에서 종료 팝업을 띄우도록 했습니다.

### 해결 결과

서버 미실행 상태에서 요청하면 `서버가 끊겼습니다.` 팝업이 뜨고, 확인을 누르면 게임이 종료됩니다.

---

# 🧪 문제 해결 기록

## 문제 1. Unity에서 DB에 직접 접속할지 고민

- 문제: Unity Android 빌드에서 MySQL/MariaDB Connector를 직접 넣는 방법도 있었지만 보안상 좋지 않음
- 선택: Unity → C++ 서버 → MariaDB 구조 선택
- 해결: Unity는 TCP 요청만 보내고 DB 계정 정보는 서버에만 존재하도록 분리

## 문제 2. 로그인 아이디에 언더바 입력 불가

- 문제: `test_user` 입력 시 `_`가 입력되지 않음
- 원인: `InputField`의 입력 타입 설정이 일반 아이디 입력에 맞지 않음
- 해결: 아이디 입력은 `ContentType.Standard`, 비밀번호는 `ContentType.Password`로 분리

## 문제 3. MariaDB 설치 후 무엇을 해야 하는지 불명확

- 문제: DB 서버 설치와 실제 테이블 생성이 다른 단계라 흐름이 헷갈림
- 선택: `schema.sql`을 별도 파일로 만들고 MariaDB Client 또는 DBeaver에서 확인 가능하게 구성
- 해결: `accounts`, `characters` 등 실제 서버가 사용할 테이블 생성

## 문제 4. DB root 비밀번호와 게임 계정 비밀번호 혼동

- 문제: `test_user / 1234`는 게임 로그인 계정이고, 서버가 DB에 접속할 때 쓰는 비밀번호는 MariaDB root 비밀번호임
- 선택: DB 비밀번호는 코드에 하드코딩하지 않고 실행 인자로 전달
- 해결: 서버 실행 시 `ProjectMORPGServer.exe 7777 DB_ROOT_PASSWORD` 형태로 분리

## 문제 5. 서버가 꺼져 있을 때 사용자 피드백 없음

- 문제: 서버 미실행 상태에서 로그인하면 사용자가 왜 안 되는지 알기 어려움
- 선택: 연결 실패를 UI 팝업으로 명확히 표시
- 해결: `서버가 끊겼습니다.` 팝업과 확인 시 종료 처리 추가

---

# 🚀 실행 방법

## 1. MariaDB 실행

MariaDB 서비스가 실행 중이어야 합니다.

DB 확인:

```sql
USE project_morpg;
SELECT account_id, login_id, password_hash FROM accounts;
```

## 2. C++ 서버 실행

```powershell
cd C:\Users\user\Documents\GitHub\Project_MORPG\Server_CPP\x64\Debug
.\ProjectMORPGServer.exe 7777 DB_ROOT_PASSWORD
```

`DB_ROOT_PASSWORD`는 게임 로그인 비밀번호가 아니라 MariaDB root 비밀번호입니다.

정상 로그:

```text
[Server] Listening on port 7777
```

## 3. Unity 실행

Unity 로그인 화면에서:

```text
test_user / 1234
```

---

# ✅ 현재 구현 상태

- Unity Runtime 로그인 UI
- 회원가입 요청/응답
- 캐릭터 3슬롯 선택 UI
- 직업 3종 캐릭터 생성
- C++ TCP 서버
- MariaDB 계정/캐릭터 저장
- Mock 서버 제거
- 서버 연결 실패 팝업 및 종료 처리

---

# 🛠 다음 작업

- 비밀번호 평문 저장을 해시 저장 방식으로 변경
- 캐릭터 삭제 기능 추가
- 캐릭터 이름 직접 입력 기능 추가
- 이동/전투 패킷을 서버 판정 구조로 확장
- Blocking Thread 서버를 IOCP 기반 서버로 확장
