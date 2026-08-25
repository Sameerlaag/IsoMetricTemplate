using System;
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
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _historyButton;

        private Action _onNextClickedCallback;

        private void Awake()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void ShowDialogue(string speaker, string text, Action onNextClicked)
        {
            _onNextClickedCallback = onNextClicked;

            if (_rootObject != null) _rootObject.SetActive(true);
            else gameObject.SetActive(true);

            if (_talker != null) _talker.text = speaker;
            if (_dialogue != null) _dialogue.text = text;
        }

        public void Hide()
        {
            if (_rootObject != null) _rootObject.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void OnNextButtonClicked()
        {
            _onNextClickedCallback?.Invoke();
        }
    }
}
