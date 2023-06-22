using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Delaunay.Geo;

namespace Delaunay
{
    
  public sealed class Site: ICoord, IComparable
  {
    private static readonly Stack<Site> _pool = new();
    public static Site Create(Vector2 p, uint index, float weight, uint color) =>
      _pool.TryPop(out var s)
      ? s.Init(p, index, weight, color)
      : new(p, index, weight, color);

    public int CompareTo(object obj) {
      // XXX: Really, really worried about this because it depends on how sorting works in AS3 impl - Julian
      Site s2 = (Site)obj;

      int returnValue = Voronoi.CompareYtoX(this, s2);
      
      // swap _siteIndex values if necessary to match new ordering:
      if (returnValue == -1 && _index > s2._index) (_index, s2._index) = (s2._index, _index);
      else if (returnValue == 1 && s2._index > _index) (_index, s2._index) = (s2._index, _index);
      
      return returnValue;
    }


    private static readonly float EPSILON = .005f;
    private static bool CloseEnough(Vector2 p0, Vector2 p1) => Vector2.Distance(p0, p1) < EPSILON;

    public Vector2 Coord { get; private set; }
    
    public uint color;
    public float weight;
    
    private uint _index;
    
    // the edges that define this Site's Voronoi region:
    internal List<Edge> Edges { get; private set; }
    // which end of each edge hooks up with the previous edge in _edges:
    private List<bool> EdgeOrn;
    // ordered list of points that define the region clipped to bounds:
    private List<Vector2> _region;

    private Site(Vector2 p, uint index, float weight, uint color) => Init(p, index, weight, color);
    
    private Site Init(Vector2 p, uint index, float weight, uint color) {
      Coord = p;
      _index = index;
      this.weight = weight;
      this.color = color;
      Edges = new();
      _region = null;
      return this;
    }
    
    public override string ToString() => $"Site {_index}: {Coord}";
    
    private void Move(Vector2 p) {
      Clear();
      Coord = p;
    }
    
    public void Dispose() {
      Clear();
      _pool.Push(this);
    }
    
    private void Clear() {
      if (Edges != null) {
        Edges.Clear();
        Edges = null;
      }
      if (EdgeOrn != null) {
        EdgeOrn.Clear();
        EdgeOrn = null;
      }
      if (_region != null) {
        _region.Clear();
        _region = null;
      }
    }
    
    public void AddEdge(Edge edge) => Edges.Add(edge);
    
    public Edge NearestEdge() => Edges.MinItem(e => e.Distance);
    
    public List<Site> NeighborSites() {
      if (Edges == null || Edges.Count == 0) {
        return new List<Site>();
      }
      if (EdgeOrn == null) { 
        ReorderEdges();
      }
      List<Site> list = new();
      Edge edge;
      for (int i = 0; i < Edges.Count; i++) {
        edge = Edges[i];
        list.Add(NeighborSite(edge));
      }
      return list;
    }
      
    private Site NeighborSite(Edge edge) {
      if (this == edge.leftSite) {
        return edge.rightSite;
      }
      if (this == edge.rightSite) {
        return edge.leftSite;
      }
      return null;
    }
    
    internal List<Vector2> Region(Rect clippingBounds) {
      if (Edges == null || Edges.Count == 0) {
        return new List<Vector2>();
      }
      if (EdgeOrn == null) { 
        ReorderEdges();
        _region = ClipToBounds(clippingBounds);
        if ((new Polygon(_region)).Winding() == Winding.CLOCKWISE) {
          _region.Reverse();
        }
      }
      return _region;
    }
    
    private void ReorderEdges() {
      //trace("_edges:", _edges);
      EdgeReorderer reorderer = new(Edges, VertexOrSite.VERTEX);
      Edges = reorderer.Edges;
      //trace("reordered:", _edges);
      EdgeOrn = reorderer.EdgeOrientations;
      reorderer.Dispose();
    }
    
    private List<Vector2> ClipToBounds(Rect bounds) {
      List<Vector2> points = new();
      int n = Edges.Count;
      int i = 0;
      while (i < n && (Edges[i].Visible == false)) ++i;
      
      if (i == n) {
        // no edges visible
        return new();
      }
      var rOrnEdge = Edges[i];
      var orn = EdgeOrn[i];
      Vector2 end1 = rOrnEdge.GetEnd(orn), end2 = rOrnEdge.GetEnd(!orn);
      points.Add(end1);
      points.Add(end2);
      
      for (int j = i + 1; j < n; ++j) {
        rOrnEdge = Edges[j];
        if (rOrnEdge.Visible == false) continue;
        Connect(points, j, bounds);
      }
      // close up the polygon by adding another corner point of the bounds if needed:
      Connect(points, i, bounds, true);
      
      return points;
    }
    
