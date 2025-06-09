using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Text.RegularExpressions;
using System.Linq;

public class State 
{
    public List<Point> points;
    public List<Line> lines;
    public List<Curve> curves;

    public List<Stroke> strokes;

    public State(List<Point> points, List<Line> lines, List<Curve> curves, List<Stroke> strokes)
    {
        this.points = points;
        this.lines = lines;
        this.curves = curves; 
        this.strokes = strokes;
    }


    public Vector3 point_i_position(int i){

        Point point = points[i];
        if (point.type == "vertex")
        {
            return new Vector3(point.x, point.y, point.z);
        } 
        else
        // a tick
        {
            float t = point.x;
            
            // make sure this float is int
            Assert.IsTrue( point.y == Math.Floor(point.y) );
            Assert.IsTrue( point.z == Math.Floor(point.z) );

            int i0 = (int) point.y;
            int i1 = (int) point.z;

            
            Vector3 p0 = point_i_position(i0);
            Vector3 p1 = point_i_position(i1);

            // https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html
            return Vector3.Lerp(p0, p1, t);

        }
    }

    public Vector3[] line_i_endpoints(int i){
        Vector3[] endpoints = new Vector3[2];

        int i0 = lines[i].startIndex;
        int i1 = lines[i].endIndex;

        Vector3 p0 = point_i_position(i0);
        Vector3 p1 = point_i_position(i1);

        endpoints[0] = p0;
        endpoints[1] = p1;

        return endpoints;
    }

    public Vector3 line_i_direction(int i){
        Vector3[] endpoints = line_i_endpoints(i);

        Vector3 start = endpoints[0];
        Vector3 end = endpoints[1];

        return (end - start).normalized;
    }


    // given the index of curve, calculate the points
    public List<Vector3> curve_i_points(int index){

        Curve curve = curves[index];

        if (curve.type == "straight_line")
        {
            List<Vector3> curve_points = new List<Vector3>();
            foreach( int curve_point_index in curve.points){
                Vector3 position = point_i_position( curve_point_index );
                curve_points.Add( position );
            }
            return curve_points;
        } 
        else
        {
            List<Vector3> curve_key_points = new List<Vector3>();
            List<Vector3> curve_key_tangents = new List<Vector3>();

            foreach( int curve_point_index in curve.points){
                Vector3 position = point_i_position( curve_point_index );
                curve_key_points.Add( position );
            }

 
     
            // curve.pointsTangents.Count == curve_key_points.Count
            for (int i = 0; i < curve.pointsTangents.Count; i++)
            {

                var curve_key_tangent = Vector3.zero; 
                var point_i_tangents = curve.pointsTangents[i];

                for (int j = 0; j < point_i_tangents.Count; j++)
                {

                    var point_tangent = point_i_tangents[j];

                    var line_index = point_tangent.line_index;
                    var line_weight = point_tangent.line_weight;

                    curve_key_tangent += line_i_direction(line_index) * line_weight;
                }

                curve_key_tangents.Add( curve_key_tangent );
            }

            return bezier_spline( curve.magnitudes, curve_key_points, curve_key_tangents);
        }
    }


    public override string ToString()
    {
        return "state has " + points.Count + " points, " + lines.Count + " lines.";
    }

    // move Point
    public void move_point(int i, Vector3 pos, float t = 0){

        if (points[i].type == "vertex" )
        {
            points[i].x = pos.x;
            points[i].y = pos.y;
            points[i].z = pos.z;
        } 
        else
        {
            // this a tick 
            points[i].x = t;
        }
    }

    // move Line

    public void move_line(int i, Vector3 s, Vector3 e){
        
        int i0 = lines[i].startIndex;
        int i1 = lines[i].endIndex;
        
        Point p0 = points[i0];
        Point p1 = points[i1];

        if (p0.type == "vertex" && p1.type == "vertex")
        {
            points[i0].x = s.x;
            points[i0].y = s.y;
            points[i0].z = s.z;

            points[i1].x = e.x;
            points[i1].y = e.y;
            points[i1].z = e.z;
        } else
        {
            // haven't decide how to move tick
            throw new NotImplementedException();
        }
    }


    
    
