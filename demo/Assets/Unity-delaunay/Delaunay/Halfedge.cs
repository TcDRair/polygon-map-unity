using UnityEngine;
using System;
using System.Collections.Generic;

namespace Delaunay
{
  public sealed class Halfedge: Utils.IDisposable
  {
    private static readonly Stack<Halfedge> _pool = new();
    public static Halfedge Create(Edge edge, bool left = default)
      => _pool.TryPop(out var h)
      ? h.Init(edge, left)
      : new(edge, left);
    
    public static Halfedge Dummy => Create(null);
    
    public Halfedge leftEdge, rightEdge;
    public Halfedge nextInPriorityQueue;
    
    public Edge edge;
    public bool left;
    public Vertex vertex;
    
    // the vertex's y-coordinate in the transformed Voronoi space V*
    public float ystar;

    public Halfedge(Edge edge = null, bool left = default) => Init(edge, left);
    
    private Halfedge Init(Edge edge, bool left) {
      this.edge = edge;
      this.left = left;
      nextInPriorityQueue = null;
      vertex = null;
      return this;
    }
    
    public override string ToString() => $"Halfedge (left: {left}, vertex: {vertex})";
    
    public void Dispose()
    {
      // don't dispose this in EdgeList or PriorityQueue
      if (leftEdge != null || rightEdge != null || nextInPriorityQueue != null) return;
      edge = null;
      vertex = null;
      _pool.Push(this);
    }
    
    public void ReallyDispose()
    {
      leftEdge = null;
      rightEdge = null;
      nextInPriorityQueue = null;
      edge = null;
      vertex = null;
      _pool.Push(this);
    }

    internal bool IsLeftOf(Vector2 p)
    {
      Site topSite;
      bool rightOfSite, above, fast;
      float dxp, dyp, dxs, t1, t2, t3, yl;
      
      topSite = edge.rightSite;
      rightOfSite = p.x > topSite.x;
      if (rightOfSite && left) return true;
      if (!rightOfSite && !left) return false;
      
      if (edge.a == 1.0) {
        dyp = p.y - topSite.y;
        dxp = p.x - topSite.x;
        fast = false;
        if ((!rightOfSite && edge.b < 0.0) || (rightOfSite && edge.b >= 0.0)) {
          above = dyp >= edge.b * dxp;  
          fast = above;
        } else {
          above = p.x + p.y * edge.b > edge.c;
          if (edge.b < 0.0) {
            above = !above;
          }
          if (!above) {
            fast = true;
          }
        }
        if (!fast) {
          dxs = topSite.x - edge.leftSite.x;
          above = edge.b * (dxp * dxp - dyp * dyp) <
            dxs * dyp * (1.0 + 2.0 * dxp / dxs + edge.b * edge.b);
          if (edge.b < 0.0) {
            above = !above;
          }
        }
      } else {  /* edge.b == 1.0 */
        yl = edge.c - edge.a * p.x;
        t1 = p.y - yl;
        t2 = p.x - topSite.x;
        t3 = yl - topSite.y;
        above = t1 * t1 > t2 * t2 + t3 * t3;
      }
      return left ? above : !above;
    }

  }
}