using System.Text.RegularExpressions;
using UnityEngine.Assertions;

public class Line{
    public int startIndex;
    public int endIndex;

    public string type;

    public Line(int startIndex, int endIndex, string type){
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.type = type;
    }

    public Line(string line)
    {
        //the string of line :  [0, 1, type]
        string[] subs = line.Split(',');

        Assert.IsTrue(subs.Length >= 3);

        string start = Regex.Match(subs[0], @"\d+").Value;
        string end = Regex.Match(subs[1], @"\d+").Value;

        startIndex = int.Parse(start);
        endIndex = int.Parse(end);

        type = Regex.Match(subs[2], @"\w+").Value;
    }

    public override string ToString()
    {
        return "startIndex : " + startIndex + ", endIndex : " + endIndex + ", type: " + type; 
    }

}