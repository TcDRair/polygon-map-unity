using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Maps
{
public class Center
{
  public int index;
  public Vector2 point;   // location
  public bool water;      // lake or ocean
  public bool ocean;      // ocean
  public bool coast;      // land polygon touching an ocean
  public bool border;     // at the edge of the map
  public Biome biome;     // biome type 
  public float elevation; // 0.0-1.0
  public float moisture;  // 0.0-1.0
  public List<Center> neighbors = new();
  public List<Edge> borders = new();
  public List<Corner> corners = new();
  
  public void Distinct() {
    neighbors = neighbors.Distinct().ToList();
    borders = borders.Distinct().ToList();
    corners = corners.Distinct().ToList();
  }

  public bool PointInside(float x, float y)
  {
    // http://alienryderflex.com/polygon/
    //  Globals which should be set before calling this function:
    //
    //  int  polyCorners  =  how many corners the polygon has
    //  float  polyX[]    =  horizontal coordinates of corners
    //  float  polyY[]    =  vertical coordinates of corners
    //  float  x, y     =  point to be tested
    //
    //  (Globals are used in this example for purposes of speed.  Change as
    //  desired.)
    //
    //  The function will return YES if the point x,y is inside the polygon, or
    //  NO if it is not.  If the point is exactly on the edge of the polygon,
    //  then the function may return YES or NO.
    //
    //  Note that division by zero is avoided because the division is protected
    //  by the "if" clause which surrounds it.
    var cp = corners[^1];
    bool oddNodes = false;

    foreach(var c in corners) {
      if (((c.Y < y && cp.Y >= y) || (c.Y < y && cp.Y >= y)) && (c.X <= x || cp.X <= x))
        oddNodes ^= c.X + (y - c.Y) / (cp.Y - c.Y) * (cp.X - c.X) < x;
      cp = c;
    }

    return oddNodes;
  }
}
}
