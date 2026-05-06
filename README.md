# 🎮 Project MORPG

## C++ TCP Server 기반 Unity 3D Quarter-View MORPG

> Unity 클라이언트가 DB를 직접 다루지 않고,
> **C++ TCP 서버가 계정 인증, 회원가입, 캐릭터 생성/삭제, 입장 흐름을 검증한 뒤
> MariaDB에 데이터를 저장하는 구조**로 설계한 MORPG 프로젝트입니다.
>
> 현재는 온라인 RPG의 가장 앞단인
> **계정 → 캐릭터 슬롯 → 캐릭터 생성/삭제 → 게임 입장** 흐름을 먼저 구현했습니다.
>
> > AI 기반 개발 보조 도구를 활용해 반복 구현과 디버깅 시간을 줄이고,  
> 서버-클라이언트-DB 연동 구조를 빠르게 검증하는 방식으로 개발하고 있습니다.

- 개발 인원: 1인
- 개발 기간: 2026.05.01 ~ 진행 중
- 개발 환경: Unity 6, C#, C++17, Winsock, MariaDB, DBeaver, GitHub, Codex, Unity MCP
- 주요 기술: TCP Socket, Server-Driven Flow, Packet Dispatcher, Repository Pattern, MariaDB

---

## 🎯 프로젝트 목표

> 온라인 RPG는 클라이언트가 “된다/안 된다”를 최종 판단하면 위험하다고 생각했습니다.
> 그래서 Unity는 입력과 화면 흐름을 담당하고, 실제 계정과 캐릭터 데이터는 서버가 검증하도록 분리했습니다.

현재는 빠른 검증을 위해 Blocking TCP + Text Protocol로 시작했습니다.
다만 Unity 패킷, 서버 세션, 서비스 계층, DB Repository를 분리해 두어
이후 IOCP 서버나 Binary Packet 구조로 확장할 수 있게 설계했습니다.

```text
Unity Client
→ C++ TCP Server
→ MariaDB
```

Unity는 서버에 요청만 보내고,
로그인 성공 여부와 캐릭터 저장/삭제는 서버와 DB가 결정합니다.

---

## 🧩 Core Systems

## 🔐 계정 시스템 (Login / Register)

- 로그인: 아이디 + 비밀번호 검증
- 회원가입: 별도 회원가입 팝업 UI에서 신규 계정 생성
- 중복 아이디 방지: `accounts.login_id` UNIQUE 제약
- 로그인 실패 시 사용자용 메시지 표시
- 서버 미실행 또는 연결 끊김 시 종료 팝업 처리

> 계정 검증은 Unity가 아니라 C++ 서버의 `AuthService`와 `MariaDbAccountRepository`가 담당합니다.
> Unity는 입력값을 패킷으로 보내고, 서버 응답에 따라 화면만 전환합니다.

## 👤 캐릭터 슬롯 시스템

- 계정당 최대 3개의 캐릭터 슬롯 제공
- 빈 슬롯에서 Warrior / Archer / Rogue 생성
- 캐릭터 생성 시 서버가 DB에 저장
- 캐릭터 삭제 시 서버가 계정 소유 여부를 확인한 뒤 DB에서 삭제
- 삭제 후에도 슬롯 번호가 밀리지 않도록 `slot_index` 기준으로 UI 표시
- 게임 씬에서 캐릭터 선택창으로 돌아가기 지원

> 캐릭터 슬롯은 단순 리스트 순서가 아니라 DB의 `slot_index`를 기준으로 표시합니다.
> 그래서 2번 슬롯 캐릭터만 삭제해도 1번/3번 슬롯 위치가 유지됩니다.


## 🌐 TCP 네트워크 흐름

```text
LoginScene / CharacterSelectScene
→ NetworkManager
→ TcpServerConnection
→ ServerTextProtocol
→ C++ ClientSession
→ AuthService
→ MariaDbAccountRepository
→ MariaDB
```

- `NetworkManager`: Unity 네트워크 진입점
- `TcpServerConnection`: 실제 TCP 송수신
- `ServerTextProtocol`: Unity Packet ↔ 서버 Text Command 변환
- `PacketDispatcher`: 서버 응답을 UI 이벤트로 분배
- `ClientSession`: 클라이언트별 명령 처리
- `AuthService`: 로그인/회원가입/캐릭터 생성/삭제 흐름 제어
- `IAccountRepository`: 저장소 추상화
- `MariaDbAccountRepository`: MariaDB 조회/저장/삭제 구현

---

## 🗄 DB 설계

```text
accounts
characters
character_skills
inventory_items
equipment
```

- `accounts`: 로그인 계정 저장
- `characters`: 계정별 캐릭터 슬롯, 직업, 레벨, 위치 저장
- `character_skills`: 직업별 기본 스킬 슬롯 저장
- `inventory_items`, `equipment`: 이후 인벤토리/장비 확장을 위한 기본 테이블

<details>
<summary><b>DB 제약 설계 펼치기/닫기</b></summary>

- `accounts.login_id`는 UNIQUE로 중복 회원가입을 방지합니다.
- `characters(account_id, slot_index)`는 UNIQUE로 같은 계정의 같은 슬롯 중복 생성을 막습니다.
- `slot_index`는 0~2만 허용해 계정당 3칸 구조를 DB에서도 보장합니다.
- 캐릭터 삭제 시 관련 스킬/인벤토리/장비 데이터가 함께 정리되도록 FK Cascade를 사용합니다.

</details>

---

