namespace Runtime.Abilities
{
    /// <summary>
    /// The Direction that the hit target should go after receiving knockback
    /// </summary>
    public enum KnockbackDirectionType
    {
        /// <summary> Relative to player aimed direction at time of hit</summary>
        AIM_DIRECTION,
        /// <summary> Relative to direction from user towards target at time of hit</summary>
        OUTWARD_RELATIVE,
        /// <summary> Relative to direction from target towards user at time of hit</summary>
        INWARD_RELATIVE,
    }
}