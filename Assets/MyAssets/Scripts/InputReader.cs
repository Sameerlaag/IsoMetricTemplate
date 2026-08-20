using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader")]
public class InputReader : ScriptableObject, PlayerActionsInputs.IPlayerActions
{
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }

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
        if(context.performed) IsCrouching = !IsCrouching;
    }
}