    private void Connect(List<Vector2> points, int j, Rect bounds, bool closingUp = false) {
      Vector2 rightPoint = points[^1];
      Edge newEdge = Edges[j];
      var newOrn = EdgeOrn[j];
      // the point that  must be connected to rightPoint:
      Vector2 newPoint = newEdge.GetEnd(newOrn);
      if (!CloseEnough(rightPoint, newPoint)) {
        // The points do not coincide, so they must have been clipped at the bounds;
        // see if they are on the same border of the bounds:
        if (rightPoint.x != newPoint.x && rightPoint.y != newPoint.y) {
          // They are on different borders of the bounds;
          // insert one or two corners of bounds as needed to hook them up:
          // (NOTE this will not be correct if the region should take up more than
          // half of the bounds rect, for then we will have gone the wrong way
          // around the bounds and included the smaller part rather than the larger)
          int rightCheck = BoundsCheck.Check(rightPoint, bounds);
          int newCheck = BoundsCheck.Check(newPoint, bounds);
          float px, py;
          if ((rightCheck & BoundsCheck.RIGHT) != 0) {
            px = bounds.xMax;
            if ((newCheck & BoundsCheck.BOTTOM) != 0) {
              py = bounds.yMax;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.TOP) != 0) {
              py = bounds.yMin;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.LEFT) != 0) {
              if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height) {
                py = bounds.yMin;
              } else {
                py = bounds.yMax;
              }
              points.Add(new Vector2(px, py));
              points.Add(new Vector2(bounds.xMin, py));
            }
          } else if ((rightCheck & BoundsCheck.LEFT) != 0) {
            px = bounds.xMin;
            if ((newCheck & BoundsCheck.BOTTOM) != 0) {
              py = bounds.yMax;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.TOP) != 0) {
              py = bounds.yMin;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.RIGHT) != 0) {
              if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height) {
                py = bounds.yMin;
              } else {
                py = bounds.yMax;
              }
              points.Add(new Vector2(px, py));
              points.Add(new Vector2(bounds.xMax, py));
            }
          } else if ((rightCheck & BoundsCheck.TOP) != 0) {
            py = bounds.yMin;
            if ((newCheck & BoundsCheck.RIGHT) != 0) {
              px = bounds.xMax;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.LEFT) != 0) {
              px = bounds.xMin;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.BOTTOM) != 0) {
              if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width) {
                px = bounds.xMin;
              } else {
                px = bounds.xMax;
              }
              points.Add(new Vector2(px, py));
              points.Add(new Vector2(px, bounds.yMax));
            }
          } else if ((rightCheck & BoundsCheck.BOTTOM) != 0) {
            py = bounds.yMax;
            if ((newCheck & BoundsCheck.RIGHT) != 0) {
              px = bounds.xMax;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.LEFT) != 0) {
              px = bounds.xMin;
              points.Add(new Vector2(px, py));
            } else if ((newCheck & BoundsCheck.TOP) != 0) {
              if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width) {
                px = bounds.xMin;
              } else {
                px = bounds.xMax;
              }
              points.Add(new Vector2(px, py));
              points.Add(new Vector2(px, bounds.yMin));
            }
          }
        }
        if (closingUp) {
          // newEdge's ends have already been added
          return;
        }
        points.Add(newPoint);
      }
      Vector2 newRightPoint = newEdge.GetEnd(!newOrn);
      if (!CloseEnough(points[0], newRightPoint)) {
        points.Add(newRightPoint);
      }
    }
                
    public float x => Coord.x;
    internal float y => Coord.y;
    
    public float Dist(ICoord p) => Vector2.Distance(p.Coord, this.Coord);
  }
}

//  class PrivateConstructorEnforcer {}

//  import flash.geom.Point;
//  import flash.geom.Rectangle;
  
static class BoundsCheck
{
  public static readonly int TOP = 1;
  public static readonly int BOTTOM = 2;
  public static readonly int LEFT = 4;
  public static readonly int RIGHT = 8;
    
  /**
     * 
     * @param point
     * @param bounds
     * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
     * 
     */
  public static int Check(Vector2 point, Rect bounds) {
    int value = 0;
    if (point.x == bounds.xMin) {
      value |= LEFT;
    }
    if (point.x == bounds.xMax) {
      value |= RIGHT;
    }
    if (point.y == bounds.yMin) {
      value |= TOP;
    }
    if (point.y == bounds.yMax) {
      value |= BOTTOM;
    }
    return value;
  }
}