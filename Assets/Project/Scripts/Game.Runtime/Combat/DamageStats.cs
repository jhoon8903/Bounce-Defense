using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.Combat
{
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
            if (view == null || amount <= 0) return;
            SkillEffectKind skill = MapToSkill(sourceType, kind);
            _byKind.TryGetValue(skill, out long cur);
            _byKind[skill] = cur + amount;
            Total += amount;
        }

        private static SkillEffectKind MapToSkill(BallSourceType src, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Burn: return SkillEffectKind.FireBall;
                case DamageKind.LaserRow: return SkillEffectKind.LaserBall;
                case DamageKind.Explosion: return SkillEffectKind.LastMatch;
                case DamageKind.ClusterSpawn: return SkillEffectKind.ClusterBall;
                default:
                    switch (src)
                    {
                        case BallSourceType.Fire: return SkillEffectKind.FireBall;
                        case BallSourceType.Ice: return SkillEffectKind.IceBall;
                        case BallSourceType.Laser: return SkillEffectKind.LaserBall;
                        case BallSourceType.Ghost: return SkillEffectKind.GhostBall;
                        case BallSourceType.Cluster: return SkillEffectKind.ClusterBall;
                        default: return SkillEffectKind.NormalBall;
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