## 🧪 개발 로그

<details>
<summary><b>1일차 - Unity 기본 구조와 자동화 작업 환경 구성</b></summary>

- Unity 6 프로젝트 기본 구조 정리
- Unity MCP + Codex 기반 로컬 코드 분석/수정 흐름 구축
- Runtime uGUI 방식으로 로그인/선택 UI를 코드에서 생성
- GitHub를 기준으로 작업 변경사항 관리

> 초반에는 빠르게 UI를 바꾸고 테스트해야 했기 때문에,
> 씬에 UI를 직접 배치하기보다 코드 기반 Runtime UI로 검증 속도를 우선했습니다.

</details>

<details>
<summary><b>2일차 - Unity Client와 C++ TCP Server 연결</b></summary>

- C++17 Winsock 기반 TCP 서버 구현
- Unity TCP Client 연결
- `LOGIN`, `ENTER_GAME`, `PING`, `QUIT` 명령 처리
- Packet / Dispatcher / Protocol 구조 분리

> 처음부터 복잡한 IOCP 서버로 가지 않고 Blocking TCP로 시작했습니다.
> 목적은 성능 최적화보다 “Unity → Server → 응답 처리” 흐름을 먼저 검증하는 것이었습니다.

</details>

<details>
<summary><b>3일차 - MariaDB 연동과 실제 계정/캐릭터 저장</b></summary>

- MariaDB schema 구성
- DBeaver로 DB 테이블과 데이터 확인
- Mock 서버/Repository 제거
- 로그인/회원가입/캐릭터 생성 DB 연동
- 캐릭터 삭제, 로그아웃, 캐릭터 선택 복귀 흐름 추가
- 서버 연결 실패 시 사용자 팝업 처리

> Unity에서 DB에 직접 접근하지 않고 C++ 서버만 DB를 다루도록 결정했습니다.
> 이 구조가 이후 인벤토리, 장비, 전투 판정처럼 신뢰가 필요한 기능을 확장하기 좋다고 판단했습니다.

</details>

<details>
<summary><b>4일차 - GameScene 맵 구성과 외부 에셋 적용</b></summary>

- RPGPP_LT 배경 에셋을 GameScene의 `Map` 루트 아래에 배치
- Unity 6 URP 환경에 맞춰 외부 에셋 머티리얼을 URP/Lit 셰이더로 변환
- 캐릭터/애니메이션/무기 에셋을 넣기 위한 모델·애니메이션 슬롯 폴더 정리
- 쿼터뷰 카메라, 조이스틱 이동, GameScene 복귀 흐름과 맵 배치를 함께 점검
- 배경 프리팹 및 FBX 모델 import 설정에 Collider를 사전 적용
- 이동을 막아야 하는 지형/건물/소품과 지나갈 수 있는 장식 에셋을 구분

> 단순히 배경을 배치하는 단계에서 끝내지 않고, 실제 플레이어 이동이 가능한 필드로 만들기 위해<br>
> 머티리얼 변환, 프리팹 충돌 세팅, GameScene 월드 구성을 함께 정리했습니다.

</details>

---

## ✅ 현재 동작 흐름

```text
1. 서버 실행
2. Unity LoginScene 실행
3. 로그인 또는 회원가입 요청
4. 서버가 MariaDB에서 계정 검증/생성
5. 캐릭터 목록 수신
6. CharacterSelectScene에서 3슬롯 표시
7. 빈 슬롯에서 캐릭터 생성
8. 생성된 캐릭터 입장 또는 삭제
9. GameScene 입장 후 쿼터뷰 맵에서 조이스틱 이동
10. 캐릭터 선택창으로 복귀 가능
```

## 🎥 동작 GIF

<details>
<summary><b>01. 서버 연결 후 로그인</b></summary>

<br>

![](Image/01-login.gif)

</details>

<details>
<summary><b>02. 캐릭터 생성 및 삭제</b></summary>

<br>

![](Image/02-character-create-delete.gif)

</details>

<details>
<summary><b>03. 캐릭터 선택 및 인게임</b></summary>

<br>

![](Image/03-character-select-ingame.gif)

</details>

<details>
<summary><b>04. 재로그인 및 서버 끊김 처리</b></summary>

<br>

![](Image/04-relogin-server-disconnect.gif)

</details>

<details>
<summary><b>05. 캐릭터 이동 및 조이스틱</b></summary>

<br>

![](Image/05-character-move-joystick.gif)

</details>



<details>
<summary><b>실행 관련 메모 펼치기/닫기</b></summary>

<br>

- 서버는 DB 비밀번호를 코드에 저장하지 않습니다.
- 실행 시 DB 비밀번호를 콘솔 입력 또는 실행 인자로 전달합니다.
- 기본 테스트 계정은 `test_user / 1234`입니다.
- 서버가 꺼져 있으면 Unity에서 “서버가 끊겼습니다.” 팝업을 표시합니다.

</details>

---

## 🚀 다음 개발 예정

- 캐릭터 이름 입력 기능
- 비밀번호 해시 저장
- 인벤토리 획득 / 사용 / 장착 구조
- 장비 스탯 반영
- 몬스터 Spawn / AI / 전투 타겟팅
- 스킬 사용 요청과 서버 검증
- HP / 데미지 계산 서버 처리
- 이동 패킷 검증
- NavMesh 기반 이동 가능 영역 정리
- 맵 오브젝트별 충돌 박스 세밀 조정
- Blocking TCP 서버를 IOCP 기반 구조로 개선
