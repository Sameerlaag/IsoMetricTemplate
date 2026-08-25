using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private CharacterController[] _characterSlots;
        [SerializeField] private Image _backgroundImage;

        public void SetBackground(Sprite bgSprite)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = bgSprite;
                _backgroundImage.enabled = bgSprite != null;
            }
        }

        public void SetCharacterSprite(VNCharacterPosition position, Sprite sprite)
        {
            CharacterController slot = GetSlotAtPosition(position);
            if (slot != null)
            {
                slot.SetSprite(sprite);
            }
        }

        public void ClearCharacterSlot(VNCharacterPosition position)
        {
            CharacterController slot = GetSlotAtPosition(position);
            if (slot != null)
            {
                slot.Clear();
            }
        }

        private CharacterController GetSlotAtPosition(VNCharacterPosition position)
        {
            if (_characterSlots == null) return null;
            foreach (var slot in _characterSlots)
            {
                if (slot != null && slot.Position == position)
                {
                    return slot;
                }
            }
            return null;
        }
    }
}
