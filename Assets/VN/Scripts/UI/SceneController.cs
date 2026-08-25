using System.Collections;
using UnityEngine;
using UnityEngine.UI;


    public class SceneController : MonoBehaviour
    {
        [SerializeField] private CharacterController[] _characterSlots;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private VNAnimationConfig _config;

        private Coroutine _bgFadeCoroutine;

        public void SetBackground(Sprite bgSprite, string animFlag)
        {
            if (_backgroundImage == null) return;

            if (_bgFadeCoroutine != null) StopCoroutine(_bgFadeCoroutine);

            _backgroundImage.sprite = bgSprite;

            if (bgSprite == null)
            {
                _backgroundImage.enabled = false;
                return;
            }

            _backgroundImage.enabled = true;

            if (!string.IsNullOrEmpty(animFlag) && animFlag.Contains("fade_in"))
            {
                float duration = GetDurationFromFlag(animFlag);
                _bgFadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration));
            }
            else
            {
                Color c = _backgroundImage.color;
                c.a = 1f;
                _backgroundImage.color = c;
            }
        }

        public void SetCharacterSprite(VNCharacterPosition position, Sprite sprite, string animFlag)
        {
            CharacterController slot = GetSlotAtPosition(position);
            if (slot != null)
            {
                slot.SetSprite(sprite, animFlag);
            }
        }

        public void ClearCharacterSlot(VNCharacterPosition position, string animFlag)
        {
            CharacterController slot = GetSlotAtPosition(position);
            if (slot != null)
            {
                slot.Clear(animFlag);
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

        private float GetDurationFromFlag(string animFlag)
        {
            if (_config == null) return 1.0f;
            if (animFlag.Contains("slow")) return _config.slowFadeDuration;
            if (animFlag.Contains("fast")) return _config.fastFadeDuration;
            return _config.normalFadeDuration;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            Color c = _backgroundImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                _backgroundImage.color = c;
                yield return null;
            }

            c.a = endAlpha;
            _backgroundImage.color = c;
        }
    }