    // helper function
    // generate bezier curve points from control points
    private List<Vector3> bezier_curve(Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3, int numPoints)
    {
        
        List<Vector3> points = new List<Vector3>();
        float t = 0f;

        for (int i = 0; i < numPoints; i++)
        {
            Vector3 point = (1 - t) * (1 - t) * (1 - t) * c0 
            + 3 * (1 - t) * (1 - t) * t * c1 
            + 3 * (1 - t) * t * t * c2 
            + t * t * t * c3;

            points.Add( point );
            
            t += (1 / (float) numPoints);
        }
        points.Add( c3 );

        return points;
    }
    
    // X : k = len(points) * 2 - 2, points, tangents 
    private List<Vector3> bezier_spline(List<float> X, List<Vector3> points, List<Vector3> tangents, float resolution = 0.008f)
    {

        List<Vector3> splinePoints = new List<Vector3>();

        int k = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 c0 = points[i];
            Vector3 c3 = points[i+1];
            Vector3 c1 = c0 + tangents[i] * X[k];
            Vector3 c2 = c3 - tangents[i+1] * X[k+1];

            k = k + 2;

            // distance between c0 and c3
            float d = (c3 - c0).magnitude;            
            int numPoints = Mathf.FloorToInt( d / resolution );

            List<Vector3> segmentPoints = bezier_curve(c0, c1, c2, c3, numPoints);

            splinePoints.AddRange(segmentPoints);
        }

