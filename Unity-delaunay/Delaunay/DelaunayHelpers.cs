using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;

namespace Delaunay
{  
  public class Node
  {
    public static Stack<Node> pool = new();
    
    public Node parent;
    public int treeSize;
  }

  public enum KruskalType { MINIMUM, MAXIMUM }

  public static class DelaunayHelpers
  {
    //? Not used
    /*
    public static List<LineSegment> VisibleLineSegments(List<Edge> edges) {
      List<LineSegment> segments = new();
      
      for (int i = 0; i<edges.Count; i++) {
        Edge edge = edges[i];
        if (edge.Visible) {
          var p1 = edge.ClippedEnds[Side.LEFT];
          var p2 = edge.ClippedEnds[Side.RIGHT];
          segments.Add(new(p1, p2));
        }
      }
      
      return segments;
    }

    public static List<Edge> SelectEdgesForSitePoint(Vector2 coord, List<Edge> edgesToTest)
      => edgesToTest.FindAll(edge => (edge.leftSite?.Coord == coord) || (edge.rightSite?.Coord == coord));

    public static List<Edge> SelectNonIntersectingEdges (List<Edge> edgesToTest) => edgesToTest;

    public static List<LineSegment> DelaunayLinesForEdges(List<Edge> edges) {
      List<LineSegment> segments = new();
      Edge edge;
      for (int i = 0; i < edges.Count; i++) {
        edge = edges [i];
        segments.Add(edge.DelaunayLine());
      }
      return segments;
    }

    public static List<LineSegment> Kruskal(List<LineSegment> lineSegments, KruskalType type = KruskalType.MINIMUM) {
      Dictionary<Vector2?, Node> nodes = new();
      List<LineSegment> mst = new();
      Stack<Node> nodePool = Node.pool;
      
      switch (type) {
      // note that the compare functions are the reverse of what you'd expect
      // because (see below) we traverse the lineSegments in reverse order for speed
      case KruskalType.MAXIMUM:
        lineSegments.Sort (delegate (LineSegment l1, LineSegment l2) {
          return LineSegment.CompareLengths (l1, l2);
        });
        break;
      default:
        lineSegments.Sort (delegate (LineSegment l1, LineSegment l2) {
          return LineSegment.CompareLengths_MAX (l1, l2);
        });
        break;
      }
      
      for (int i = lineSegments.Count; --i > -1;) {
        LineSegment lineSegment = lineSegments [i];

        Node node0 = null;
        Node rootOfSet0;
        if (!nodes.ContainsKey (lineSegment.p0)) {
          node0 = nodePool.Count > 0 ? nodePool.Pop () : new Node ();
          // intialize the node:
          rootOfSet0 = node0.parent = node0;
          node0.treeSize = 1;
          
          nodes [lineSegment.p0] = node0;
        } else {
          node0 = nodes [lineSegment.p0];
          rootOfSet0 = Find (node0);
        }
        
        Node node1 = null;
        Node rootOfSet1;
        if (!nodes.ContainsKey (lineSegment.p1)) {
          node1 = nodePool.Count > 0 ? nodePool.Pop () : new Node ();
          // intialize the node:
          rootOfSet1 = node1.parent = node1;
          node1.treeSize = 1;
          
          nodes [lineSegment.p1] = node1;
        } else {
          node1 = nodes [lineSegment.p1];
          rootOfSet1 = Find (node1);
        }
        
        if (rootOfSet0 != rootOfSet1) {  // nodes not in same set
          mst.Add (lineSegment);
          
          // merge the two sets:
          int treeSize0 = rootOfSet0.treeSize;
          int treeSize1 = rootOfSet1.treeSize;
          if (treeSize0 >= treeSize1) {
            // set0 absorbs set1:
            rootOfSet1.parent = rootOfSet0;
            rootOfSet0.treeSize += treeSize1;
          } else {
            // set1 absorbs set0:
            rootOfSet0.parent = rootOfSet1;
            rootOfSet1.treeSize += treeSize0;
          }
        }
      }
      foreach (Node node in nodes.Values) {
        nodePool.Push (node);
      }
      
      return mst;
    }

    private static Node Find(Node node) {
      Node current = node, parent = current.parent;
      while (parent != current) {
        current = parent;
        parent = current.parent;
      }
      node.parent = current; // <- current == root
      return current;
    }
    */
  }
}