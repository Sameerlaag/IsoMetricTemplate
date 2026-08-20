using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader")]
public class InputReader : ScriptableObject, PlayerActionsInputs.IPlayerActions
{
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }

    public event Action OnInteractPerformed;
    public event Action OnInteractStarted;
    public event Action OnInteractCanceled;

    public event Action OnInventoryPerformed;

    public event Action OnMenuPerformed;

    public event Action OnEquipedItemPerformed;
    public event Action OnEquipedItemStarted;
    public event Action OnEquipedItemCanceled;

    public event Action OnReloadStarted;
    public event Action OnReloadCanceled;

    private PlayerActionsInputs _inputActions;

    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerActionsInputs();
            _inputActions.Player.SetCallbacks(this);
        }

        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started) IsSprinting = true;
        else if (context.canceled) IsSprinting = false;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed) IsCrouching = !IsCrouching;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed) OnInteractPerformed?.Invoke();
        else if (context.started) OnInteractStarted?.Invoke();
        else if (context.canceled) OnInteractCanceled?.Invoke();
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed) OnInventoryPerformed?.Invoke();
    }

    public void OnMenu(InputAction.CallbackContext context)
    {
        if (context.performed) OnMenuPerformed?.Invoke();
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.started) OnInteractStarted?.Invoke();
        else if (context.canceled) OnInteractCanceled?.Invoke();
    }

    public void OnEquiped(InputAction.CallbackContext context)
    {
        if (context.performed) OnEquipedItemPerformed?.Invoke();
        else if (context.started) OnEquipedItemStarted?.Invoke();
        else if (context.canceled) OnEquipedItemCanceled?.Invoke();
    }
}