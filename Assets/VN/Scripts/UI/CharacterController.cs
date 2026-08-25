using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private VNCharacterPosition _position;
        [SerializeField] private string _characterID;
        [SerializeField] private Image _characterImage;

        public VNCharacterPosition Position => _position;
        public string CharacterID => _characterID;

        public void SetSprite(Sprite sprite)
        {
            if (_characterImage != null)
            {
                _characterImage.sprite = sprite;
                _characterImage.enabled = sprite != null;
            }
        }

        public void Clear()
        {
            if (_characterImage != null)
            {
                _characterImage.sprite = null;
                _characterImage.enabled = false;
            }
        }
    }
}