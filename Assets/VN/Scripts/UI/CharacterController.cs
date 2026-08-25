using System.Collections;
using UnityEngine;
using UnityEngine.UI;


    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private VNCharacterPosition _position;
        [SerializeField] private string _characterID;
        [SerializeField] private Image _characterImage;
        [SerializeField] private VNAnimationConfig _config;

        public VNCharacterPosition Position => _position;
        public string CharacterID => _characterID;

        private Coroutine _fadeCoroutine;

        public void SetSprite(Sprite sprite, string animFlag)
        {
            if (_characterImage == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            _characterImage.sprite = sprite;

            if (sprite == null)
            {
                _characterImage.enabled = false;
                return;
            }

            _characterImage.enabled = true;

            if (!string.IsNullOrEmpty(animFlag) && animFlag.Contains("fade_in"))
            {
                float duration = GetDurationFromFlag(animFlag);
                _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration));
            }
            else
            {
                Color c = _characterImage.color;
                c.a = 1f;
                _characterImage.color = c;
            }
        }

        public void Clear(string animFlag = null)
        {
            if (_characterImage == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            if (!string.IsNullOrEmpty(animFlag) && animFlag.Contains("fade_out"))
            {
                float duration = GetDurationFromFlag(animFlag);
                _fadeCoroutine = StartCoroutine(FadeRoutine(_characterImage.color.a, 0f, duration, disableOnEnd: true));
            }
            else
            {
                _characterImage.sprite = null;
                _characterImage.enabled = false;
            }
        }

        private float GetDurationFromFlag(string animFlag)
        {
            if (_config == null) return 1.0f;
            if (animFlag.Contains("slow")) return _config.slowFadeDuration;
            if (animFlag.Contains("fast")) return _config.fastFadeDuration;
            return _config.normalFadeDuration;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, bool disableOnEnd = false)
        {
            float elapsed = 0f;
            Color c = _characterImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                _characterImage.color = c;
                yield return null;
            }

            c.a = endAlpha;
            _characterImage.color = c;

            if (disableOnEnd && endAlpha <= 0f)
            {
                _characterImage.sprite = null;
                _characterImage.enabled = false;
            }
        }
    }
