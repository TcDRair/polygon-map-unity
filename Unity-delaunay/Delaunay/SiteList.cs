using UnityEngine;
using System;
using System.Collections.Generic;
using Delaunay.Geo;
using Delaunay.Utils;

namespace Delaunay
{

  public sealed class SiteList: Utils.IDisposable
  {
    private List<Site> _sites;
    private int _currentIndex;
    
    private bool _sorted;
    
    public SiteList () {
      _sites = new List<Site>();
      _sorted = false;
    }
    
    /// <summary>제거하기 전 저장된 모든 Site 개체를 정리합니다.</summary>
    public void Dispose() {
      if (_sites is not null) {
        for (int i = 0; i < _sites.Count; i++) _sites[i].Dispose();
        _sites.Clear();
        _sites = null;
      }
    }
    
    /// <summary>해당 Site를 추가하고 현재 Site 개수를 반환합니다.</summary>
    public int Add(Site site) {
      _sorted = false;
      _sites.Add(site);
      return _sites.Count;
    }
    
    public int Count => _sites.Count;
    
    public Site Next() {
      //! 정렬된 상태에서 호출해야 합니다.
      if (_sorted == false) {
        Debug.LogError ("SiteList::next():  sites have not been sorted");
      }
      if (_currentIndex < _sites.Count) {
        return _sites [_currentIndex++];
      } else {
        return null;
      }
    }

    internal Rect GetSitesBounds()
    {
      if (_sorted == false) {
        _sites.Sort();
        _currentIndex = 0;
        _sorted = true;
      }
      float xmin, xmax, ymin, ymax;
      if (_sites.Count == 0) {
        return new Rect (0, 0, 0, 0);
      }
      xmin = float.MaxValue;
      xmax = float.MinValue;
      for (int i = 0; i<_sites.Count; i++) {
        Site site = _sites [i];
        if (site.x < xmin) {
          xmin = site.x;
        }
        if (site.x > xmax) {
          xmax = site.x;
        }
      }
      // here's where we assume that the sites have been sorted on y:
      ymin = _sites[0].y;
      ymax = _sites[^1].y;
      
      return new(xmin, ymin, xmax - xmin, ymax - ymin);
    }

    public List<uint> SiteColors(/*BitmapData referenceImage = null*/)
    {
      List<uint> colors = new();
      Site site;
      for (int i = 0; i< _sites.Count; i++) {
        site = _sites [i];
        colors.Add (/*referenceImage ? referenceImage.getPixel(site.x, site.y) :*/site.color);
      }
      return colors;
    }

    public List<Vector2> SiteCoords ()
    {
      List<Vector2> coords = new();
      Site site;
      for (int i = 0; i<_sites.Count; i++) {
        site = _sites [i];
        coords.Add (site.Coord);
      }
      return coords;
    }

    /**
     * 
     * @return the largest circle centered at each site that fits in its region;
     * if the region is infinite, return a circle of radius 0.
     * 
     */
    public List<Circle> Circles()
    {
      List<Circle> circles = new();
      Site site;
      for (int i = 0; i<_sites.Count; i++) {
        site = _sites [i];
        float radius = 0f;
        Edge nearestEdge = site.NearestEdge();
        
        if (!nearestEdge.IsPartOfConvexHull()) {
          radius = nearestEdge.Distance * 0.5f;
        }
        circles.Add(new(site.x, site.y, radius));
      }
      return circles;
    }

    public List<List<Vector2>> Regions(Rect plotBounds) {
      List<List<Vector2>> regions = new();
      Site site;
      for (int i = 0; i< _sites.Count; i++) {
        site = _sites [i];
        regions.Add (site.Region(plotBounds));
      }
      return regions;
    }
  }
}