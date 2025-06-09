using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class StateParser 
{
    public State parse(string stateText)
    {
        var stateObject = JsonConvert.DeserializeObject<StateObject>(stateText);

        List<Line> lines = new List<Line>();
        List<Point> points = new List<Point>();
        List<Curve> curves = new List<Curve>();
        List<Stroke> strokes = new List<Stroke>();
        List<object> stroke_points = new List<object>();



        foreach (object lineObject in stateObject.lines)
        {
            var line = new Line(lineObject.ToString());

            // Debug.Log(line.startIndex);
            // Debug.Log(line.endIndex);
            // Debug.Log(line.type);

            lines.Add(line);
        }

        foreach (object pointObject in stateObject.points)
        {
            var point = new Point(pointObject.ToString());
            // Debug.Log( point.ToString());
            points.Add(point);
        }

        foreach( Curve curveObject in stateObject.curves)
        {
            
            var curve = new Curve(curveObject.points, curveObject.tangents, curveObject.magnitudes, curveObject.type);

            curves.Add( curve );

        }


        curves = stateObject.curves;

        strokes = stateObject.strokes;

        Debug.Log(stateObject.strokes.Count);

        return new State(points, lines, curves, strokes);
    }

   
}
