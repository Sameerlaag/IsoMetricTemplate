using System;

[Serializable]
public class GunInstance : ItemInstance
{
    // Cleanly expose the strongly-typed GunItem definition without shadowing base
    public new GunItem Definition => base.Definition as GunItem;

    public GunInstance(GunItem definition, int count) : base(definition, count)
    {
        // base(definition, count) sets the base Definition field properly.
        // No extra assignment needed here.
    }
}