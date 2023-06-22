using Delaunay.Utils;
using System.Collections.Generic;

namespace Delaunay
{
  public enum VertexOrSite
  {
    VERTEX,
    SITE
  }
  
  sealed class EdgeReorderer: IDisposable
  {
    public List<Edge> Edges { get; private set; }
    public List<bool> EdgeOrientations { get; private set; }
    
    public EdgeReorderer(List<Edge> origEdges, VertexOrSite criterion) {
      Edges = new();
      EdgeOrientations = new();
      if (origEdges.Count > 0) {
        Edges = ReorderEdges(origEdges, criterion);
      }
    }
    
    public void Dispose() {
      Edges = null;
      EdgeOrientations = null;
    }

    private List<Edge> ReorderEdges(List<Edge> origEdges, VertexOrSite criterion) {
      int n = origEdges.Count;
      Edge edge;
      // we're going to reorder the edges in order of traversal
      var done = new bool[n];
      for (int j=0; j<n; j++) done[j] = false;
      List<Edge> newEdges = new(); // TODO: Switch to Deque if performance is a concern
      
      edge = origEdges[0];
      newEdges.Add (edge);
      EdgeOrientations.Add(true);
      ICoord firstPoint = (criterion == VertexOrSite.VERTEX) ? edge.LeftVertex : edge.leftSite;
      ICoord lastPoint = (criterion == VertexOrSite.VERTEX) ? edge.RightVertex : edge.rightSite;
      
      if (firstPoint == Vertex.VERTEX_NAN || lastPoint == Vertex.VERTEX_NAN) return new();
      
      done[0] = true;

      int nDone = 1;
      while (nDone < n) {
        for (int i = 1; i < n; ++i) {
          if (done[i]) continue;
          edge = origEdges[i];
          ICoord leftPoint = (criterion == VertexOrSite.VERTEX) ? edge.LeftVertex : edge.leftSite;
          ICoord rightPoint = (criterion == VertexOrSite.VERTEX) ? edge.RightVertex : edge.rightSite;
          if (leftPoint == Vertex.VERTEX_NAN || rightPoint == Vertex.VERTEX_NAN) return new();

          if (leftPoint == lastPoint) {
            lastPoint = rightPoint;
            EdgeOrientations.Add(true);
            newEdges.Add(edge);
          } else if (rightPoint == firstPoint) {
            firstPoint = leftPoint;
            EdgeOrientations.Insert(0, true); // TODO: Change datastructure if this is slow
            newEdges.Insert(0, edge);
          } else if (leftPoint == firstPoint) {
            firstPoint = rightPoint;
            EdgeOrientations.Insert(0, false);
            newEdges.Insert(0, edge);
          } else if (rightPoint == lastPoint) {
            lastPoint = leftPoint;
            EdgeOrientations.Add(false);
            newEdges.Add(edge);
          }
          else continue;
          done[i] = true;
          ++nDone;
        }
      } 
      return newEdges;
    }
  }
}