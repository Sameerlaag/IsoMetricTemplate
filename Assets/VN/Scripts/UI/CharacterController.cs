using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private VNCharacterPosition _position;
        [SerializeField] private string _characterID;
        [SerializeField] private Image _characterImage;
        [SerializeField] private VNAnimationConfig _config;

        public VNCharacterPosition Position => _position;
        public string CharacterID => _characterID;

        private Coroutine _fadeCoroutine;
        private bool _isAnimating = false;

        private void Awake()
        {
            _characterImage.enabled = false;
        }

        public void SetSprite(Sprite sprite, string animFlag, bool allowSkip, Action onComplete)
        {
            if (_characterImage == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            _characterImage.sprite = sprite;

            if (sprite == null)
            {
                _characterImage.enabled = false;
                onComplete?.Invoke();
                return;
            }

            _characterImage.enabled = true;

            if (!string.IsNullOrEmpty(animFlag) && animFlag.Contains("fade_in"))
            {
                float duration = GetDurationFromFlag(animFlag);
                _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, duration, allowSkip, false, onComplete));
            }
            else
            {
                Color c = _characterImage.color;
                c.a = 1f;
                _characterImage.color = c;
                onComplete?.Invoke();
            }
        }

        public void Clear(string animFlag, bool allowSkip, Action onComplete)
        {
            if (_characterImage == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            if (!string.IsNullOrEmpty(animFlag) && animFlag.Contains("fade_out"))
            {
                float duration = GetDurationFromFlag(animFlag);
                _fadeCoroutine = StartCoroutine(FadeRoutine(_characterImage.color.a, 0f, duration, allowSkip, true, onComplete));
            }
            else
            {
                _characterImage.sprite = null;
                _characterImage.enabled = false;
                onComplete?.Invoke();
            }
        }

        private float GetDurationFromFlag(string animFlag)
        {
            if (_config == null) return 1.0f;
            if (animFlag.Contains("slow")) return _config.slowFadeDuration;
            if (animFlag.Contains("fast")) return _config.fastFadeDuration;
            return _config.normalFadeDuration;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, bool allowSkip, bool disableOnEnd, Action onComplete)
        {
            _isAnimating = true;
            float elapsed = 0f;
            Color c = _characterImage.color;

            while (elapsed < duration)
            {
                // If skipping is enabled and player presses skip/click button
                if (allowSkip && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    Debug.Log("[CharacterController] Skipping character fade animation.");
                    break;
                }

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

            _isAnimating = false;
            onComplete?.Invoke();
        }
    }
}
