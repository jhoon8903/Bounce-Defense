namespace Game.Combat
{
    public interface IStatusReceiver
    {
        void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks);
        void ApplyFreeze(float durationSeconds, float slow);
    }
}
