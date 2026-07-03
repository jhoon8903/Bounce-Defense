네, 시니어 게임 클라이언트 개발자로서 '통통 디펜스: 핀볼 마스터' Stage 1 영상을 정밀 분석하여 보고하겠습니다. Unity 재구현에 필요한 구체적인 데이터를 관찰과 추론으로 나누어 정확하게 추출하겠습니다.

---

## 관찰 보고서: Stage 1 (Deep Forest)

### 1. 화면에 보이는 모든 숫자

**[관찰] 시간대별 숫자 데이터**

*   **00:00**
    *   스테이지: "1. Deep Forest"
    *   진행도: "0%"
    *   웨이브: "1" (깃발 아이콘 옆)
    *   플레이어 HP: "300" (빨간 후드 캐릭터 하단)
*   **00:09**
    *   일반 데미지: "8" (흰색 텍스트)
*   **00:17 - Level Up (Lv. 2)**
    *   카드 1 [Laser Ball (Horizontal)]: "Best!", "Deals **7** damage", 아이콘 좌측 상단 숫자 "11"
    *   카드 2 [Fire Ball]: "Inflicts Burn for **4s** (Max **3** stacks). Deals **8** damage per stack", 아이콘 숫자 "21"
    *   카드 3 [Cluster Ball]: "**40%** chance to create a Shard Ball. Shard Balls deal **10** damage", 아이콘 숫자 "27"
*   **00:24**
    *   레이저 볼 데미지: "7", "11" (청록색 텍스트). 카드 설명의 7과 일치하는 값과, 아이콘의 11과 일치하는 값이 동시에 보임.
*   **00:29 - Level Up (Lv. 3)**
    *   카드 1 [The Last Matchstick]: "Best!", "dealing **10** damage to nearby enemies"
    *   카드 2 [Iron Boots]: "Move Speed by **20%**", "increases damage by **25%**"
    *   카드 3 [Magic Potion]: "fluctuates between **-20%** and **+35%**"
*   **00:32**
    *   폭발 데미지 (The Last Matchstick): "11", "15" (붉은색 텍스트). 카드 설명의 10과 다름.
*   **00:34 - Level Up (Lv. 4)**
    *   카드 1 [Cluster Ball]: "Best!", "**40%** chance", "**10** damage", 아이콘 숫자 "27"
    *   카드 3 [Laser Ball (Horizontal)]: 아이콘 숫자 "11" -> "15"로 업그레이드 표시.
*   **00:42 - Level Up (Lv. 5)**
    *   진행도: "26%"
    *   카드 1 [Ghost Ball]: "Best!", 아이콘 숫자 "14"
    *   카드 2 [Warm Tin Heart]: "Increases all Normal Ball damage by **20%**"
    *   카드 3 [Cluster Ball]: "**50%** chance", "**15** damage", 아이콘 숫자 "27" -> "30"
*   **01:06 - Level Up (Lv. 6)**
    *   진행도: "38%"
    *   카드 1 [Fire Ball]: "Best!", "Inflicts Burn for **4s** (Max **3** stacks). Deals **8** damage", 아이콘 숫자 "21"
    *   카드 2 [Cluster Ball]: "**50%** chance", "**15** damage", 아이콘 숫자 "27" -> "30"
    *   카드 3 [Tortoise Shell]: "blocks **1** instance of damage", "Recharges every **40** seconds"
*   **01:16 - Level Up (Lv. 7)**
    *   진행도: "48%"
    *   카드 1 [Fire Ball]: "Inflicts Burn for **5s** (Max **4** stacks). Deals **10** damage", 아이콘 숫자 "21" -> "24"
    *   카드 2 [Thorny Rose]: "Damage increases as enemies get closer (Up to **20%**)."
    *   카드 3 [Cluster Ball]: "Best!", "**50%** chance", "**15** damage", 아이콘 숫자 "27" -> "30"
*   **01:24 - Level Up (Lv. 8)**
    *   진행도: "53%"
    *   카드 2 [Fire Ball]: "Best!", "Inflicts Burn for **5s** (Max **4** stacks). Deals **10** damage", 아이콘 숫자 "21" -> "24"
    *   카드 3 [Golden Goose Egg]: "**50%** chance", "fire **1** Normal Ball(s)"
*   **01:39 - Level Up (Lv. 9)**
    *   진행도: "60%"
    *   카드 1 [Laser Ball (Horizontal)]: "Best!", "Deals **11** damage", 아이콘 숫자 "11" -> "15"
    *   카드 2 [Amethyst Dagger]: "Increases Crit Rate by **10%**"
    *   카드 3 [Magic Mirror]: "next hit damage by **20%**"
