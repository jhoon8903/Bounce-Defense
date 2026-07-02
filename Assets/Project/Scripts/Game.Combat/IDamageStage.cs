namespace Game.Combat
{
    public interface IDamageStage
    {
        void Process(HitContext context);
    }
}
