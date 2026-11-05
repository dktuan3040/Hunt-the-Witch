using UnityEngine;
using UnityEngine.InputSystem;

// Run after PauseMenuUI so the same Escape press cannot also open the pause menu.
[DefaultExecutionOrder(500),DisallowMultipleComponent]
public sealed class WorkbenchUnlockMenu : MonoBehaviour
{
    private void Update()
    {
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)gameObject.SetActive(false);
    }
}
