# 렌더링 SRP 배칭 정리 — 진행/계획 (§12-12)

목표: URP를 쓰는 이상 **모든 렌더러가 SRP 배처를 타도록** 셰이더/머티리얼을 통일한다.
(성능 이득 + "URP를 제대로 설계했다"는 제출 인상. Daniel 스타일 = URP면 SRP 배칭이 정답.)

## 진단 (프레임 디버거 실측)
- 전투 프레임(볼 발사 + 적 다수) = **68 이벤트**. events 0~32이 `DrawSRPBatcher ↔ Draw` 핑퐁.
- 원인 = **URP 셰이더(SRP) ↔ 레거시 셰이더(non-SRP) 혼존**이 렌더 순서상 교차.
  - 볼/보드/캐릭 = `Universal Render Pipeline/2D/Sprite-Unlit-Default` (SRP ✅)
  - **트레일** = `Sprites/Default` (레거시 ❌)  ← 수정 완료
  - **적 블록·몹** = `Game/SpriteHitFlash` (레거시 CG + `UnitySprites.cginc` + MPB ❌❌)
  - 적 HP바·그림자 = URP 스프라이트(SRP ✅) → 적마다 URP HP바가 레거시 적 배치를 깸.

## 완료 (이번 세션 §12-12)
1. **트레일 → URP**: `Trail_Particle.mat` `Sprites/Default` → `URP/2D/Sprite-Unlit-Default`.
   텍스처 없는 순수 버텍스컬러 트레일이라 외형 100% 동일.
2. **"Ball" 정렬 레이어 신설** (TagManager, uniqueID `3990651`, Default 위=맨앞).
   6개 볼 프리팹의 스프라이트+트레일을 Ball 레이어로 → 볼+트레일이 **맨 위 단일 SRP 배치**(event 52).
   ⚠️ 교훈: 정렬레이어 추가/프리팹 편집은 **에디트 모드에서** 할 것. 플레이 모드에서 TagManager를 만지면
   도메인 리로드에 반영이 날아가고 프리팹만 죽은 레이어ID(3990651)를 참조 → `<unknown layer>` 발생함.

## 다음 세션 = 적 URP 통합 (남은 주범, Daniel 승인함)
전제: **먼저 Daniel 미커밋 적 작업(E_Enemy.prefab·EnemyView.cs·흰 Fill 스프라이트)을 커밋해 클린 베이스 확보.**

두 가지 동시 필요 (하나만으론 SRP 배칭 안 됨):
1. **셰이더 재작성** `Game/SpriteHitFlash` 레거시 CG → **URP HLSL 2D 스프라이트**.
   - 베이스: `Packages/com.unity.render-pipelines.universal/Shaders/2D/Sprite-Unlit-Default.shader` 복사.
   - `UnityPerMaterial` CBUFFER에 `_Color/_FlashColor/_FlashAmount/_FrostColor/_FrostAmount` 선언(= SRP 조건).
   - 프래그먼트: 냉동 틴트(`_FrostAmount`로 `_FrostColor` lerp) → 히트 플래시(`_FlashAmount`로 `_FlashColor` lerp) → 프리멀티플. 기존 수식 그대로.
2. **EnemyView.cs: MPB → 캐시된 인스턴스 머티리얼**.
   - MPB는 셰이더가 URP여도 SRP 배칭을 깬다(렌더러가 SRP 부적격).
   - `_blockMat = blockRenderer.material`(1회 인스턴스화, 캐시), `_mobMat = mobRenderer.material`. 이후 `SetFloat/SetColor`는 캐시된 인스턴스에.
   - 풀 오브젝트라 인스턴스 영구 재사용 → **핫패스 할당 0**. despawn 시 flash/frost 0 복원(고스트 방지, 이미 있음).
   - SRP 배처는 **같은 셰이더의 서로 다른 머티리얼 인스턴스**를 배칭함 → 적들 배칭됨.
- 기대: 적 블록·몹·HP바·그림자 전부 URP → Default 레이어가 소수 배치로. 월드 이벤트 ~52 → ~10~15.
- 리스크: 흰 피격 플래시 + 냉동 청록 틴트 **외형 재검증(Daniel 눈)**. EnemyView 수정(그의 파일).

## 파티클 SRP 리서치 결과 (Daniel 질문: 6.3에서 파티클도 SRP 배칭 되지 않나?)
**결론: 안 됨 — 설계상 제외 (Unity 6.3/6000.x 공식 문서 기준).**
- Unity 6 매뉴얼: "HDRP·URP의 모든 Lit/Unlit 셰이더가 SRP 배처 요건을 충족하나 **파티클 버전은 예외**."
  6.3에서 이를 바꾼 언급 없음.
- 근본 이유: Shuriken `ParticleSystemRenderer`는 매 프레임 동적 지오메트리(월드공간 정점에 파티클 트랜스폼을 구움)라
  SRP 배처가 필요로 하는 **지속적 per-object `UnityPerDraw` CBUFFER가 없음** → 버전 문제 아님, 아키텍처 문제.
- 파티클 드로우콜 줄이는 실제 방법: ① 머티리얼/아틀라스 공유(파티클 자체 배칭) ② **VFX Graph**(GPU 파티클) ③ 메시 파티클 + GPU 인스턴싱.
- → 13개 `Particles/Standard Unlit`을 URP 파티클 셰이더로 바꿔도 **SRP 배칭 이득 0** + 외형 리스크. 스프라이트 트레일과 달리 파티클 셰이더 교체는 비추천. (단 `Particles/Standard Unlit`은 빌트인이라 URP에서 렌더 정합/HDR 이슈 여지는 있음 — 별건.)
- 출처: docs.unity3d.com/6000.3 SRPBatcher, discussions.unity.com "Could SRP batcher work with particles?"

## 남은 제출 항목 (렌더링과 별개)
④ 밸런싱(플레이) · ⑧ APK(Android IL2CPP, **FRAMEWORK_DEBUG define 제거 필수**) · ⑨ 영상+제출 패키지.
