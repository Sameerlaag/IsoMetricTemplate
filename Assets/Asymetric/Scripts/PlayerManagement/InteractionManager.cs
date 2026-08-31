using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Input & Layer")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private LayerMask _interactableLayer;

    [Header("Detection Parameters")]
    [SerializeField] private float _detectionRadius = 3f;
    [SerializeField] private float _maxInteractionAngle = 90f;
    [Range(0f, 1f)]
    [SerializeField] private float _angleWeight = 0.6f;

    private readonly Collider[] _hitBuffer = new Collider[10];
    private readonly List<InteractableTarget> _detectedInteractables = new();

    // Active Hold State Variables
    private IInteractable _activeHoldInteractable;
    private float _currentHoldTimer;
    private bool _isHolding;

    public IInteractable CurrentInteractable => _detectedInteractables.Count > 0 ? _detectedInteractables[0].Interactable : null;

    private struct InteractableTarget
    {
        public IInteractable Interactable;
        public float Score;
    }

    private void OnEnable()
    {
        if (_inputReader == null) return;
        _inputReader.OnInteractStarted += HandleInteractStarted;
        _inputReader.OnInteractCanceled += HandleInteractCanceled;
    }

    private void OnDisable()
    {
        if (_inputReader == null) return;
        _inputReader.OnInteractStarted -= HandleInteractStarted;
        _inputReader.OnInteractCanceled -= HandleInteractCanceled;
    }

    private void Update()
    {
        ScanAndRankInteractables();
        ProcessHoldInteraction();
    }

    private void ProcessHoldInteraction()
    {
        if (!_isHolding || _activeHoldInteractable == null) return;

        // Reset if player walks away mid-hold
        if (_activeHoldInteractable != CurrentInteractable)
        {
            CancelCurrentHold();
            return;
        }

        _currentHoldTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_currentHoldTimer / _activeHoldInteractable.HoldDuration);
        
        _activeHoldInteractable.OnHoldProgress(progress);

        if (_currentHoldTimer >= _activeHoldInteractable.HoldDuration)
        {
            _activeHoldInteractable.Interact(gameObject);
            _activeHoldInteractable.OnHoldProgress(0f);
            ResetHoldState();
        }
    }

    private void HandleInteractStarted()
    {
        IInteractable target = CurrentInteractable;
        if (target == null) return;

        if (!target.RequiresHold)
        {
            target.Interact(gameObject);
        }
        else
        {
            _activeHoldInteractable = target;
            _currentHoldTimer = 0f;
            _isHolding = true;
            _activeHoldInteractable.OnHoldStarted(gameObject);
        }
    }

    private void HandleInteractCanceled()
    {
        if (_isHolding)
        {
            CancelCurrentHold();
        }
    }

    private void CancelCurrentHold()
    {
        if (_activeHoldInteractable != null)
        {
            _activeHoldInteractable.OnHoldCanceled(gameObject);
            _activeHoldInteractable.OnHoldProgress(0f);
        }
        ResetHoldState();
    }

    private void ResetHoldState()
    {
        _isHolding = false;
        _activeHoldInteractable = null;
        _currentHoldTimer = 0f;
    }

    private void ScanAndRankInteractables()
    {
        _detectedInteractables.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _hitBuffer, _interactableLayer);

        Vector3 forward = transform.forward;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i].TryGetComponent<IInteractable>(out var interactable))
            {
                Vector3 dirToTarget = (interactable.InteractionPosition - playerPos);
                dirToTarget.y = 0;

                float distance = dirToTarget.magnitude;
                float angle = Vector3.Angle(forward, dirToTarget);

                if (angle > _maxInteractionAngle) continue;

                float normDistance = Mathf.Clamp01(distance / _detectionRadius);
                float normAngle = Mathf.Clamp01(angle / _maxInteractionAngle);
                float compositeScore = (normDistance * (1f - _angleWeight)) + (normAngle * _angleWeight);

                _detectedInteractables.Add(new InteractableTarget { Interactable = interactable, Score = compositeScore });
            }
        }

        _detectedInteractables.Sort((a, b) => a.Score.CompareTo(b.Score));
    }
}