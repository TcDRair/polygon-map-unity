using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;

namespace Delaunay
{
  // @author ashaw
  public sealed class Edge
  {
    private static readonly Stack<Edge> _pool = new();

    public static Edge CreateBisectingEdge(Site site0, Site site1)
    {
      Edge edge = Create();
      
      edge.leftSite = site0;
      edge.rightSite = site1;
      site0.AddEdge(edge);
      site1.AddEdge(edge);
      edge.LeftVertex = null;
      edge.RightVertex = null;

      float dx = site1.x - site0.x, dy = site1.y - site0.y, d = dx/dy;
      var c = site0.x * dx + site0.y * dy + (dx * dx + dy * dy) * 0.5f;
      if (Mathf.Abs(d) > 1) {
        edge.a = 1;
        edge.b = 1/d;
        edge.c = c/dx;
      }
      else {
        edge.a = d;
        edge.b = 1;
        edge.c = c/dy;
      }
      //trace("createBisectingEdge: a ", edge.a, "b", edge.b, "c", edge.c);
        
      return edge;
    }

    private static Edge Create() => _pool.TryPop(out var e) ? e : new();
    
    /*
    //    private static const LINESPRITE:Sprite = new Sprite();
    //    private static const GRAPHICS:Graphics = LINESPRITE.graphics;
    //    
    //    private var _delaunayLineBmp:BitmapData;
    //    internal function get delaunayLineBmp():BitmapData
    //    {
    //      if (!_delaunayLineBmp)
    //      {
    //        _delaunayLineBmp = makeDelaunayLineBmp();
    //      }
    //      return _delaunayLineBmp;
    //    }
    //    
    //    // making this available to Voronoi; running out of memory in AIR so I cannot cache the bmp
    //    internal function makeDelaunayLineBmp():BitmapData
    //    {
    //      var p0:Point = leftSite.coord;
    //      var p1:Point = rightSite.coord;
    //      
    //      GRAPHICS.clear();
    //      // clear() resets line style back to undefined!
    //      GRAPHICS.lineStyle(0, 0, 1.0, false, LineScaleMode.NONE, CapsStyle.NONE);
    //      GRAPHICS.moveTo(p0.x, p0.y);
    //      GRAPHICS.lineTo(p1.x, p1.y);
    //            
    //      var w:int = int(Math.ceil(Math.max(p0.x, p1.x)));
    //      if (w < 1)
    //      {
    //        w = 1;
    //      }
    //      var h:int = int(Math.ceil(Math.max(p0.y, p1.y)));
    //      if (h < 1)
    //      {
    //        h = 1;
    //      }
    //      var bmp:BitmapData = new BitmapData(w, h, true, 0);
    //      bmp.draw(LINESPRITE);
    //      return bmp;
    //      }
    */

    public LineSegment DelaunayLine() => new(leftSite.Coord, rightSite.Coord);
    public LineSegment VoronoiEdge() => new(leftEnd, rightEnd);

    private static int _nedges = 0;
      
    public static readonly Edge DELETED = new();
      
    // the equation of the edge: ax + by = c
    public float a, b, c;
      
    // the two Voronoi vertices that the edge connects
    //    (if one of them is null, the edge extends to infinity)
    public Vertex LeftVertex { get; private set; }
    public Vertex RightVertex { get; private set; }
    public Vertex Vertex(bool left) => left ? LeftVertex : RightVertex;
    public void SetVertex (bool left, Vertex v) {
      if (left) LeftVertex = v;
      else RightVertex = v;
    }
      
    public bool IsPartOfConvexHull() => LeftVertex == null || RightVertex == null;

    public float Distance => Vector2.Distance(leftSite.Coord, rightSite.Coord);
      
    // Once clipVertices() is called, this Dictionary will hold two Points
    // representing the clipped coordinates of the left and right ends...
    public Vector2 leftEnd, rightEnd;
    public Vector2 GetEnd(bool left) => left ? leftEnd : rightEnd;
    public bool Visible { get; private set; }
    public Site leftSite;
    public Site rightSite;

    public Site Site(bool left) => left ? leftSite : rightSite;
      
    private readonly int _edgeIndex;
      
    public void Dispose() {
      LeftVertex = null;
      RightVertex = null;
      leftSite = rightSite = null;
      Visible = false;
        
      _pool.Push(this);
    }

    private Edge() { _edgeIndex = _nedges++; }
      
    public override string ToString () => $"Edge {_edgeIndex}: sites {leftSite}, {rightSite}, endVertices {LeftVertex?.VIndex}, {RightVertex?.VIndex}";

    /**
       * Set _clippedVertices to contain the two ends of the portion of the Voronoi edge that is visible
       * within the bounds.  If no part of the Edge falls within the bounds, leave _clippedVertices null. 
       * @param bounds
       * 
       */
    public void ClipVertices (Rect bounds)
    {
      float xmin = bounds.xMin;
      float ymin = bounds.yMin;
      float xmax = bounds.xMax;
      float ymax = bounds.yMax;
        
      Vertex vertex0, vertex1;
      float x0, x1, y0, y1;
        
      if (a == 1.0 && b >= 0.0) {
        vertex0 = RightVertex;
        vertex1 = LeftVertex;
      } else {
        vertex0 = LeftVertex;
        vertex1 = RightVertex;
      }
      
      if (a == 1.0) {
        y0 = ymin;
        if (vertex0 != null && vertex0.Y > ymin) {
          y0 = vertex0.Y;
        }
        if (y0 > ymax) {
          return;
        }
        x0 = c - b * y0;
          
        y1 = ymax;
        if (vertex1 != null && vertex1.Y < ymax) {
          y1 = vertex1.Y;
        }
        if (y1 < ymin) {
          return;
        }
        x1 = c - b * y1;
          
        if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
          return;
        }
          
        if (x0 > xmax) {
          x0 = xmax;
          y0 = (c - x0) / b;
        } else if (x0 < xmin) {
          x0 = xmin;
          y0 = (c - x0) / b;
        }
          
        if (x1 > xmax) {
          x1 = xmax;
          y1 = (c - x1) / b;
        } else if (x1 < xmin) {
          x1 = xmin;
          y1 = (c - x1) / b;
        }
      } else {
        x0 = xmin;
        if (vertex0 != null && vertex0.X > xmin) {
          x0 = vertex0.X;
        }
        if (x0 > xmax) {
          return;
        }
        y0 = c - a * x0;
          
        x1 = xmax;
        if (vertex1 != null && vertex1.X < xmax) {
          x1 = vertex1.X;
        }
        if (x1 < xmin) {
          return;
        }
        y1 = c - a * x1;
          
        if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
          return;
        }
          
        if (y0 > ymax) {
          y0 = ymax;
          x0 = (c - y0) / a;
        } else if (y0 < ymin) {
          y0 = ymin;
          x0 = (c - y0) / a;
        }
          
        if (y1 > ymax) {
          y1 = ymax;
          x1 = (c - y1) / a;
        } else if (y1 < ymin) {
          y1 = ymin;
          x1 = (c - y1) / a;
        }
      }

      if (vertex0 == LeftVertex) {
        leftEnd = new(x0, y0);
        rightEnd = new(x1, y1);
      } else {
        rightEnd = new(x0, y0);
        leftEnd = new(x1, y1);
      }

      Visible = true;
    }

  }
}

//class PrivateConstructorEnforcer {}