using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 스테이지 중 스킬(볼+패시브)별 누적 데미지 집계(결과창 DTResult, 가산점).
    //  - CombatEventHub.OnHit 구독 → 적 피격(view != null)만 (sourceType, kind) → SkillEffectKind로 매핑해 누적.
    //  - 패시브 Last Match(Explosion)·Fire 번(Burn)·Laser 행(LaserRow)·Cluster 특수볼(ClusterSpawn)도 각 스킬로 정확히 귀속.
    //  - 브리치(캐릭터 피격)는 view=null이라 자동 제외. 단검·양철·거울은 다른 스킬 데미지를 보정만 → 별도 항목 없음(녹아듦).
    // 재시작=씬 리로드(DI 새 인스턴스)라 별도 Reset 불필요.
    public sealed class DamageStats : IDisposable
    {
        private readonly CombatEventHub _hub;
        private readonly Dictionary<SkillEffectKind, long> _byKind = new();

        public IReadOnlyDictionary<SkillEffectKind, long> ByKind => _byKind;
        public long Total { get; private set; }

        public DamageStats(CombatEventHub hub)
        {
            _hub = hub;
            _hub.OnHit += OnHit;
        }

        private void OnHit(EnemyView view, Vector2 pos, int amount, bool isCrit, Vector2 hitDir, BallSourceType sourceType, DamageKind kind)
        {
            if (view == null || amount <= 0) return; // 적 피격만(브리치=플레이어 피해 제외)
            SkillEffectKind skill = MapToSkill(sourceType, kind);
            _byKind.TryGetValue(skill, out long cur);
            _byKind[skill] = cur + amount;
            Total += amount;
        }

        // 데미지 원천 → 스킬. kind가 우선(번=Fire·행=Laser·폭발=LastMatch·분열=Cluster), 직격은 볼 타입 기준.
        private static SkillEffectKind MapToSkill(BallSourceType src, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Burn: return SkillEffectKind.FireBall;
                case DamageKind.LaserRow: return SkillEffectKind.LaserBall;
                case DamageKind.Explosion: return SkillEffectKind.LastMatch;   // 패시브: 마지막 성냥
                case DamageKind.ClusterSpawn: return SkillEffectKind.ClusterBall;
                default: // Direct
                    switch (src)
                    {
                        case BallSourceType.Fire: return SkillEffectKind.FireBall;
                        case BallSourceType.Ice: return SkillEffectKind.IceBall;
                        case BallSourceType.Laser: return SkillEffectKind.LaserBall;
                        case BallSourceType.Ghost: return SkillEffectKind.GhostBall;
                        case BallSourceType.Cluster: return SkillEffectKind.ClusterBall;
                        default: return SkillEffectKind.NormalBall; // 노멀 볼(집계 항목). 양철·거울 보정도 노멀 직격에 녹아 여기 귀속.
                    }
            }
        }

        public long DamageOf(SkillEffectKind kind) => _byKind.TryGetValue(kind, out long v) ? v : 0;

        public void Dispose()
        {
            if (_hub != null) _hub.OnHit -= OnHit;
        }
    }
}
