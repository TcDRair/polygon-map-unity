using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Assets.Util;
namespace Assets.Maps
{
  public class MapTexture : IProgressTimerProvider
  {
    readonly int textureScale;
    public MapTexture(int textureScale) { this.textureScale = textureScale; }

    public ProgressTimer Timer { get; private set; } = new(
      "MapTexture",
      ("Filling Polygons", 0.00f,  true),
      ("Drawing Lines"   , 0.90f, false),
      ("Drawing Rivers"  , 0.94f, false),
      ("Applying"        , 0.97f, false)
    );

    public Texture2D Texture { get; private set; }
    public Texture2D DataMap { get; private set; }
    /// <summary>주어진 Map 개체에 대한 시각적 정보를 반환합니다.</summary>
    public IEnumerator CreateMapMaterial(Map map)
    {
      int _textureWidth = map.Width * textureScale;
      int _textureHeight = map.Height * textureScale;
      Texture = new Texture2D(_textureWidth, _textureHeight);
      DataMap = new Texture2D(_textureWidth, _textureHeight) {
        filterMode = FilterMode.Point,
        wrapMode = TextureWrapMode.Clamp
      };
      DataMap.SetPixels(Enumerable.Repeat(Color.clear, DataMap.width * DataMap.height).ToArray());
      var lines = map.Graph.vars.edges.Where(p => p.v0 != null).Select(p => new[] 
      { 
        p.v0.X, p.v0.Y,
        p.v1.X, p.v1.Y
      }).ToArray();

      int total = map.Graph.vars.centers.Count, count = 0;
      foreach (var c in map.Graph.vars.centers) {
        Texture.FillPolygon(c.corners.Select(p => p.point * textureScale), BiomeProperties.Colors[c.biome]);
        DataMap.FillPolygon(c, textureScale, true);

        ++count;
        if (Timer.Elapsed) { Timer.SetDetail(count, total); yield return null; }
      }

      Timer.Next();
      foreach (var line in map.Graph.vars.edges.Where(p => p.river > 0 && !p.d0.water && !p.d1.water)) {
        DrawLine(Texture, line.v0.X, line.v0.Y, line.v1.X, line.v1.Y, Color.blue);
        if (Timer.Elapsed) yield return null;
      }
      Timer.Next();
      Texture.Apply();
      yield return null;
      Timer.Next();
    }
    public byte[] HeightmapRaw => DataMap.GetPixels32().Select(p => p.r).ToArray();

    private void DrawLine(Texture2D texture, float x0, float y0, float x1, float y1, Color color) {
      texture.DrawLine((int)(x0 * textureScale), (int)(y0 * textureScale), (int)(x1 * textureScale), (int)(y1 * textureScale), color);
    }
  }
}
