using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class DialogueBoxController : MonoBehaviour
    {
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private TextMeshProUGUI _talker;
        [SerializeField] private TextMeshProUGUI _dialogue;
        [SerializeField] private Button _nextButton;
        [SerializeField] private VNAnimationConfig _config;

        private Action _onNextClickedCallback;
        private Coroutine _typewriterCoroutine;
        private string _fullText = "";
        private bool _isTyping = false;

        private void Awake()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void ShowDialogue(string speaker, string text, AudioClip audioClip, Action onNextClicked)
        {
            _onNextClickedCallback = onNextClicked;
            _fullText = text ?? "";

            if (_rootObject != null) _rootObject.SetActive(true);
            else gameObject.SetActive(true);

            if (_talker != null) _talker.text = speaker;

            if (VNAudioManager.Instance != null)
            {
                if (audioClip != null)
                {
                    VNAudioManager.Instance.PlaySFX(audioClip);
                }
            }

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_fullText));
        }

        public void Hide()
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _isTyping = false;

            if (_rootObject != null) _rootObject.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void OnNextButtonClicked()
        {
            if (VNAudioManager.Instance != null)
            {
                VNAudioManager.Instance.PlayDefaultClickSFX();
            }

            if (_isTyping)
            {
                // First click while typing: auto show full text instantly
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                if (_dialogue != null)
                {
                    _dialogue.text = _fullText;
                    _dialogue.maxVisibleCharacters = _fullText.Length;
                }
                _isTyping = false;
            }
            else
            {
                // Second click when text is full: advance to next node
                _onNextClickedCallback?.Invoke();
            }
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            if (_dialogue != null)
            {
                _dialogue.text = text;
                _dialogue.maxVisibleCharacters = 0;

                float delay = _config != null ? _config.typewriterSpeed : 0.03f;
                int totalChars = text.Length;

                for (int i = 0; i <= totalChars; i++)
                {
                    _dialogue.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(delay);
                }
            }
            _isTyping = false;
        }
    }
}
