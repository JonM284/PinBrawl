namespace Runtime.Perks
{
    public enum EventListenState
    {
        NONE = 0,
        OWNER_PRE_DIED = 1,
        OWNER_KILL_OTHER = 2,
        OWNER_DAMAGED = 3,
        OWNER_DAMAGE_OTHER = 4,
        OWNER_SWAP_BALL = 5,
        OWNER_PROJECTILE_END = 6,
    }
}