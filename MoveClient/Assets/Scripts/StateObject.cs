using System.Collections.Generic;

// a wrapper for state
class StateObject
{
    // C Sharp JSON can only parse [ ['x' : float, 'y' : float ], ..]
    // not a [ [x,y] ... ]
    public List<object> points;
    public List<object> lines;
    public List<Curve> curves;

    public List<Stroke> strokes;

    // This will note be used
    public List<object> stroke_points;

}

