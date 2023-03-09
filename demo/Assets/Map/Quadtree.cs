using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/*
using Assets.Map;
public class QuadCorners : Quadtree2Pow<Corner>
{
  public QuadCorners(RectInt bounds, int maxCapacity = CAPACITY) : base(bounds, maxCapacity) {}
  protected override Quadtree2Pow<Corner> GetInstance(RectInt bounds, int maxCapacity) => new QuadCorners(bounds, maxCapacity);
  protected override Vector2 GetPoint(Corner corner) => corner.point;
}
public class QuadCenters : Quadtree2Pow<Center>
{
  public QuadCenters(RectInt bounds, int maxCapacity = CAPACITY) : base(bounds, maxCapacity) {}
  protected override Quadtree2Pow<Center> GetInstance(RectInt bounds, int maxCapacity) => new QuadCenters(bounds, maxCapacity);
  protected override Vector2 GetPoint(Center center) => center.point;
}

public abstract class Quadtree2Pow<T>
{
  public const int CAPACITY = 256;
  private readonly int m_limit;
  private readonly RectInt m_bounds;
  private readonly List<T> m_obj;

  protected abstract Vector2 GetPoint(T obj);

  private Quadtree2Pow<T> NW, NE, SW, SE;
  private bool divided = false;

  public Quadtree2Pow(RectInt bounds, int maxCapacity = CAPACITY) {
    m_bounds = bounds;
    m_limit = maxCapacity;
    m_obj = new(maxCapacity);
  }

  #region Mechanism
  public void Insert(T obj) {
    var point = GetPoint(obj);
    if (divided) {
      if (point.x < m_bounds.center.x && point.y >= m_bounds.center.y) NW.Insert(obj);
      else if (point.x >= m_bounds.center.x && point.y >= m_bounds.center.y) NE.Insert(obj);
      else if (point.x < m_bounds.center.x && point.y < m_bounds.center.y) SW.Insert(obj);
      else SE.Insert(obj);
    }
    else { m_obj.Add(obj); if (m_obj.Count > m_limit) Divide(); }
  }

  protected abstract Quadtree2Pow<T> GetInstance(RectInt bounds, int maxCapacity);
  private void Divide() {
    int x = m_bounds.xMin;
    int y = m_bounds.yMin;
    int width = m_bounds.width / 2;
    int height = m_bounds.height / 2;

    NW = GetInstance(new(x        , y + height, width, height), m_limit);
    NE = GetInstance(new(x + width, y + height, width, height), m_limit);
    SW = GetInstance(new(x        , y         , width, height), m_limit);
    SE = GetInstance(new(x + width, y         , width, height), m_limit);

    foreach (var p in m_obj) {
      if      (NW.m_bounds.Contains(GetPoint(p))) NW.Insert(p);
      else if (NE.m_bounds.Contains(GetPoint(p))) NE.Insert(p);
      else if (SW.m_bounds.Contains(GetPoint(p))) SW.Insert(p);
      else if (SE.m_bounds.Contains(GetPoint(p))) SE.Insert(p);
    }
    m_obj.Clear();

    divided = true;
  }
  #endregion

  #region Query
  /// <summary>Finds closest object from given position within given range.</summary>
  /// <returns><see langword="true"/> if any object is found within range.</returns>
  public bool GetClosest(Vector2 pos, float range, out T result) {
    result = default;
    //* 0. Out of bounds
    if (pos.x + range < m_bounds.xMin || pos.x - range > m_bounds.xMax || pos.y + range < m_bounds.yMin || pos.y - range > m_bounds.yMax) return false;
    //* 1-1. Divided -> Get Closest from inner quads
    if (divided) {
      var inners = new[] { NW, NE, SW, SE }.Select(qt => { var n = qt.GetClosest(pos, range, out T p); return (n, p); });
      bool any;
      if (any = inners.Any(np => np.n)) result = inners.Where(np => np.n).MinItem(np => Vector2.Distance(pos, GetPoint(np.p))).p;
      return any;
    }
    //* 1-2. Not divided -> Find
    else {
      if (m_obj.Count > 0) {
        T closest = m_obj.MinItem(o => Vector2.Distance(pos, GetPoint(o)));
        if (Vector2.Distance(pos, GetPoint(closest)) <= range) { result = closest; return true; }
      }
      return false;
    }
  }
  /// <summary>Returns closest object from given position.</summary>
  public T GetClosest(Vector2 pos) {
    float range = 4; bool found; T result;
    do { found = GetClosest(pos, range, out result); range *= 2; } while (!found);
    return result;
  }
  /// <summary>Returns object with given position.</summary>
  /// <returns>throws Exception if not found.</returns>
  public T GetObject(Vector2 pos) {
    if (divided) {
      return (pos.x < m_bounds.center.x, pos.y < m_bounds.center.y) switch {
        (true, true) => SW.GetObject(pos),
        (true,    _) => NW.GetObject(pos),
        (   _, true) => SE.GetObject(pos),
        (   _,    _) => NE.GetObject(pos)
      };
    }
    else if (m_obj.Where(obj => GetPoint(obj) == pos) is var obj && obj.Count() > 0) return obj.First();
    throw new ArgumentException($"No {typeof(T).Name} found with given argument!");
  }
  #endregion
}
*/

public static class LinqExtension {
  public static T MinItem<T>(this IEnumerable<T> q, Func<T, float> selector) {
    if (q.Count() == 0) { Debug.LogError("No items in given sequence"); return default; }
    T min = q.First();
    foreach (var i in q) if (selector(min) > selector(i)) min = i;
    return min;
  }
  public static T MaxItem<T>(this IEnumerable<T> q, Func<T, float> selector) {
    if (q.Count() == 0) { Debug.LogError("No items in given sequence"); return default; }
    T max = q.First();
    foreach (var i in q) if (selector(max) < selector(i)) max = i;
    return max;
  }
}