        return splinePoints;
    }

    // For the below methods, very naive - no optimized code

    public Vector3[] stroke_i_control_points(int index){

        List<List<float>> floats_list = strokes[index].weights;

        // Debug.Log(floats_list);
        // Debug.Log(floats_list.Count);
        // Debug.Log(floats_list[0].Count);

        var matWeights = NestedFloatsListToMultiArray(floats_list);

        // Debug.Log("matWeights");
        // PrintFloat2DArray(matWeights);


        List<Vector3> points_list = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            if ( points[i].type == "vertex")
            {
                points_list.Add( point_i_position(i) ); 
            } 
        }

        // Debug.Log("matPoints");
        var matPoints = ListOfVector3ToMatrix(points_list);
        // Transpose to the correct dimension
        var matPointsT = Transpose(matPoints);

        // PrintFloat2DArray(matPointsT);

        var matWeightsT = Transpose( matWeights );

        var matStroke = MatrixMultiply(matPointsT, matWeightsT);
        
        // PrintFloat2DArray(matStroke);

        var matStrokeT = Transpose(matStroke);

        var stroke_control_points = MatrixToVector3Array(matStrokeT);

        return stroke_control_points;

    }


    private float[,] NestedFloatsListToMultiArray( List<List<float>> list)
    {

        float[,] res;
        int r = list.Count;
        if (r < 1)
        {
            res = null;
        }
        else
        {
            int c = list[0].Count;

            res = new float[r, c];
            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    res[i, j] = list[i][j];
                }
            }
        }
        return res;        
    }

    

    private float[,] ListOfVector3ToMatrix(List<Vector3> list)
    {
        float[,] res;
        int r = list.Count;

        if (r < 1)
        {
            res = null;
        } 
        else
        {
            res = new float[r, 3];

            for (int i = 0; i < r; i++)
            {
                var point_value = list[i];

                res[i, 0] = point_value.x;
                res[i, 1] = point_value.y;
                res[i, 2] = point_value.z;
            }
            
        }
        return res;

    }


    private float[,] MatrixMultiply(float[,] matA, float[,] matB)
    {
        float[,] res;
        int rowsA = matA.GetLength(0);
        int columnsA = matA.GetLength(1);
        int rowsB = matB.GetLength(0);
        int columnsB = matB.GetLength(1);


        if ( columnsA != rowsB)
        {
            res = null;
        } 
        else
        {
            res = new float[rowsA, columnsB];

            for (int a = 0; a < columnsB; a++)
            {
                for (int i = 0; i < rowsA; i++)
                {
                    float sum = 0;
                    for (int j = 0; j < columnsA; j++)
                    {
                        sum += matA[i, j] * matB[j, a];
                    }
                    res[i, a] = sum;
                }
            }
        }
        return res;
    }

    private Vector3[] MatrixToVector3Array(float [,] mat){
        Vector3[] res;
        int rowsA = mat.GetLength(0);
        int columnsA = mat.GetLength(1);

        // Debug.Log("rowsA " + rowsA);
        // Debug.Log("columnsA " + columnsA);


        if (columnsA != 3)
        {
            res = null;
        }
        else
        {
            res = new Vector3[rowsA];

            for (int i = 0; i < rowsA; i++)
            {
                res[i].x = mat[i, 0];
                res[i].y = mat[i, 1];
                res[i].z = mat[i, 2]; 
            }
        }

        return res;

    }


    private float[,] Transpose(float[,] matrix)
    {
        int w = matrix.GetLength(0);
        int h = matrix.GetLength(1);

        float[,] result = new float[h, w];

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                result[j, i] = matrix[i, j];
            }
        }

        return result;
    }



    private void PrintFloat2DArray(float[,] matrix)
    {

        int row = matrix.GetLength(0);
        int column = matrix.GetLength(1);

        Debug.Log("row " + row);
        Debug.Log("column " + column);

    }

    public List<Vector3> stroke_i_points(int index)
    {
        var cps = stroke_i_control_points(index);


        var degree = strokes[index].degree ;
        var controlPoints = new List<Vector3>(cps);
        var knots = strokes[index].knots;
        var n = strokes[index].n;
        // how about decrease n


        var points = GenerateBSpline(controlPoints, knots, degree, n);

        // Debug.Log( index );
        // Debug.Log( points.Count );

        // for (int i = 0; i < points.Count; i++)
        // {
        //     Debug.Log( points[i]);
            
        // }

        return points; 

        

    }

    public List<Vector3> GenerateBSpline(List<Vector3> controlPoints, List<float> knots, int degree, int steps)
    {
        List<Vector3> newPoints = new List<Vector3>();
        float inc = 1.0f / steps;
        //degree = controlPoints.Count;
        // # of Knots = m + 1 = p + n + 1
        //              m + 1 = (Control Points - 1) + Degree + 1
        //              m + 1 = Control Points + Degree
        int knotCount = controlPoints.Count + degree + 1;

        Debug.Assert( knots.Count == controlPoints.Count + degree + 1);



        for (float t = 0.0f; t < 1.0f; t += inc)
        {
            // Debug.Log("t " + t);
            Vector3 v = GetBSplinePoint(t, knots, controlPoints, degree);
            newPoints.Add(v);
        }

        // Add a point close to 1
        var last_v = GetBSplinePoint(1.0f - 1e-5f, knots, controlPoints, degree);
        newPoints.Add(last_v);

        return newPoints;
    }


    //
    // wikipedia: https://en.wikipedia.org/wiki/De_Boor%27s_algorithm
    // attention the parameters try to match the wikipedia page
    //
    private Vector3 GetBSplinePoint(float x, List<float> t, List<Vector3> c, int p)
    {
        // find the k : the index of knot interval that contains x
        int k = 0;

        for (int m = 0; m < t.Count - 1; m++)
        {
            if (t[m] <= x && x < t[m+1])
            {
                k = m;
                break;
            }    
        }

        // Debug.Log("k "+ k);

        List<Vector3> d = new List<Vector3>();

        for (int j = 0; j < p+1; j++)
        {
            d.Add(c[j+k-p]);
        }

        for (int r = 1; r < p+1; r++)
        {
            for (int j = p; j > r-1; j--)
            {
                float alpha = ( x - t[j+k-p]) / (t[j+1+k-r] - t[j+k-p]);
                d[j] = (1.0f - alpha) * d[j-1] + alpha * d[j];
            }
            
        }

        return d[p];

    }





}
