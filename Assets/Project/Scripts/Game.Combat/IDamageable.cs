namespace Game.Combat
{
    // 볼(또는 향후 공격원)이 데미지를 줄 수 있는 대상. 그리드 occupant 브리지로도 쓰인다(GridController).
    public interface IDamageable
    {
        void ApplyDamage(int amount);
    }
}
