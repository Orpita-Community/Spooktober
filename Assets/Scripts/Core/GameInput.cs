using UnityEngine;
using UnityEngine.InputSystem;

// Reads actions from the project-wide actions asset (InputSystem_Actions). All maps stay enabled,
// so callers decide when an action matters (dialogue open, menu open, transitioning...).
public static class GameInput
{
    public static InputAction Find(string actionPath)
    {
        InputActionAsset actions = InputSystem.actions;

        if (actions == null)
        {
            Debug.LogWarning("No project-wide input actions are set (Project Settings > Input System Package).");
            return null;
        }

        InputAction action = actions.FindAction(actionPath);

        if (action == null)
            Debug.LogWarning($"Input action '{actionPath}' was not found in '{actions.name}'.");

        return action;
    }

    public static bool WasPressed(InputAction action) => action != null && action.WasPressedThisFrame();
    public static bool IsPressed(InputAction action) => action != null && action.IsPressed();
}
