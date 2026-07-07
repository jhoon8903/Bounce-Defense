using Game.Core.Observer;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyModel : Observable
    {
        public enum SpawnPhase { Entering, Active, Breaching }

        private SpawnPhase _phase = SpawnPhase.Active;
        private float _freezeSlow;
        public string Id { get; private set; }
        public EnemyDefinition Definition { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public Vector2 Position { get; private set; }
        public bool IsDead { get; private set; }

        public float DescentSpeed => (Definition != null ? Definition.DescentSpeed : 0f) * (1f - _freezeSlow);
        public Footprint Footprint => Definition != null ? Definition.Footprint : Footprint.Size1x1;
        public bool IsEntering => _phase == SpawnPhase.Entering;
        public bool IsBreaching => _phase == SpawnPhase.Breaching;
        public float LandedY { get; private set; }

        public void Initialize(string id, EnemyDefinition definition, Vector2 position, float hpScale = 1f)
        {
            Id = id;
            Definition = definition;
            int rawHp = definition != null ? definition.BaseHp : 1;
            MaxHp = Mathf.Max(1, Mathf.RoundToInt(rawHp * (hpScale > 0f ? hpScale : 1f)));
            Hp = MaxHp;
            Position = position;
            IsDead = false;
            _freezeSlow = 0f;
            _phase = SpawnPhase.Active;
            Raise();
        }

        public void BeginEntering(float landedY)
        {
            _phase = SpawnPhase.Entering;
            LandedY = landedY;
        }

        public void MarkActive() => _phase = SpawnPhase.Active;
        public void BeginBreaching() => _phase = SpawnPhase.Breaching;

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || _phase == SpawnPhase.Entering || _phase == SpawnPhase.Breaching) return;
            Hp = Mathf.Max(0, Hp - amount);
            if (Hp == 0) IsDead = true;
            Raise();
        }

        public void SetPosition(Vector2 position)
        {
            if (Position == position) return;
            Position = position;
            Raise();
        }

        public void SetFreezeSlow(float slow)
        {
            _freezeSlow = Mathf.Clamp01(slow);
        }
    }
}
