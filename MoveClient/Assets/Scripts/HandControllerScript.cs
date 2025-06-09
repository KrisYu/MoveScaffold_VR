// 
// All these functions can be put in the InitSketch script
// 
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;


//
// This attached to hand prefab, so the script can access the animator directly
// 
// The button use UnityEvent so I can setup the actual event in the inspector
// https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html
//
//  
// This Script controlls the hand animation and controller button press event
// https://docs.unity3d.com/Manual/xr_input.html
public class HandControllerScript : MonoBehaviour
{
    [SerializeField] InputActionReference primaryTouchAction;
    [SerializeField] InputActionReference aButtonPressedAction;

    [SerializeField] InputActionReference bButtonPressedAction;


    // can I try this?
    public UnityEvent onPressA;

    public UnityEvent onJoystickMoveLeft;
    public UnityEvent onJoystickMoveRight;



    private void OnEnable() 
    {

        primaryTouchAction.action.performed += PrimaryTouchPressed;
        aButtonPressedAction.action.performed += AButtonPressed;
        
    }

    private void OnDisable() 
    {

        primaryTouchAction.action.performed -= PrimaryTouchPressed;
        aButtonPressedAction.action.performed -= AButtonPressed;

    }



    private void PrimaryTouchPressed(InputAction.CallbackContext context)
    {
        Debug.Log("PrimaryTouch");
        Vector2 currentJoystickValue = context.ReadValue<Vector2>();
        Debug.Log( currentJoystickValue);

        if ( currentJoystickValue.x < 0)
        {
            Debug.Log(" undo ");
            onJoystickMoveLeft.Invoke();
        }
        else
        {
            Debug.Log(" redo ");
            onJoystickMoveRight.Invoke();
        }

    }

    private void AButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("A button pressed");
        onPressA.Invoke();
    }






}
