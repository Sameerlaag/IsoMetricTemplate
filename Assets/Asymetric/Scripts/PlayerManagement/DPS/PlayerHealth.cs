using System;
using UnityEngine;

/// <summary>
/// Minimal health component. The UI listens to OnHealthChanged.
/// If you already have a health system, just raise an equivalent event from it
/// and point UIController at that instead.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;

    public float Current { get; private set; }
    public float Max => _maxHealth;
    public bool IsDead => Current <= 0f;

    /// <summary>(current, max)</summary>
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        Current = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        OnHealthChanged?.Invoke(Current, _maxHealth);

        if (Current <= 0f) OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(_maxHealth, Current + amount);
        OnHealthChanged?.Invoke(Current, _maxHealth);
    }
}