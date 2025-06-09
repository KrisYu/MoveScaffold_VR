// This is a pushable VR button
// Learn from https://youtu.be/lPPa9y_czlE
// Not used
using UnityEngine;
using UnityEngine.Events;

public class ButtonVR : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onPress;
    public UnityEvent onRelease;
    AudioSource sound;
    GameObject presser;
    private bool isPressed;

    // Start is called before the first frame update
    void Start()
    {

        isPressed = false;
        sound = GetComponent<AudioSource>();
        
    }


    private void OnTriggerEnter(Collider other) 
    {
        Debug.Log("pressed button");
        
        if (!isPressed)
        {
            button.transform.localPosition = new Vector3(0, 0.05f, 0);
            presser = other.gameObject;
            sound.Play();
            onPress.Invoke();
            isPressed = true;
        }

        
    
    }


    private void OnTriggerExit(Collider other) {
        if (other.gameObject == presser)
        {
            button.transform.localPosition = new Vector3(0, 0.1f, 0);
            onRelease.Invoke();
            isPressed = false;
        }
    }
}
