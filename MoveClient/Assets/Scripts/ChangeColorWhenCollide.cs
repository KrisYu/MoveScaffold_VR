// Attach this scaffolds cylinder
// selected, turn red
// deselected, back to gray
// when the scaffolds is moving, cannot be selected

using UnityEngine;


public class ChangeColorWhenCollide : MonoBehaviour
{

    private bool _isSelected = false;
    private bool _isDetailMode = false;
    public bool canBeSelected = true;


    public bool isSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
            var renderer = GetComponent<Renderer>();
            if (_isSelected == true)
            {
                renderer.material.SetColor("_Color", Color.red);
            } else {
                renderer.material.SetColor("_Color", Color.gray);

            }
        }
    }

    public bool isDetailMode {
        get { return _isDetailMode; }
        set{
            _isDetailMode = value;
            var renderer = GetComponent<Renderer>();
            if (_isSelected == true)
            {
                renderer.material.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.3f));
            } else {
                renderer.material.SetColor("_Color", Color.gray);

            }
        }
    }
    
    // Move collide to select here
    private void OnTriggerEnter(Collider other) 
    {

        // only allow selection/deselection when the canBeSelected is true
        if ( canBeSelected == true )
        {
            isSelected = !isSelected;
        }
    }

}