*   **01:44 - Level Up (Lv. 10)**
    *   진행도: "70%"
    *   카드 1 [Red Cloak]: "Best!", "Reduces damage taken by **20%**"
    *   카드 2 [Alchemist's Stone]: "**10%** chance", "Max **1** time per shot"
*   **01:51 - Level Up (Lv. 11)**
    *   진행도: "81%"
    *   카드 1 [Cluster Ball]: "Best!", "**60%** chance", "**20** damage", 아이콘 숫자 "30" -> "33"
    *   카드 2 [The Last Matchstick]: "**20** damage"
*   **01:57 - Level Up (Lv. 12)**
    *   진행도: "87%"
    *   카드 1 [Ghost Ball]: "Best!", 아이콘 숫자 "14" -> "21"
    *   카드 2 [Laser Ball (Horizontal)]: "Deals **15** damage", 아이콘 숫자 "15" -> "19"
*   **02:20 - BOSS 등장**
    *   보스 HP: "4,600"

### 2. HUD 레이아웃

*   **[관찰] 상단 HUD**
    *   좌측: `AUTO` 토글 버튼.
    *   중앙: `1. Deep Forest` (스테이지 이름) | 주황색 진행도 바 | 진행도 `%` | 웨이브 숫자.
    *   우측: `||` (일시정지) 버튼.
*   **[관찰] 하단 HUD**
    *   플레이어 캐릭터 및 HP 바(`300`).
    *   초기에는 `Tap the screen...` 또는 `Touch the desired location...` 같은 튜토리얼 텍스트 박스가 나타남.
*   **[관찰] 레벨업 UI**
    *   상단: `Level Up` 타이틀, 레벨 숫자와 함께 차오르는 경험치 바.
    *   중앙:
        *   `Active Skill`: 4개의 슬롯.
        *   `Passive Skill`: 2개의 슬롯.
        *   스킬을 획득하면 해당 슬롯에 아이콘이 채워짐 (00:29부터 확인 가능).
    *   하단: 3개의 스킬/볼 카드 선택지.
    *   최하단: `Refresh` 버튼과 남은 횟수 (`AD Refresh Left: 1/1`).

### 3. 색상/팔레트

*   **[관찰] 배경**: 어둡고 채도가 낮은 녹색/청록색 톤. 덩굴 느낌의 프레임. 플레이 영역은 희미한 격자무늬.
*   **[관찰] UI**:
    *   기본 프레임/버튼: 어두운 갈색/적갈색 바탕에 금색 테두리 및 텍스트.
    *   진행도 바: 선명한 주황색.
    *   레벨업 카드: 어두운 청록색 바탕. 'Best!' 태그는 노란색.
*   **[관찰] 데미지 텍스트**:
    *   일반/클러스터 볼: 흰색.
    *   화염/폭발(Last Matchstick): 주황색/붉은색.
    *   레이저 볼: 청록색.
    *   고스트 볼: 보라색.
*   **[관찰] 볼 색상**:
    *   일반 볼: 흰색.
    *   레이저 볼: 청록색.
    *   화염 볼: 주황색.
    *   클러스터 볼: 짙은 녹색.
    *   고스트 볼: 보라색.

### 4. 화면 비율 & 안전영역

*   **[관찰]** 영상 해상도(1206x2622)는 약 1:2.17 비율의 세로형 화면.
*   **[관찰]** 실제 게임 플레이가 이뤄지는 격자 영역은 전체 화면 높이의 약 75-80%를 차지.
*   **[추론]** 상단 HUD가 약 10%, 하단 플레이어 영역이 약 10-15%를 차지하는 것으로 보임. UI 요소들이 화면 끝에 붙어있지 않아 대부분의 모바일 기기 노치/안전영역에 대응 가능할 것으로 추정됨.

### 5. 타이밍

*   **[관찰] 웨이브 간격**: 몬스터를 모두 처치하면 거의 즉시 다음 웨이브 몬스터가 생성됨. 웨이브 클리어 시간에 따라 간격이 결정됨.
    *   예: 웨이브 1 (00:03~00:16) 약 13초 소요.
*   **[관찰] 레벨업 빈도**:
    *   Lv.2: 00:17
    *   Lv.3: 00:29 (+12초)
    *   Lv.4: 00:34 (+5초)
    *   Lv.5: 00:42 (+8초)
    *   ...
    *   [추론] 레벨업은 고정 시간이 아니라 적 처치로 얻는 경험치에 따라 결정됨. 초반에는 느리다가 화력이 강해지면서 레벨업 속도가 빨라짐.
*   **[관찰] 볼 발사 간격**:
    *   초기에는 탭하여 수동 발사.
    *   이후 자동 발사 시, 볼들이 촘촘한 간격으로 연속 발사됨.
    *   [추론] 시각적으로 초당 4-5회 스트림(여러 발이 한 줄로 나가는 것)이 발사되는 느낌. 정확한 수치는 프레임 단위 분석이 필요하나, 매우 빠른 연사 속도.

---

### **재구현용 수치 표**

아래는 관찰을 통해 확정 가능한 주요 수치와 신뢰도입니다.

| 항목 | 값 | 확신도 | 비고 |
| :--- | :--- | :--- | :--- |
| 플레이어 시작 HP | `300` | 높음 | |
| 액티브 스킬 슬롯 수 | `4`개 | 높음 | |
| 패시브 스킬 슬롯 수 | `2`개 | 높음 | |
| 일반 볼 기본 데미지 | `8` | 중간 | 초반 데미지 기준. 버프/스킬로 변동 가능성 있음. |
| **Laser Ball (Lvl 1)** | 텍스트: `7`, 실제 데미지: `7`, `11` | 높음 | **카드 설명과 실제 값이 다를 수 있음.** |
| **Last Matchstick (Lvl 1)** | 텍스트: `10`, 실제 데미지: `11`, `15` | 높음 | **카드 설명과 실제 값이 다를 수 있음.** |
| Cluster Ball (Lvl 1) 확률 | `40%` | 높음 | 카드 텍스트 기준. |
| Cluster Ball (Lvl 1) 파편 데미지 | `10` | 높음 | 카드 텍스트 기준. |
| 보스 HP | `4,600` | 높음 | |

**[개발자 의견]**
분석 결과, 일부 스킬 카드에 표기된 수치와 실제 적용되는 데미지 수치 간에 차이가 관찰되었습니다. 이는 버그일 수도 있고, 보이지 않는 다른 스탯(e.g., 아이콘의 숨겨진 레벨 값)의 영향을 받는 복잡한 계산식일 수도 있습니다. 재구현 시, 이 부분을 '표기 값'과 '실제 적용 값'을 분리하여 기획하고 테스트하는 방향을 고려해야 합니다.