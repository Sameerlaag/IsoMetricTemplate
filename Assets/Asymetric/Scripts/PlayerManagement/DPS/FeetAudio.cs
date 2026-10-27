
using UnityEngine;

public class FeetAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _feetAudioSource;

    [SerializeField] private AudioClip[] _aiming;
    [SerializeField] private AudioClip[] _walking;
    [SerializeField] private AudioClip[] _running;

    [Header("Step Intervals")]
    [SerializeField] private float _aimStepInterval = 0.8f;
    [SerializeField] private float _walkStepInterval = 0.5f;
    [SerializeField] private float _runStepInterval = 0.3f;

    [Header("Monster Movement")]
    [SerializeField] private float _runSpeedThreshold = 2.5f;

    private float _stepTimer;

    // Existing player-compatible API.
    public void PlayFootsteps(
        bool isGrounded,
        bool isSprinting,
        bool isAiming,
        float speed)
    {
        UpdateFootsteps(
            isGrounded,
            isSprinting,
            isAiming,
            speed
        );
    }

    // Call this from MonsterController.
    public void PlayMonsterFootsteps(
        bool isGrounded,
        float speed)
    {
        bool isMovingFast = speed >= _runSpeedThreshold;

        UpdateFootsteps(
            isGrounded,
            isMovingFast,
            false,
            speed
        );
    }

    private void UpdateFootsteps(
        bool isGrounded,
        bool isSprinting,
        bool isAiming,
        float speed)
    {
        if (!isGrounded || speed < 0.1f)
        {
            _stepTimer = 0f;
            return;
        }

        float currentInterval;
        AudioClip[] currentArray;

        if (isAiming)
        {
            currentInterval = _aimStepInterval;
            currentArray = _aiming;
        }
        else if (isSprinting)
        {
            currentInterval = _runStepInterval;
            currentArray = _running;
        }
        else
        {
            currentInterval = _walkStepInterval;
            currentArray = _walking;
        }

        if (currentInterval <= 0f)
            return;

        _stepTimer += Time.deltaTime;

        if (_stepTimer >= currentInterval)
        {
            _stepTimer -= currentInterval;

            if (currentArray == null ||
                currentArray.Length == 0 ||
                _feetAudioSource == null)
            {
                return;
            }

            AudioClip clip = currentArray[
                Random.Range(0, currentArray.Length)
            ];

            if (clip != null)
                _feetAudioSource.PlayOneShot(clip);
        }
    }
}
