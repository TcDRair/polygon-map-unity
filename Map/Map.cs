using Delaunay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assets.Util;
namespace Assets.Maps
{
  public enum Size {
    s1 = 64,
    s2 = 128,
    s3 = 256,
    s4 = 512,
    // s5 = 1024, s6 = 2048 //? Too big, may cause too dirty or simple map
  }

  public class Map : IProgressTimerProvider
  {
    readonly int size;
    public int Width { get; init; }
    public int Height { get; init; }
    const int NUM_LLOYD_RELAXATIONS = 2;
    
    public Graph Graph { get; private set; }

    public ProgressTimer Timer { get; private set; } = new(
      "Map",
      ("Generating Random Points"  , 0.00f,  true),
      ("Relaxing Points"           , 0.50f,  true),
      ("Generating Voronoi Diagram", 0.90f, false)
    );

    /// <summary>노드, 점, 높이, 강물, 기후 등 섬의 정보를 담고 있는 지도를 생성합니다.</summary>
    public Map(Size size) {
      this.size = (int)size;
      Width = this.size; Height = this.size;
    }
    private bool initialized = false;
    public IEnumerator Initialize() {
      if (initialized) yield break;
      int pointCount = size * size / 50; // 설정한 지도 크기에 비례해서 점의 개수를 설정합니다.

      var colors = new uint[pointCount];
      var points = new Vector2[pointCount];
      // 랜덤 점 생성
      yield return GeneratePoints(points, size, size);

      // Voronoi Diagram 생성
      Timer.Next();
      var voronoi = new Voronoi(points, colors, new Rect(0, 0, size, size));
      Graph = new Graph(points, voronoi, (Size)size);
      Timer.Next();
      initialized = true;
    }

    IEnumerator GeneratePoints(Vector2[] points, int width, int height) {
      int count = points.Length;
      for (int i = 0; i < count; i++) {
        points[i] = new Vector2(Random.value * width, Random.value * height);
        if (Timer.Elapsed) { Timer.SetDetail(i, count); yield return null; }
      }
      
      Timer.Next();
      // 균일화 작업
      for (int i = 0; i < NUM_LLOYD_RELAXATIONS; i++) {
        Graph.RelaxPoints(points, width, height);
        if (Timer.Elapsed) { Timer.SetDetail(i, NUM_LLOYD_RELAXATIONS); yield return null; }
      }
    }
  }
}
