/*
 * The author of this software is Steven Fortune.  Copyright (c) 1994 by AT&T
 * Bell Laboratories.
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;
using Delaunay.Utils;

namespace Delaunay
{
  public sealed class Voronoi: Utils.IDisposable
  {
    private SiteList _sites;
    private Dictionary <Vector2,Site> _sitesIndexedByLocation;
    private List<Triangle> _triangles;
    public List<Edge> Edges { get; private set; }

    
    // TODO generalize this so it doesn't have to be a rectangle;
    // then we can make the fractal voronois-within-voronois
    public Rect PlotBounds { get; private set; }

    /// <summary>제거하기 전 저장된 하위 개체들을 정리합니다.</summary>
    public void Dispose() {
      _sites?.Dispose();
      _sites = null;

      _triangles?.ForEach(t => t.Dispose());
      _triangles?.Clear();
      _triangles = null;
      
      Edges?.ForEach(e => e.Dispose());
      Edges?.Clear();
      Edges = null;

      // _plotBounds = null;
      _sitesIndexedByLocation = null;
    }
    
    public Voronoi(Vector2[] points, uint[] colors, Rect plotBounds) {
      _sites = new();
      _sitesIndexedByLocation = new(); // XXX: Used to be Dictionary(true) -- weak refs.
      AddSites(points, colors);
      PlotBounds = plotBounds;
      _triangles = new();
      Edges = new();
      FortunesAlgorithm();
    }
    
    private void AddSites(Vector2[] points, uint[] colors) {
      int length = points.Length;
      if (colors == null) for (int i = 0; i < length; i++) AddSite(points[i], 0, i);
      else for (int i = 0; i < length; i++) AddSite(points[i], colors[i], i);
    }
    
    private void AddSite(Vector2 p, uint color, int index) {
      if (_sitesIndexedByLocation.ContainsKey(p)) return; // Prevent duplicate site! (Adapted from https://github.com/nodename/as3delaunay/issues/1)
      float weight = UnityEngine.Random.value * 100f;
      Site site = Site.Create(p, (uint)index, weight, color);
      _sites.Add(site);
      _sitesIndexedByLocation[p] = site;
    }
          
    public List<Vector2> Region(Vector2 p) {
      Site site = _sitesIndexedByLocation[p];
      if (site == null) return new List<Vector2>();
      return site.Region(PlotBounds);
    }

    private Site fortunesAlgorithm_bottomMostSite;
    private void FortunesAlgorithm() {
      Site newSite, bottomSite, topSite, tempSite;
      Vertex v, vertex;
      Vector2 newintstar = Vector2.zero; //Because the compiler doesn't know that it will have a value - Julian
      Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
      Edge edge;
      
      Rect dataBounds = _sites.GetSitesBounds();
      
      int sqrt_nsites = (int)Mathf.Sqrt(_sites.Count + 4);
      HalfedgePriorityQueue heap = new(dataBounds.y, dataBounds.height, sqrt_nsites);
      EdgeList edgeList = new(dataBounds.x, dataBounds.width, sqrt_nsites);
      List<Halfedge> halfEdges = new();
      List<Vertex> vertices = new();
      
      fortunesAlgorithm_bottomMostSite = _sites.Next();
      newSite = _sites.Next();
      
      for (;;) {
        if (heap.Empty() == false) newintstar = heap.Min();
      
        if (newSite != null && (heap.Empty() || CompareByYThenX(newSite, newintstar) < 0)) {
          /* new site is smallest */
          //trace("smallest: new site " + newSite);
          
          // Step 8:
          lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord);  // the Halfedge just to the left of newSite
          //trace("lbnd: " + lbnd);
          rbnd = lbnd.rightEdge;    // the Halfedge just to the right
          //trace("rbnd: " + rbnd);
          bottomSite = FortunesAlgorithm_rightRegion(lbnd);    // this is the same as leftRegion(rbnd)
          // this Site determines the region containing the new site
          //trace("new Site is in region of existing site: " + bottomSite);
          
          // Step 9:
          edge = Edge.CreateBisectingEdge(bottomSite, newSite);
          //trace("new edge: " + edge);
          Edges.Add(edge);
          
          bisector = Halfedge.Create(edge, true);
          halfEdges.Add(bisector);
          // inserting two Halfedges into edgeList constitutes Step 10:
          // insert bisector to the right of lbnd:
          edgeList.Insert(lbnd, bisector);
          
          // first half of Step 11:
          if ((vertex = Vertex.Intersect(lbnd, bisector)) != null) {
            vertices.Add(vertex);
            heap.Remove(lbnd);
            lbnd.vertex = vertex;
            lbnd.ystar = vertex.Y + newSite.Dist(vertex);
            heap.Insert(lbnd);
          }
          
          lbnd = bisector;
          bisector = Halfedge.Create(edge, false);
          halfEdges.Add(bisector);
          // second Halfedge for Step 10:
          // insert bisector to the right of lbnd:
          edgeList.Insert(lbnd, bisector);
          
          // second half of Step 11:
          if ((vertex = Vertex.Intersect(bisector, rbnd)) != null) {
            vertices.Add(vertex);
            bisector.vertex = vertex;
            bisector.ystar = vertex.Y + newSite.Dist(vertex);
            heap.Insert(bisector);  
          }
          
          newSite = _sites.Next();  
        } else if (heap.Empty() == false) {
          /* intersection is smallest */
          lbnd = heap.ExtractMin();
          llbnd = lbnd.leftEdge;
          rbnd = lbnd.rightEdge;
          rrbnd = rbnd.rightEdge;
          bottomSite = FortunesAlgorithm_leftRegion(lbnd);
          topSite = FortunesAlgorithm_rightRegion(rbnd);
          // these three sites define a Delaunay triangle
          // (not actually using these for anything...)
          //_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));
          
          v = lbnd.vertex;
          v.SetIndex();
          lbnd.edge.SetVertex(lbnd.left, v);
          rbnd.edge.SetVertex(rbnd.left, v);
          edgeList.Remove(lbnd); 
          heap.Remove(rbnd);
          edgeList.Remove(rbnd); 
          bool left = true;
          if (bottomSite.y > topSite.y) {
            tempSite = bottomSite;
            bottomSite = topSite;
            topSite = tempSite;
            left = false;
          }
          edge = Edge.CreateBisectingEdge(bottomSite, topSite);
          Edges.Add(edge);
          bisector = Halfedge.Create(edge, left);
          halfEdges.Add(bisector);
          edgeList.Insert(llbnd, bisector);
          edge.SetVertex(!left, v);
          if ((vertex = Vertex.Intersect(llbnd, bisector)) != null) {
            vertices.Add(vertex);
            heap.Remove(llbnd);
            llbnd.vertex = vertex;
            llbnd.ystar = vertex.Y + bottomSite.Dist(vertex);
            heap.Insert(llbnd);
          }
          if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null) {
            vertices.Add(vertex);
            bisector.vertex = vertex;
            bisector.ystar = vertex.Y + bottomSite.Dist(vertex);
            heap.Insert(bisector);
          }
        }
        else break;
      }
      
      // heap should be empty now
      heap.Dispose();
      edgeList.Dispose();
      
      for (int hIndex = 0; hIndex < halfEdges.Count; hIndex++) {
        Halfedge halfEdge = halfEdges[hIndex];
        halfEdge.ReallyDispose();
      }
      halfEdges.Clear();
      
      // we need the vertices to clip the edges
      for (int eIndex = 0; eIndex < Edges.Count; eIndex++) {
        edge = Edges[eIndex];
        edge.ClipVertices(PlotBounds);
      }
      // but we don't actually ever use them again!
      for (int vIndex = 0; vIndex < vertices.Count; vIndex++) {
        vertex = vertices[vIndex];
        vertex.Dispose();
      }
      vertices.Clear();
    }

    private Site FortunesAlgorithm_leftRegion(Halfedge he) {
      return he.edge?.Site(he.left) ?? fortunesAlgorithm_bottomMostSite;
    }
    
    private Site FortunesAlgorithm_rightRegion(Halfedge he) {
      return he.edge?.Site(!he.left) ?? fortunesAlgorithm_bottomMostSite;
    }
    
    public static bool CASDG(Site s1, Site s2) {
      return (s1.y - s2.y) switch {
        > 0 => true,
        < 0 => false,
        _ => s1.x > s2.x
      };
    }

    public static int CompareYtoX(Site s1, Site s2) {
      
      if (s1.y < s2.y)
        return -1;
      if (s1.y > s2.y)
        return 1;
      if (s1.x < s2.x)
        return -1;
      if (s1.x > s2.x)
        return 1;
      return 0;
    }

    public static int CompareByYThenX(Site s1, Vector2 s2) {
      if (s1.y < s2.y)
        return -1;
      if (s1.y > s2.y)
        return 1;
      if (s1.x < s2.x)
        return -1;
      if (s1.x > s2.x)
        return 1;
      return 0;
    }

  }
}