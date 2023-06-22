using UnityEngine;
using System;

namespace Delaunay.Geo
{
  public sealed class LineSegment
  {
    public Vector2 p0;
    public Vector2 p1;
  
    public LineSegment (Vector2 p0, Vector2 p1) { this.p0 = p0; this.p1 = p1; }
  }
}