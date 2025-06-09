using UnityEngine;
using System;


[Serializable]
public class ScaffoldLineJSON {
    
    public int index;
    public Vector3 start;
    public Vector3 end;

    public ScaffoldLineJSON(int index, Vector3 start, Vector3 end){
        this.index = index;
        this.start = start;
        this.end = end;
    }
}