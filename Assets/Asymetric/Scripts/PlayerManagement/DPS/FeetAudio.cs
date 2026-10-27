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

    private float _stepTimer;

    public void PlayFootsteps(bool isGrounded, bool isSprinting, bool isAiming, float speed)
    {
        // No audio if not grounded or basically standing still
        if (!isGrounded || speed < 0.1f)
        {
            _stepTimer = 0f;
            return;
        }

        // Determine interval and clip array based on the current movement state
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

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= currentInterval)
        {
            _stepTimer = 0f;

            // Pick a random clip from the array if it contains elements
            if (currentArray != null && currentArray.Length > 0 && _feetAudioSource != null)
            {
                AudioClip randomClip = currentArray[Random.Range(0, currentArray.Length)];
                if (randomClip != null)
                {
                    _feetAudioSource.PlayOneShot(randomClip);
                }
            }
        }
    }
}