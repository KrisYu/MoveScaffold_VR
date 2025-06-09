using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


// 
// Attach to RightHand Controller
// Vibrate when collide with scaffolds
// 
public class HandHaptics : MonoBehaviour
{
    private InputDevice rightController;


    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> rightHandDevices = new List<InputDevice>();
        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, rightHandDevices);

        foreach (var item in rightHandDevices)
        {
            Debug.Log( item.name + item.characteristics);
        }

        if (rightHandDevices.Count > 0)
        {
            rightController = rightHandDevices[0];
        }        
    }


    //
    private void OnTriggerEnter(Collider other) 
    {
        //
        // The haptics will start under 2 conditions
        // 1. if the other gameObject has the ChangeColorWhenCollide component
        // 2. if the ChangeColorWhenCollide component can be selected
        // 

        var change = other.gameObject.GetComponent<ChangeColorWhenCollide>();
        if (change != null && change.canBeSelected == true)
        {
            UnityEngine.XR.HapticCapabilities capabilities;
            rightController.TryGetHapticCapabilities(out capabilities);
            if (capabilities.supportsImpulse)
            {
                    uint channel = 0;
                    float amplitude = 0.5f;
                    float duration = 0.3f;
                    rightController.SendHapticImpulse(channel, amplitude, duration);
            }
        }
    }



}
