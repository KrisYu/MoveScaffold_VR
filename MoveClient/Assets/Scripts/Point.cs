using System.Text.RegularExpressions;
using UnityEngine.Assertions;
using UnityEngine;
using System;


/// 
/// I have to use the regular expression to parse the string
/// because C Sharp is strong type 
/// C Sharp JSON support : {'x': x, 'y': y, 'z': z, 'type': type}
/// Do not want to mix int and string in C Sharp Array/List [x, y, z, type]

///
///
/// Be aware depend on type:
/// 'vertex' : x, y, z -> real x, y, z value
/// 'tick' : t, i0, i1 -> i0, i1 index of the other points, lerp(t, position[i0], position[i1])
/// 
/// i0, i1 : only valid for the 'tick' type
/// 
/// ToVector3() : only valid for 'vertex' type
///

public class Point {

    public float x;
    public float y;
    public float z;
    public string type;

    // valid for the tick type
    public int i0;
    public int i1;

    public Point(float x, float y, float z, string type){
        this.x = x;
        this.y = y;
        this.z = z;
        this.type = type;
    }

    public Point(string point)
    {
        // the string of point :  [x, y, z, type]
        // remove brackets and then split
        // beaware, x, y, z are not necessary the positions
        point = point.Replace("[", string.Empty);
        point = point.Replace("]", string.Empty);
        string[] subs = point.Split(',');

        Assert.IsTrue( subs.Length == 4);
        // string xval = Regex.Match(subs[0], @"-?\d+\.?\d+").Value;
        // string yval = Regex.Match(subs[1], @"-?\d+\.?\d*").Value;
        // string zval = Regex.Match(subs[2], @"-?\d+\.?\d*").Value;

        // x = float.Parse(xval);
        // y = float.Parse(yval);
        // z = float.Parse(zval);

        x = float.Parse(subs[0]);
        y = float.Parse(subs[1]);
        z = float.Parse(subs[2]);

        type = Regex.Match(subs[3], @"\w+").Value;

        if (type == "tick")
        {
            Assert.IsTrue( y == Math.Floor(y) );
            Assert.IsTrue( z == Math.Floor(z) );

            i0 = (int) y;
            i1 = (int) z;
        }
    }

    // valid for vertex type
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }


    public override string ToString()
    {
        return "type: " + type + " , x : " + x + " , y : " + y + " ,z :" + z;
    }

}
