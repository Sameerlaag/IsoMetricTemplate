using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("General")] public string WeaponName = "Rifle";
    public GameObject WeaponPrefab;
    public AmmoType RequiredAmmoType = AmmoType.HANDGUN;
    public WeaponType WeaponType = WeaponType.ONE_HANDED;
    
    [Header("Stats")] public int MaxClipAmmo = 30;
    public float FireRate = 0.15f; // Time between shots
    public float ReloadTime = 1.8f;

    [Header("Accuracy & Spread")] public float BaseSpread = 2f;
    public float MovementSpreadMultiplier = 1.5f;
    public float RecoilPerShot = 1f;

    [Header("Sprites")] public Sprite EnabledAimSprite;
    public Sprite DisabledAimSprite;
}