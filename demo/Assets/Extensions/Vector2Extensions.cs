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
    int dy = (int)(y1 - y0);
    int dx = (int)(x1 - x0);
    int stepx, stepy;

    if (dy < 0) { dy = -dy; stepy = -1; }
    else { stepy = 1; }
    if (dx < 0) { dx = -dx; stepx = -1; }
    else { stepx = 1; }
    dy <<= 1;
    dx <<= 1;

    float fraction = 0;

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

  static void FillPolygonWithFunc(this Texture2D texture, Vector2[] points, System.Func<int, int, Color> func) {
    // http://alienryderflex.com/polygon_fill/

    int xMin = (int)points.Min(p => p.x),
      yMin = (int)points.Min(p => p.y),
      xMax = (int)points.Max(p => p.x),
      yMax = (int)points.Max(p => p.y)
    ;
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

  public static void FillPolygon(this Texture2D texture, Vector2[] points, Color color) {
    texture.FillPolygonWithFunc(points, (x, y) => color);
  }

  static bool e = true;
  public static void FillPolygon(this Texture2D texture, Vector2[] points, float[] elevations) {
    texture.FillPolygonWithFunc(
      points,
      (x, y) => {
        float[] distances = points.Select(p => Vector2.Distance(p, new Vector2(x, y))).ToArray();
        float sum = distances.Sum();
        float[] weights = distances.Select(d => sum - d).ToArray();
        float weightSum = weights.Sum();
        float elv = weights.Select((w, i) => w / weightSum * elevations[i]).Sum();
        //TODO 1 : make it simple
        //TODO 2 : check if corners are redistributed by center elevation
        if (e && elevations.Min() + 3 < elevations.Max()) { Debug.Log($"elv : {elv}, ({string.Join(',', elevations)})"); e = false; }
        return new Color(elv, elv, elv);
      }
    );
  }
}


