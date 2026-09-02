using System;

[Serializable]
public class AmmoInstance : ItemInstance
{
    // Cleanly expose the strongly-typed AmmoItem definition without shadowing base
    public new AmmoItem Definition => base.Definition as AmmoItem;
    
    // Convenience getter that fetches AmmoType directly from the definition asset
    public AmmoType AmmoType => Definition != null ? Definition.ammoType : AmmoType.HANDGUN;

    public AmmoInstance(AmmoItem definition, int count) : base(definition, count)
    {
        // base(definition, count) sets the base Definition field properly.
        // No extra assignment needed here.
    }
}