using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


public class Curve {
    
    public List<int> points;

    // The tangent is a list of [[line_index, line_weights],...]
    // one point tangent can be such a list
    public List<object> tangents;
    public List<float> magnitudes;
    public string type;

    public List<List<CurvePointTangent>> pointsTangents;

    public Curve(List<int> points, List<object> tangents, List<float> magintudes, string type)
    {
        this.points = points;
        this.tangents = tangents;
        this.magnitudes = magintudes;
        this.type = type;


        ParseTangents( tangents );
    }


    // Parse a one tangent

    public void ParseTangents(List<object> tangents)
    {
        // pointsTangents: all the points tangent for the curve
        pointsTangents = new List<List<CurvePointTangent>>();


        for (int i = 0; i < tangents.Count; i++)
        {
            

            string tangentString = tangents[i].ToString();

            tangentString = tangentString.Replace("[", string.Empty);
            tangentString = tangentString.Replace("]", string.Empty);
            
            string[] subs = tangentString.Split(',');

            Debug.Log("i " + i);

            Debug.Log("ParseTangent " + subs.Length);

            Assert.IsTrue( subs.Length % 2 == 0);

            // pointTangents: a point tangents
            List<CurvePointTangent> pointTangents = new List<CurvePointTangent>();

            for (int j = 0; j < subs.Length; j = j+2)
            {
                int line_index = int.Parse(subs[j]);
                float line_weight = float.Parse(subs[j+1]);

                CurvePointTangent pointTangent = new CurvePointTangent(line_index, line_weight);

                pointTangents.Add( pointTangent );
            }

            //
            pointsTangents.Add( pointTangents );
        }

    }




}
