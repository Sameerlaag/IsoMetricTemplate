using UnityEngine;

public class CharatcerController : MonoBehaviour
{
    [SerializeField] private CharacterPosition _position;
    [SerializeField] private string _charatcerID;
    
    
}

internal enum CharacterPosition
{
    Left,
    Right,
    Middle
}