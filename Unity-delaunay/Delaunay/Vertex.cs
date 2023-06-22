using UnityEngine;
using System;
using System.Collections.Generic;
namespace Delaunay
{
  
  public sealed class Vertex: ICoord
  {
    public static readonly Vertex VERTEX_NAN = new(float.NaN, float.NaN);
    
    private static readonly Stack<Vertex> _pool = new();
    private static Vertex Create(float x, float y) =>
      (float.IsNaN(x) || float.IsNaN(y))
      ? VERTEX_NAN
      : (_pool.TryPop(out var v) ? v.Init(x, y) : new(x, y));

    private static int _nvertices = 0;
    public Vector2 Coord { get; private set; }
    public float X => Coord.x;
    public float Y => Coord.y;
    public int VIndex { get; private set; }
    
    public Vertex(float x, float y) => Init(x, y);
    
    private Vertex Init(float x, float y) {
      Coord = new Vector2(x, y);
      return this;
    }
    
    public void Dispose() => _pool.Push(this);
    public void SetIndex() =>VIndex = _nvertices++;
    
    public override string ToString() => $"Vertex ({VIndex})";

    public static Vertex Intersect(Halfedge halfedge0, Halfedge halfedge1)
    {
      Edge edge0, edge1, edge;
      Halfedge halfedge;
      float determinant, intersectionX, intersectionY;
      bool rightOfSite;
    
      edge0 = halfedge0.edge;
      edge1 = halfedge1.edge;
      if (edge0 == null || edge1 == null) {
        return null;
      }
      if (edge0.rightSite == edge1.rightSite) {
        return null;
      }
    
      determinant = edge0.a * edge1.b - edge0.b * edge1.a;
      if (-1.0e-10 < determinant && determinant < 1.0e-10) {
        // the edges are parallel
        return null;
      }
    
      intersectionX = (edge0.c * edge1.b - edge1.c * edge0.b) / determinant;
      intersectionY = (edge1.c * edge0.a - edge0.c * edge1.a) / determinant;
    
      if (Voronoi.CompareYtoX (edge0.rightSite, edge1.rightSite) < 0) {
        halfedge = halfedge0;
        edge = edge0;
      } else {
        halfedge = halfedge1;
        edge = edge1;
      }
      rightOfSite = intersectionX >= edge.rightSite.x;
      if ((rightOfSite && halfedge.left)
        || (!rightOfSite && !halfedge.left)) {
        return null;
      }
    
      return Create(intersectionX, intersectionY);
    }
  }
}