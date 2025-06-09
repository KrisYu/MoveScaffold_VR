using UnityEngine;
using System;

// The ScaffoldPoint is a tick point on line
[Serializable]
public class ScaffoldPointJSON 
{

    public int index;
    public float t;

    public ScaffoldPointJSON(int index, float t){
        this.index = index;
        this.t = t;
    }


}
