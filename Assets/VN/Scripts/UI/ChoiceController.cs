using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class ChoiceController : MonoBehaviour
    {
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;

        public void ShowChoices(List<VNChoiceOption> choices, Action<int, string> onChoiceSelected)
        {
            if (_rootObject != null) _rootObject.SetActive(true);
            else gameObject.SetActive(true);

            foreach (Transform child in _choicesContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                int choiceIndex = i;

                GameObject btnObj = Instantiate(_choiceButtonPrefab, _choicesContainer);
                var btn = btnObj.GetComponent<Button>();
                var txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (txt != null) txt.text = choice.choiceText;

                btn.onClick.AddListener(() =>
                {
                    Hide();
                    onChoiceSelected?.Invoke(choiceIndex, choice.targetNodeId);
                });
            }
        }

        public void Hide()
        {
            if (_rootObject != null) _rootObject.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}