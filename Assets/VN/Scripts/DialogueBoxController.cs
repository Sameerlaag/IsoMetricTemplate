using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBoxController : MonoBehaviour
    {
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _historyButton;
        [SerializeField] private TextMeshProUGUI _talker;
        [SerializeField] private TextMeshProUGUI _dialogue;
        [SerializeField] private GameObject _choice;
        [SerializeField] private Transform _choicesTransform;
        
    }
