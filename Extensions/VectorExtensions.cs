using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Vector2Extensions
{
  public static Vector2 Interpolate(Vector2 pt1, Vector2 pt2, float f)
  {
    var x = f * pt1.x + (1 - f) * pt2.x;
    var y = f * pt1.y + (1 - f) * pt2.y;

    return new Vector2(x, y);
  }

  public static void DrawLine(this Texture2D tex, int x0, int y0, int x1, int y1, Color col)
  {
    int dy = y1 - y0;
    int dx = x1 - x0;
    int stepx, stepy;

    if (dy < 0) { dy = -dy; stepy = -1; }
    else { stepy = 1; }
    if (dx < 0) { dx = -dx; stepx = -1; }
    else { stepx = 1; }
    dy <<= 1;
    dx <<= 1;

    float fraction;

    tex.SetPixel(x0, y0, col);
    if (dx > dy)
    {
      fraction = dy - (dx >> 1);
      while (Mathf.Abs(x0 - x1) > 1)
      {
        if (fraction >= 0)
        {
          y0 += stepy;
          fraction -= dx;
        }
        x0 += stepx;
        fraction += dy;
        tex.SetPixel(x0, y0, col);
      }
    }
    else
    {
      fraction = dx - (dy >> 1);
      while (Mathf.Abs(y0 - y1) > 1)
      {
        if (fraction >= 0)
        {
          x0 += stepx;
          fraction -= dy;
        }
        y0 += stepy;
        fraction += dx;
        tex.SetPixel(x0, y0, col);
      }
    }
  }

  //? IsInsidePolygon : https://bowbowbow.tistory.com/m/24
  //! Suppose those points are sorted clockwise(or counter-).
  static void FillPolygonWithFunc(this Texture2D texture, IEnumerable<Vector2> points, System.Func<int, int, Color> func) {
    // http://alienryderflex.com/polygon_fill/

    int xMin = (int)points.Min(p => p.x),
      yMin = (int)points.Min(p => p.y),
      xMax = (int)points.Max(p => p.x),
      yMax = (int)points.Max(p => p.y);

    /*RectInt area = new(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
    var pos = points.ToArray();
    var lines = pos.Select((p, i) => (p, pos[(i+1)%pos.Length]));

    List<Vector2Int> inside = new();
    foreach (var p in area.allPositionsWithin) {
      int crosses = 0;
      foreach (var (p1, p2) in lines) {
        if ((p1.y - p.y) * (p2.y - p.y) > 0) continue;
        var x = (p.y - p2.y) * (p2.x - p1.x) / (p2.y - p1.y) + p2.x;
        if (x >= p.x) crosses++;
      }
      if (crosses%2 > 0) inside.Add(p);
    }
    inside.ForEach(v => texture.SetPixel(v.x, v.y, func(v.x, v.y)));*/

    var corners = points.Count();
    float[] polyX = points.Select(p => p.x).ToArray(), polyY = points.Select(p => p.y).ToArray();

    var nodeX = new int[corners];
    int nodes, i, j, swap;

    //  Loop through the rows of the image.
    for (int y = yMin; y <= yMax; y++) {
      nodes = 0;
      j = corners - 1;
      //  Build a list of nodes.
      for (i = 0; i < corners; i++) {
        if (polyY[i] < y && polyY[j] >= y || polyY[j] < y && polyY[i] >= y) {
          nodeX[nodes++] = (int)(polyX[i] + (y - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
        }
        j = i;
      }

      //  Sort the nodes, via a simple “Bubble” sort.
      i = 0;
      while (i < nodes - 1) {
        if (nodeX[i] > nodeX[i + 1]) {
          swap = nodeX[i];
          nodeX[i] = nodeX[i + 1];
          nodeX[i + 1] = swap;
          if (i > 0) i--;
        }
        else i++;
      }

      //  Fill the pixels between node pairs.
      for (i = 0; i < nodes; i += 2) {
        if (nodeX[i] < xMax && nodeX[i + 1] > xMin) {
          if (nodeX[i] < xMin) nodeX[i] = xMin;
          if (nodeX[i + 1] > xMax) nodeX[i + 1] = xMax;
          for (j = nodeX[i]; j < nodeX[i + 1]; j++) texture.SetPixel(j, y, func(j, y));
        }
      }
    }
  }

  public static void FillPolygon(this Texture2D texture, IEnumerable<Vector2> points, Color color) => texture.FillPolygonWithFunc(points, (x, y) => color);
  public static void FillPolygon(this Texture2D texture, IEnumerable<Vector2> points, bool isLand) => texture.FillPolygonWithFunc(points, isLand ? ((_, _) => Color.white) : ((_, _) => Color.clear));

  public static void FillPolygon(this Texture2D texture, Assets.Maps.Center center, int scaler, bool gradient = false) {
    if (gradient) {
      var cs = center.corners;
      texture.FillPolygonWithFunc(
        cs.Select(c => c.point * scaler),
        (x, y) => {
          var weights = center.corners.Select(p => 1/Vector2.Distance(p.point * scaler, new Vector2(x, y)));
          var normalizedWeights = weights.Select(w => w / weights.Sum());
          float h = cs.Zip(normalizedWeights, (c, w) => c.elevation * w).Sum(),
            m = cs.Zip(normalizedWeights, (c, w) => c.moisture * w).Sum(),
            w = cs.Any(c => c.water) ? 0 : 1;
          return new Color(h, m, w);
        }
      );
    }
    else texture.FillPolygonWithFunc(
      center.corners.Select(c => c.point * scaler),
      (x, y) => {
        float h = center.elevation, m = center.moisture, w = center.water ? 0 : 1;
        return new Color(h, m, w);
      }
    );
  }
}

public static class Vector3Extensions
{
  public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);
}