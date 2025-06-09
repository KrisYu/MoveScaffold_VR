using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stroke 
{

    public List<float> knots;
    public List<List<float>> weights;
    public int degree; // this is 3
    public int n; // # number of points


    public Stroke(List<float> knots, List<List<float>> weights, int degree, int n) {
        this.knots = knots;
        this.weights = weights;
        this.degree = degree;
        this.n = n;

    }

}
