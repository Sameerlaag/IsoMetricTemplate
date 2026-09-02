using System;

[Serializable]
public class WeaponInstance : ItemInstance
{
    // Cleanly expose the strongly-typed GunItem definition without shadowing base
    public new GunItem Definition => base.Definition as GunItem;

    public int CurrentAmmo { get; set; }

    public WeaponInstance(GunItem definition, int count) : base(definition, count)
    {
        if (definition != null && definition.WeaponData != null)
        {
            CurrentAmmo = definition.WeaponData.MaxClipAmmo;
        }
    }
}