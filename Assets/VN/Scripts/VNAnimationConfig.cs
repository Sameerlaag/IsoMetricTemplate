using UnityEngine;


    [CreateAssetMenu(fileName = "VNAnimationConfig", menuName = "UI-VN/VN Animation Config")]
    public class VNAnimationConfig : ScriptableObject
    {
        [Header("Fade Speeds")]
        public float slowFadeDuration = 1.5f;
        public float fastFadeDuration = 0.5f;
        public float normalFadeDuration = 1.0f;

        [Header("Dialogue Typewriter")]
        public float typewriterSpeed = 0.03f;
    }

