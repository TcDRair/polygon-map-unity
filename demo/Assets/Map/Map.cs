using Delaunay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Map
{
  public enum Size {
    s1 = 64,
    s2 = 128,
    s3 = 256,
    s4 = 512,
    // s5 = 1024, s6 = 2048 //? Too big, may cause too dirty or simple map
  }

  public class Map
  {
    readonly int _size;
    public int Width { get; init; }
    public int Height { get; init; }
    const int NUM_LLOYD_RELAXATIONS = 2;
    
    public Graph Graph { get; private set; }
    public Center SelectedCenter { get; private set; }

    
    public class Progress {
      /// <summary>Graph 작업의 전체적인 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 실제 시간과 일치하지 않을 수 있습니다.</summary>
      public float TotalProgress => state switch {
        State.NotStarted => 0f,
        State.GeneratingRandomPoints => 0.5f * CurrentProgress,
        State.RelaxingPoints => 0.3f * CurrentProgress + 0.5f,
        State.GeneratingVoronoiDiagram => 0.9f,
        State.Finished => 1,
        _ => 0
      };
      //? 상기 비율은 몇 번의 테스트 이후 대략적인 시간적 비율을 나타내도록 합시다.

      /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
      public float CurrentProgress => (float)currentProgressCount.x/currentProgressCount.y;
      /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>Vector2(n, m)으로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
      public Vector2Int currentProgressCount;
      public enum State {
        NotStarted,
        GeneratingRandomPoints, RelaxingPoints, GeneratingVoronoiDiagram,
        Finished,
      } public State state = State.NotStarted;
      public bool HasStarted => state != State.NotStarted;

      public override string ToString() {
        if (state == State.GeneratingRandomPoints || state == State.RelaxingPoints)
          return "[Map] " + state.ToString().ToNiceString() + " " + currentProgressCount.ToNiceString();
        else return "[Map] " + state.ToString().ToNiceString();
      }
    } public Progress progress = new();

    /// <summary>노드, 점, 높이, 강물, 기후 등 섬의 정보를 담고 있는 지도를 생성합니다.</summary>
    public Map(Size size) {
      _size = (int)size;
      Width = _size; Height = _size;

      int pointCount = _size * _size / 50; // 설정한 지도 크기에 비례해서 점의 개수를 설정합니다.

      var colors = new uint[pointCount];
      var points = new Vector2[pointCount];

      // 랜덤 점 생성
      GeneratePoints(points, _size, _size);

      // Voronoi Diagram 생성   
      progress.state = Progress.State.GeneratingVoronoiDiagram;
      var voronoi = new Voronoi(points, colors, new Rect(0, 0, _size, _size));
      Graph = new Graph(points, voronoi, size);
      

      progress.state = Progress.State.Finished;
    }

    private void GeneratePoints(Vector2[] points, int width, int height) {
      int count = points.Length;
      progress.currentProgressCount.y = count;
      progress.state = Progress.State.GeneratingRandomPoints;
      for (int i = 0; i < count; i++) {
        progress.currentProgressCount.x = i+1;
        points[i] = new Vector2(UnityEngine.Random.value * width, UnityEngine.Random.value * height);
      }
      
      progress.currentProgressCount.y = NUM_LLOYD_RELAXATIONS;
      progress.state = Progress.State.RelaxingPoints;
      // 균일화 작업
      for (int i = 0; i < NUM_LLOYD_RELAXATIONS; i++) {
        progress.currentProgressCount.x = i+1;
        Graph.RelaxPoints(points, width, height);
      }
    }

    //? Automatically generate biome map
    public Biome[,] GetDataArray(int width, int height) {
      //? Link Biome
      var biomes = new Biome[width, height];
      float sX = Width/width, sY = Height/height;
      for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) {
        biomes[x, y] = Graph.GetNearestCenter(new Vector2(x * sX, y * sY)).biome;
      }
      return biomes;
    }

    internal void Click(Vector2 point) {
      SelectedCenter = Graph.main.centers.FirstOrDefault(p => p.PointInside(point.x, point.y));
    }
  }
}
