using Delaunay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static Unity.EditorCoroutines.Editor.EditorCoroutineUtility;

namespace Assets.Map
{
  public class Graph
  {
    public class Variables {
      public Variables(Vector2[] points, Voronoi voronoi) { this.points = points; this.voronoi = voronoi; }
      public Vector2[] points;
      public Voronoi voronoi;
      public List<Center> centers = new();
      public List<Corner> corners = new();
      public List<Edge> edges = new();
      public Dictionary<Vector2, Center> centerLookup = new();
      public Dictionary<int, Dictionary<int, List<Corner>>> cornerMap = new();
      public IEnumerable<Corner> LandCorners  => corners.Where(p => !p.ocean && !p.coast);
      public IEnumerable<Corner> OceanCorners => corners.Where(p => p.ocean || p.coast);
      public IEnumerable<Corner> WaterCorners => corners.Where(p => p.water || p.coast || p.ocean);
    } public Variables main;

    //? mask, Gradient 등 별도의 Voronoi Diagram이 필요할 경우 대비

    // List<KeyValuePair<int, Corner>> cornerMap = new List<KeyValuePair<int, Corner>>();
    // System.Func<Vector2, (float, float)> insideWithValue; // For Debug
    System.Func<Vector2, bool> Inside;
    const bool _needMoreRandomness = true;

    //* 그래프의 크기는 마스크 맵과 일반 맵 모두 동일해야 합니다.
    readonly int _size;
    readonly bool hasMaskMap;
    
    //* 일정 크기 이상에서 마스크 맵을 지정하는 것이 좋습니다.
    public Graph(Vector2[] points, Voronoi voronoi, Size size) {
      _size = (int)size;
      main = new Variables(points, voronoi);
      hasMaskMap = size > Size.s4;
    }

    public class Progress {
      /// <summary>Graph 작업의 전체적인 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 실제 시간과 일치하지 않을 수 있습니다.</summary>
      public float TotalProgress => state switch {
        State.GraphTaskNotStarted => 0,
        State.BuildingGraphPoints => 0.1f,
        State.BuildingGraphCenters => 0.2f,
        State.BuildingGraphDelunayPoints => 0.2f + 0.5f * CurrentProgress,
        State.BuildingGraphSortedCorners => 0.71f,
        State.AssigningWaterLevel => 0.72f,
        State.AssigningCornerElevations => 0.73f,
        State.AssigningCenterBorders => 0.74f,
        State.AssigningCenterOceans => 0.76f,
        State.AssigningCenterCoasts => 0.78f,
        State.AssigningCorners => 0.79f,
        State.RedistributeLandCornerElevations => 0.80f,
        State.RedistributeWaterCornerElevations => 0.81f,
        State.AssignCenterElevations => 0.82f,
        State.CalculatingDownslopes => 0.83f,
        State.CalculatingWatersheds => 0.86f,
        State.CalculatingRivers => 0.89f,
        State.CalculatingFreshwaterMoisture => 0.93f,
        State.CalculatingLandMoisture => 0.94f,
        State.CalculatingCenterMoisture => 0.95f,
        State.SettingBiomes => 0.96f,
        State.FinishingGraphGenerating => 1,
        _ => 1
      };
      //? 상기 비율은 몇 번의 테스트 이후 대략적인 시간적 비율을 나타내도록 합시다.
        
      
      /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
      public float CurrentProgress => (float)currentProgressCount.x/currentProgressCount.y;
      /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>Vector2(n, m)으로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
      public Vector2Int currentProgressCount;
      public enum State {
        GraphTaskNotStarted,
        BuildingGraphPoints, BuildingGraphCenters, BuildingGraphDelunayPoints, BuildingGraphSortedCorners,
        AssigningWaterLevel, AssigningCornerElevations, AssigningCenterBorders, AssigningCenterOceans, AssigningCenterCoasts, AssigningCorners,
        RedistributeLandCornerElevations, RedistributeWaterCornerElevations, AssignCenterElevations,
        CalculatingDownslopes, CalculatingWatersheds, CalculatingRivers,
        CalculatingFreshwaterMoisture, CalculatingLandMoisture, /*CalculateOceanMoisture,*/ CalculatingCenterMoisture,
        SettingBiomes,
        FinishingGraphGenerating,
      } public State state = State.GraphTaskNotStarted;
      public bool HasStarted => state != State.GraphTaskNotStarted;

      public override string ToString() {
        if (state == State.BuildingGraphDelunayPoints)
          return "[Graph] " + state.ToString().ToNiceString() + " " + currentProgressCount.ToNiceString();
        else return "[Graph] " + state.ToString().ToNiceString();
      }
    } public Progress progress = new();

    float _prevTime;
    const float deltaTime = 0.01f;
    bool Elapsed {
      get {
        bool e = Time.realtimeSinceStartup - _prevTime > deltaTime;
        if (e) _prevTime = Time.realtimeSinceStartup;
        return e;
      }
    }

    /// <summary>에디트 타임에 그래프를 그립니다.</summary>
    public IEnumerator InitGraph(float lakeThreshold, float landRatio, int riverCount) {
      //TODO 소요 시간 감축
      yield return StartCoroutineOwnerless(BuildGraph(main));
      yield return StartCoroutineOwnerless(AssignCornerElevations(main, landRatio));
      yield return StartCoroutineOwnerless(AssignOceanCoastAndLand(lakeThreshold));
      yield return StartCoroutineOwnerless(RedistributeElevations());

      yield return StartCoroutineOwnerless(AssignPolygonElevations());

      // Determine downslope paths.
      yield return StartCoroutineOwnerless(CalculateDownslopes());

      // Determine watersheds: for every corner, where does it flow
      // out into the ocean? 
      yield return StartCoroutineOwnerless(CalculateWatersheds());

      // Create rivers.
      yield return StartCoroutineOwnerless(CreateRivers(riverCount));
      // Determine moisture at corners, starting at rivers
      // and lakes, but not oceans. Then redistribute
      // moisture to cover the entire range evenly from 0.0
      // to 1.0. Then assign polygon moisture as the average
      // of the corner moisture.
      yield return StartCoroutineOwnerless(AssignCornerMoisture());
      // RedistributeMoisture();
      yield return StartCoroutineOwnerless(AssignPolygonMoisture());
      
      progress.state = Progress.State.SettingBiomes;
      foreach (var c in main.centers) {
        c.biome = GetBiome(c);
        if (Elapsed) yield return null;
      }

      progress.state = Progress.State.FinishingGraphGenerating;
    }
    /*/// <summary>런타임에 그래프를 그립니다.</summary>
    public IEnumerator InitGraph(float lakeThreshold, float landRatio, int riverCount, MonoBehaviour mono) {
      //TODO 소요 시간 감축
      yield return mono.StartCoroutine(BuildGraph(main));
      if (mask != null) {
        yield return mono.StartCoroutine(BuildGraph(mask));
        yield return mono.StartCoroutine(AssignCornerElevations(main, width, height, landRatio, mask));
      }
      else yield return mono.StartCoroutine(AssignCornerElevations(main, width, height, landRatio));
      yield return mono.StartCoroutine(AssignOceanCoastAndLand(lakeThreshold));
      yield return mono.StartCoroutine(RedistributeElevations());

      yield return mono.StartCoroutine(AssignPolygonElevations());

      // Determine downslope paths.
      yield return mono.StartCoroutine(CalculateDownslopes());

      // Determine watersheds: for every corner, where does it flow
      // out into the ocean? 
      yield return mono.StartCoroutine(CalculateWatersheds());

      // Create rivers.
      yield return mono.StartCoroutine(CreateRivers(riverCount));
      // Determine moisture at corners, starting at rivers
      // and lakes, but not oceans. Then redistribute
      // moisture to cover the entire range evenly from 0.0
      // to 1.0. Then assign polygon moisture as the average
      // of the corner moisture.
      yield return mono.StartCoroutine(AssignCornerMoisture());
      // RedistributeMoisture();
      yield return mono.StartCoroutine(AssignPolygonMoisture());

      progress.state = Progress.State.SettingBiomes;
      foreach (var c in main.centers) {
        c.biome = GetBiome(c);
        if (elapsed) yield return null;
      }

      progress.state = Progress.State.FinishingGraphGenerating;
    }*/


    #region Graph Building
    private IEnumerator BuildGraph(Variables variables) {
      #region comment
      /* 원문 :
        * Build graph data structure in 'edges', 'centers', 'corners',
        * based on information in the Voronoi results: point.neighbors
        * will be a list of neighboring points of the same type (corner
        * or center); point.edges will be a list of edges that include
        * that point. Each edge connects to four points: the Voronoi edge
        * edge.{v0,v1} and its dual Delaunay triangle edge edge.{d0,d1}.
        * For boundary polygons, the Delaunay edge will have one null
        * point, and the Voronoi edge may be null.
      */

      /* 해석 + 참고 :
       * Voronoi 다이어그램을 이용해 'edge', 'center', 'corner' 그래프 데이터를 생성합니다.
       * point.neighbors는 각 개체와 동일 타입(Corner는 Corner, Center는 Center)의 인접 개체를 나타냅니다.
       * point.edges는 해당 점을 포함하는 Edge 개체 리스트를 나타냅니다.
       * 각 Edge는 4개의 점을 연결합니다: Voronoi Edge{v0, v1}와, 그로부터 산출된 Delunay edge{d0, d1}입니다.
       ? Voronoi Diagram과 Delaunay Triangle은 서로 듀얼 이미지(dual image) 관계에 있어, 하나로 다른 한 쪽을 구할 수 있습니다.
       * 참고) 경계선 폴리곤에서는 Delaunay Edge는 둘 중 하나가 null을, Voronoi Edge는 모두가 null을 가질 것입니다.
      */
      #endregion

      var libedges = variables.voronoi.Edges;

      // Build Center objects for each of the points, and a lookup map
      // to find those Center objects again as we build the graph
      progress.state = Progress.State.BuildingGraphPoints;
      foreach (var point in variables.points) {
        var p = new Center { index = variables.centers.Count, point = point };
        variables.centers.Add(p);
        variables.centerLookup[point] = p;
        if (Elapsed) yield return null;
      }

      // Workaround for Voronoi lib bug: we need to call region()
      // before Edges or neighboringSites are available
      progress.state = Progress.State.BuildingGraphCenters;
      foreach (var p in variables.centers) {
        variables.voronoi.Region(p.point);
        if (Elapsed) yield return null;
      }

      progress.state = Progress.State.BuildingGraphDelunayPoints;
      progress.currentProgressCount.y = libedges.Count;
      int count = 0;
      foreach (var libedge in libedges) {
        //TODO 소요 시간 감축
        var dedge = libedge.DelaunayLine();
        var vedge = libedge.VoronoiEdge();

        // Fill the graph data. Make an Edge object corresponding to
        // the edge from the voronoi library.
        var edge = new Edge {
          index = variables.edges.Count,
          river = 0,

          // Edges point to corners. Edges point to centers. 
          v0 = vedge.p0.HasValue ? MakeCorner(vedge.p0.Value, variables.corners, variables.cornerMap) : null,
          v1 = vedge.p1.HasValue ? MakeCorner(vedge.p1.Value, variables.corners, variables.cornerMap) : null,
          d0 = main.centerLookup[dedge.p0.Value],
          d1 = main.centerLookup[dedge.p1.Value]
        };
        if (vedge.p0.HasValue && vedge.p1.HasValue) edge.midpoint = Vector2Extensions.Interpolate(vedge.p0.Value, vedge.p1.Value, 0.5f);

        variables.edges.Add(edge);
        #region Add Centers / Corners to Edges
        // Centers point to edges. Corners point to edges.
        if (edge.d0 != null) { edge.d0.borders.Add(edge); }
        if (edge.d1 != null) { edge.d1.borders.Add(edge); }
        if (edge.v0 != null) { edge.v0.protrudes.Add(edge); }
        if (edge.v1 != null) { edge.v1.protrudes.Add(edge); }

        // Centers point to centers.
        if (edge.d0 != null && edge.d1 != null) {
          AddToCenterList(edge.d0.neighbors, edge.d1);
          AddToCenterList(edge.d1.neighbors, edge.d0);
        }

        // Corners point to corners
        if (edge.v0 != null && edge.v1 != null) {
          AddToCornerList(edge.v0.adjacent, edge.v1);
          AddToCornerList(edge.v1.adjacent, edge.v0);
        }

        // Centers point to corners
        if (edge.d0 != null) {
          AddToCornerList(edge.d0.corners, edge.v0);
          AddToCornerList(edge.d0.corners, edge.v1);
        }
        if (edge.d1 != null) {
          AddToCornerList(edge.d1.corners, edge.v0);
          AddToCornerList(edge.d1.corners, edge.v1);
        }

        // Corners point to centers
        if (edge.v0 != null) {
          AddToCenterList(edge.v0.touches, edge.d0);
          AddToCenterList(edge.v0.touches, edge.d1);
        }
        if (edge.v1 != null) {
          AddToCenterList(edge.v1.touches, edge.d0);
          AddToCenterList(edge.v1.touches, edge.d1);
        }
        #endregion

        progress.currentProgressCount.x = ++count;
        if (Elapsed) yield return null;
      }
      // GC
      variables.cornerMap = null;

      // TODO: use edges to determine these
      var topLeft = variables.centers.OrderBy(p => p.point.x + p.point.y).First();
      AddCorner(topLeft, 0, 0);

      var bottomRight = variables.centers.OrderByDescending(p => p.point.x + p.point.y).First();
      AddCorner(bottomRight, _size, _size);

      var topRight = variables.centers.OrderByDescending(p => _size - p.point.x + p.point.y).First();
      AddCorner(topRight, 0, _size);

      var bottomLeft = variables.centers.OrderByDescending(p => p.point.x + _size - p.point.y).First();
      AddCorner(bottomLeft, _size, 0);

      // required for polygon fill
      progress.state = Progress.State.BuildingGraphSortedCorners;
      foreach (var center in variables.centers) {
        center.corners.Sort(ClockwiseComparison(center));
        if (Elapsed) yield return null;
      }
    }

    private static void AddCorner(Center topLeft, int x, int y) { if (topLeft.point != new Vector2(x, y)) topLeft.corners.Add(new() { ocean = true, point = new Vector2(x, y) }); }

    private System.Comparison<Corner> ClockwiseComparison(Center center) => (a, b) => (int)(((a.point.x - center.point.x) * (b.point.y - center.point.y) - (b.point.x - center.point.x) * (a.point.y - center.point.y)) * 1000);

    private Corner MakeCorner(Vector2 point, List<Corner> corners, Dictionary<int, Dictionary<int, List<Corner>>> cornerMap)
    {
      // The Voronoi library generates multiple Point objects for
      // corners, and we need to canonicalize to one Corner object.
      // To make lookup fast, we keep an array of Points, bucketed by
      // x value, and then we only have to look at other Points in
      // nearby buckets. When we fail to find one, we'll create a new
      // Corner object.
      
      int pX = (int)point.x; int pY = (int)point.y;
      var keyList = new (int x, int y)[9] {
        (pX - 1, pY - 1), (pX, pY - 1), (pX + 1, pY - 1),
        (pX - 1, pY  ), (pX, pY  ), (pX + 1, pY  ),
        (pX - 1, pY + 1), (pX, pY + 1), (pX + 1, pY + 1)
      };

      Corner c = null;
      foreach (var (x, y) in keyList) {
        if (cornerMap.TryGetValue(x, out var yMap)) {
          if (yMap.TryGetValue(y, out var cs)) {
            if (cs.Any(corner => Vector2.Distance((c = corner).point, point) < 1e-3f)) return c;
          }
          else cornerMap[x][y] = new();
        }
        else cornerMap[x] = new();
      }
      c = new() {
        index = corners.Count,
        point = point,
        border = point.x == 0 || point.x == _size || point.y == 0 || point.y == _size
      };
      corners.Add(c);
      cornerMap[pX][pY].Add(c);
      return c;
    }

    private void AddToCornerList(List<Corner> v, Corner x) { if (x != null && v.IndexOf(x) < 0) v.Add(x); }

    private void AddToCenterList(List<Center> v, Center x) { if (x != null && v.IndexOf(x) < 0) { v.Add(x); } }
    #endregion

    #region Elevations
    private IEnumerator AssignCornerElevations(Variables variables, float landRatio) {
      //* 고도 설정
      #region comment
      /* 원문 :
       * Determine elevations and water at Voronoi corners. By
       * construction, we have no local minima. This is important for
       * the downslope vectors later, which are used in the river
       * construction algorithm. Also by construction, inlets/bays
       * push low elevation areas inland, which means many rivers end
       * up flowing out through them. Also by construction, lakes
       * often end up on river paths because they don't raise the
       * elevation as much as other terrain does.
      */

      /* 해석 :
       * Voronoi로 생성된 Corner들의 높이와 물 여부를 결정한다.
       * 물 생성 시 지역 최소점을 고려하지 않는다. (= 해수면보다 높은 호수를 갖지 않음)
       * 이는 다음의 세 가지 측면에서 그 이유를 가진다:
       * 이후 강 생성 알고리즘에서 사용되는 내리막 벡터에서 중요한 특징을 지닌다. (= 무조건 해수면 높이까지 흐르는 강이 생성됨)
       * 그리고 만(灣)은 저고도 내륙지역으로, 많은 강들이 이 지점에서 끝나게 된다. (= 파인 곳으로 강이 흐르는 자연적 특성을 반영할 수 있음)
       * 마지막으로 일부 호수로 강이 흐를 수 있는데, 이는 호수가 인접 육지보다 항상 낮은 높이를 갖기 때문이다. (호수 생성 알고리즘의 신뢰성 부여)
       ? (높은 고도에 호수를 생성할 때 높이 데이터만 사용하는 게 문제가 있나? -> 아마 특정 케이스에서 물이 범람하는 문제가 있는 것 같다.)
      */
      #endregion

      var queue = new Queue<Corner>();
      float goalRatio = landRatio, gap = 0f;
      //* 지정 높이 기준으로 각 Corner의 물 여부 결정, 원하는 육지 비율에 도달할 때까지 반복
      int _try = 0;
      progress.state = Progress.State.AssigningWaterLevel;
      if (hasMaskMap) do {
        //* Map이 고밀도일 경우 일정 개수의 Corner만 체크하여 저밀도 land 마스크를 생성합니다.
        int water = 0, maskRatio = _size/(int)Size.s4;
        Inside = IslandShape.MakePerlin(_size, _size, gap == 0f ? null : gap > 0f);

        Queue<Corner> elevation = new();
        var centersArray = main.centers.ToArray();
      
        //*  실제 water 여부 결정
        for (int i = 0; i < centersArray.Length; i += maskRatio) foreach (var c in centersArray[i].corners) {
          c._marked = true;
          elevation.Enqueue(c);
          if (c.water = !Inside(c.point)) water++;
          if (Elapsed) yield return null;
        }
        //* 모든 Corner를 마크합니다.
        while (elevation.Any()) {
          Corner c = elevation.Dequeue();
          foreach (var n in c.adjacent) if (!n._marked) {
            n._marked = true;
            n.water = c.water;
            elevation.Enqueue(n);
          }
          if (Elapsed) yield return null;
        }

        gap = 1 - ((float)water/variables.corners.Count) - goalRatio;
        // Debug.Log("land ratio: " + (gap + goalRatio).ToString() + ", goal ratio: " + goalRatio + ", water Level :" + IslandShape.SEA_LEVEL);
      } while (Mathf.Abs(gap) > 0.01f && _try++ < 10);
      else do {
        int water = 0;
        Inside = IslandShape.MakePerlin(_size, _size, gap == 0f ? null : gap > 0f);
        foreach (var q in variables.corners) {
          if (q.water = !Inside(q.point)) water += 1;
        }
        gap = 1 - ((float)water/variables.corners.Count) - goalRatio;
        if (Elapsed) yield return null;
        // Debug.Log("land ratio: " + (gap + goalRatio).ToString() + ", goal ratio: " + goalRatio + ", water Level :" + IslandShape.SEA_LEVEL);
      } while (Mathf.Abs(gap) > 0.01f && _try++ < 10);
      
      foreach (var q in variables.corners) {
        // 맵 경계면을 0으로 처리. 해안 작업에 추가
        if (q.border) {
          q.elevation = 0;
          queue.Enqueue(q);
        }
        // 나머지는 최대치로 초기화
        else q.elevation = float.PositiveInfinity;
      }

      //* 해안 설정
      #region comment
      /*
       * Traverse the graph and assign elevations to each point. As we
       * move away from the map border, increase the elevations. This
       * guarantees that rivers always have a way down to the coast by
       * going downhill (no local minima).
      */

      /*
       * Graph를 쭉 가로지르면서 각 점들의 높이를 할당합니다.
       * 맵 경계면에서 멀어지면서 높이를 증가시킵니다.
       * 이는 강이 낮은 곳으로 흐르면서 항상 해안으로 향하게 만듭니다. (지역 최소점 없음)
      */
      #endregion

      while (queue.Any())
      {
        //* 모든 Corner에 대해 다음의 작업을 수행합니다.
        var q = queue.Dequeue();
        //* 인접 Corner에 대해서...
        foreach (var s in q.adjacent) if (s.elevation > float.MaxValue) {
          float newElevation = q.elevation, rnd = Random.value;
          // 둘 중 한 쪽이라도 물이면 높이 변동 없음
          newElevation += (q.water || s.water) ? 0.01f * rnd : (_needMoreRandomness) ? 1 + rnd : 1 + 0.01f * rnd;
          // 높이 최초 설정 시 작업 추가
          s.elevation = newElevation;
          queue.Enqueue(s);
        }
        progress.state = Progress.State.AssigningCornerElevations;
        if (Elapsed) yield return null;
      }
    }

    private IEnumerator AssignOceanCoastAndLand(float lakeThreshold) {
      #region comment
      /* 원문
       * Compute polygon attributes 'ocean' and 'water' based on the
       * corner attributes. Count the water corners per
       * polygon. Oceans are all polygons connected to the edge of the
       * map. In the first pass, mark the edges of the map as ocean;
       * in the second pass, mark any water-containing polygon
       * connected an ocean as ocean.
      */

      /* 해석
       * 폴리곤의 'ocean'과 'water' 속성을 Corner의 속성에 따라 계산합니다.
       * 폴리곤 당 'water' 속성의 Corner 개수를 계산합니다.
       * 'ocean'은 맵의 경계면에 연결된 폴리곤입니다. (참고 : 'water'이면서 'ocean'이 아닌 폴리곤은 'lake'입니다.)
       * 첫 단계에서 모든 맵 경계면을 'ocean'으로 표시하고,
       * 두 번째 단계에서 'ocean'에 연결된 'water' 폴리곤을 'ocean'으로 표시합니다.
       ? 판정 매커니즘) 경계면 'ocean' 판정 -> 안쪽으로 들어가면서 'ocean' 판정 -> 'coast' 판정 -> 전범위 'water' 판정
      */
      #endregion

      //var p:Center, q:Corner, r:Center, numWater:int;

      //* 경계면 Center는 항상 'ocean' 속성이 부여됩니다.
      progress.state = Progress.State.AssigningCenterBorders;
      foreach(var c in main.centers) {
        if (c.corners.Any(q => q.border)) {
          c.border = true;
          c.ocean = true;
          c.corners.ForEach(q => q.water = true);
        }
        if (Elapsed) yield return null;
      }
      
      //? 맵의 미적 요소 보강을 위해 경계면으로부터 3칸 이내의 Center는 모두 'ocean' 속성이 부여됩니다.
      var n = 3 * (_size / (int)Size.s4);
      while (n-- > 0) main.centers.Where(p => p.ocean).ToList().ForEach(p => p.neighbors.ForEach(r => r.ocean = true));
      


      //* 폴리곤(Center)은 'ocean'이거나 'water'인 Corner를 지정된 비율 이상으로 가지고 있을 때 'water' 속성을 가집니다.
      main.centers.ForEach(p => { if (p.ocean || (float)p.corners.Count(c => c.water)/p.corners.Count() > lakeThreshold) p.water = true; });

      //* 경계면에 연결된 'water' 속성의 Center에 'ocean' 속성을 추가합니다.
      progress.state = Progress.State.AssigningCenterOceans;
      while (main.centers.Where(c => c.ocean).Count(o => o.neighbors.Count(r => {
        if (r.water && !r.ocean) { r.ocean = true; return true; }
        return false;
      }) > 0) > 0) { if (Elapsed) yield return null; }

      //* 'land' 속성의 Center와 'ocean' 속성의 Center 둘 모두를 이웃으로 갖고 있는 폴리곤은 'coast' 속성을 가집니다.
      progress.state = Progress.State.AssigningCenterCoasts;
      main.centers.ForEach(p => p.coast = p.neighbors.Exists(r => r.ocean) && p.neighbors.Exists(r => !r.water));
      if (Elapsed) yield return null;

      //* 마지막으로 Corner에 대해 모든 인접 폴리곤이 'ocean'이면 'ocean', 'land'면 'land', 다른 경우 'coast' 속성을 부여합니다.
      progress.state = Progress.State.AssigningCorners;
      foreach (var q in main.corners) {
        bool ocean = q.touches.Exists(p => p.ocean); // 인접 폴리곤에 ocean이 하나라도 있으면 true
        bool land = q.touches.Exists(p => !p.water); // 인접 폴리곤에 land가 하나라도 있으면 true
        q.ocean = ocean && !land; // ocean 인접이면서 land가 근처에 없으면 항상 ocean 판정.
        q.coast = ocean && land; // ocean 인접이면서 land가 근처에 있으면 coast 판정.
        q.water = !q.coast && !land; // land가 근처에 없으면서 coast가 아니면 water 판정.
        if (Elapsed) yield return null;
      }
    }

    private IEnumerator RedistributeElevations() {
      #region comment
      /* 원문
       * Change the overall distribution of elevations so that lower
       * elevations are more common than higher
       * elevations. Specifically, we want elevation X to have frequency
       * (1-X).  To do this we will sort the corners, then set each
       * corner to its desired elevation.
      */

      /* 해석
       * 전체 고도를 조정하여 낮은 고도가 높은 고도보다 더 많은 비율이 되도록 합니다.
       * 일반적으로 높이 X를 1-X의 비율로 분포되도록 합니다.
       * 따라서 Corner를 정렬한 후 각 Corner의 고도를 목표 고도로 재설정합니다.
      */
      #endregion

      const float SCALE_FACTOR = 1.2f; //고고도 영역을 증가시킵니다. 상위 20%의 고도 비율을 중심으로 이 값을 조정합니다.
      const float referenceDepth = -1f; // 기준 깊이 값을 의미합니다. 기본값은 상대 고도 1f에 대응하는 -1f입니다.
      //* Ocean, Coast도 아닌 모든 Corner에 대해 다음의 작업을 수행합니다. 'lake' 여부와 무관합니다.
      var locations = main.LandCorners.OrderBy(p => p.elevation).ToList();
      int count = locations.Count;
      //* 모든 Land Corner를 고도 오름차순으로 조정한 뒤 적절한 비율로 해발고도를 입력합니다.
      progress.state = Progress.State.RedistributeLandCornerElevations;
      for (int p = 0; p < count; p++) { 
        locations[p].elevation = 1 - Mathf.Pow(1 - (float)p/count, 0.5f * SCALE_FACTOR);
        if (Elapsed) yield return null;
      }

      //* Land가 아닌 Corner의 고도를 조정합니다.
      //? 기존 코드에서는 0으로 초기화하였으나, 필요에 따라 점차 음의 고도를 갖게 조정합니다.
      float maxDepth = main.WaterCorners.Max(x => x.elevation); // 가장 높은 고도를 의미하며, 변환 시 가장 작은 절댓값을 가집니다.
      float factor = referenceDepth / (maxDepth - main.OceanCorners.Min(x => x.elevation));
      progress.state = Progress.State.RedistributeWaterCornerElevations;
      foreach (var x in main.OceanCorners) {
        x.elevation = (maxDepth - x.elevation) * factor;
        if (Elapsed) yield return null;
      }
      //? 경계면의 Corner는 모두 -1의 고도를 갖게 됩니다.
    }

    private IEnumerator AssignPolygonElevations() {
      progress.state = Progress.State.AssignCenterElevations;
      foreach (var c in main.centers) {
        c.elevation = c.corners.Average(x => x.elevation);
        if (Elapsed) yield return null;
      }
    }

    private IEnumerator CalculateDownslopes() {
      //* 모든 Corner에 대해 가장 낮은 인접 Corner를 설정합니다. 없을 경우 자기 자신을 기본값으로 가집니다.
      progress.state = Progress.State.CalculatingDownslopes;
      foreach (var c in main.corners) {
        Corner lowest = c.adjacent.OrderBy(x => x.elevation).First();
        c.downslope = lowest.elevation < c.elevation ? lowest : c;
        if (Elapsed) yield return null;
      }
    }

    private IEnumerator CalculateWatersheds() {
      #region comment
      /* 원문
       * Calculate the watershed of every land point. The watershed is
       * the last downstream land point in the downslope graph. TODO:
       * watersheds are currently calculated on corners, but it'd be
       * more useful to compute them on polygon centers so that every
       * polygon can be marked as being in one watershed.
      */

      /* 해석
       * 모든 land 지점의 분수령을 계산합니다. 분수령은 Downslope 그래프의 마지막 도착 land 지점입니다.
       * TODO : 분수령은 현재 corner에서 계산되지만, polygon의 중심으로 계산하는 것이 더 유용할 것입니다.
      */
      #endregion

      //* 모든 Corner의 분수령을 계산하고 제거를 용이하게 하기 위해 LinkedList를 사용합니다.
      LinkedList<Corner> lands = new(main.LandCorners);
      int count = 0;
      progress.state = Progress.State.CalculatingWatersheds;
      do {
        var current = lands.First;
        while (current != null) {
          var next = current.Next;
          Corner p = current.Value, pW = p.watershed;
          //* downslope를 따라 흐른 watershed가 그대로이거나(해변 or 지역적 최소 높이) 물에 다다랐으면 종료.
          if ((p.watershed = p.watershed.downslope) == pW || p.watershed.notLand) lands.Remove(current);
          current = next;
          if (Elapsed) yield return null;
        }
      } while(lands.Count > 0 && ++count < 1000);
      //? 위 반복문은 (500, 500) 크기의 맵(육지 비율 50%)에서 최대 40회 정도 수행됩니다.
      //? 모든 고도가 겹치지 않게 작성하여 무한 반복을 피했으나 만약을 위해 최대 1000회를 수행하도록 하였습니다.

      //* 분수령으로 지정된 Corner의 크기를 설정합니다.
      foreach (var q in main.corners) q.watershed.watershed_size++;
      // Debug.Log(LandCorners.Max(p => p.watershed.watershed_size) + "," + count);
    }
    #endregion

    #region Moisture
    private IEnumerator CreateRivers(int count) {
      /*
       * Edge를 따라서 강을 생성합니다. 무작위 Corner를 선택하고, downslope를 따라 이동시킵니다.
       * 지나가는 모든 Edge와 Corner를 강으로 설정합니다.
      */
      progress.state = Progress.State.CalculatingRivers;
      for (var i = 0; i < count; i++) {
        Corner q;
        //* 일정 고도 범위 내 육지 Corner를 선택합니다.
        do q = main.corners[Random.Range(0, main.corners.Count)]; while (q.ocean || q.elevation < 0.3 || q.elevation > 0.9);
        // Bias rivers to go west: if (q.downslope.x > q.x) continue;
        //* downslope로 이동하면서 더 이상 진행할 수 없거나 해안에 도달하기 전까지 강으로 설정합니다.
        while (!q.coast && q != q.downslope) {
          var edge = FindIntersectionEdge(q, q.downslope);
          edge.river++;
          q.river++;
          q = q.downslope;
        }
        if (Elapsed) yield return null;
      }
    }

    private IEnumerator AssignCornerMoisture()
    {
      #region comment
      /* 원문
       * Calculate moisture. Freshwater sources spread moisture: rivers
       * and lakes (not oceans). Saltwater sources have moisture but do
       * not spread it (we set it at the end, after propagation).
      */

      /* 해석
       * 습도를 계산합니다. 담수(강/호수)는 습기를 퍼트립니다.
       * 해수는 습기를 가지나 퍼트리지는 않습니다. (propagation 이후에 설정됩니다)
      */
      #endregion

      var queue = new Queue<Corner>();
      
      //* 담수 습도 설정. (다른 Corner의 moisture는 0으로 초기화되어 있음)
      progress.state = Progress.State.CalculatingFreshwaterMoisture;
      foreach (var corner in main.corners.Where(p => (p.water || p.river > 0) && !p.ocean)) {
        corner.moisture = corner.river > 0 ? Mathf.Min(10, 0.75f*corner.river, 2) : 1.0f;
        queue.Enqueue(corner); // ↑ 15 이상의 river에서 습도가 최대치에 달함. //TODO 추후 습도 적정량으로 조절
        if (Elapsed) yield return null;
      }
      //* 담수 근처 육지 습도 설정
      progress.state = Progress.State.CalculatingLandMoisture;
      while (queue.Any()) {
        var q = queue.Dequeue();
        float adjacentMoisture = q.moisture * 0.9f;

        foreach (var r in q.adjacent.Where(s => s.moisture < adjacentMoisture)) {
          r.moisture = adjacentMoisture;
          queue.Enqueue(r);
        }
        if (Elapsed) yield return null;
      }

      //* 해수 습도 설정
      foreach (var c in main.OceanCorners) c.moisture = 1f;
    }

    private IEnumerator AssignPolygonMoisture() {
      //* 폴리곤의 습도는 Corner 습도의 평균입니다. (각 Corner는 최대 1의 가중치를 갖습니다.)
      progress.state = Progress.State.CalculatingCenterMoisture;
      foreach (var c in main.centers) {
        c.moisture = c.corners.Average(q => Mathf.Min(q.moisture, 1.0f));
        if (Elapsed) yield return null;
      }
    }

    //? 습도를 재분배할 이유가 있을까
    private void RedistributeMoisture()
    {
      // Change the overall distribution of moisture to be evenly distributed.
      var locations = main.LandCorners.ToList();
      locations.Sort((a, b) => a.moisture.CompareTo(b.moisture));

      for (var i = 0; i < locations.Count; i++)
      {
        locations[i].moisture = (float)i / (locations.Count - 1);
      }
    }
    #endregion

    #region Simple Methods
    /// <summary>두 폴리곤에 모두 포함된 Edge를 찾습니다. 찾지 못하면 null을 반환합니다.</summary>
    private Edge FindIntersectionEdge(Center p, Center r) {
      foreach (var edge in p.borders) if (edge.d0 == r || edge.d1 == r) return edge;
      return null;
    }
    /// <summary>두 Corner에 모두 걸쳐 있는 Edge를 찾습니다. 찾지 못하면 null을 반환합니다.</summary>
    private Edge FindIntersectionEdge(Corner q, Corner s) {
      foreach (var edge in q.protrudes) if (edge.v0 == s || edge.v1 == s) return edge;
      return null;
    }

    Biome GetBiome(Center p)
    {
      //TODO 비율 조정 기능 추가
      if (p.ocean) return Biome.Ocean;
      if (p.coast) return Biome.Beach;
      if (p.water) return p.elevation < 0.1f ? Biome.Marsh : p.elevation < 0.8f ? Biome.Lake : Biome.Ice;

      return (p.elevation, p.moisture) switch {
        (> .8f, > .50f) => Biome.Snow,
        (> .8f, > .33f) => Biome.Tundra,
        (> .8f, > .16f) => Biome.Bare,
        (> .8f,      _) => Biome.Scorched,
        (> .6f, > .66f) => Biome.Taiga,
        (> .6f, > .33f) => Biome.Shrubland,
        (> .6f,      _) => Biome.TemperateDesert,
        (> .3f, > .83f) => Biome.TemperateRainForest,
        (> .3f, > .50f) => Biome.TemperateDeciduousForest,
        (> .3f, > .16f) => Biome.Grassland,
        (> .3f,      _) => Biome.TemperateDesert,
        (    _, > .66f) => Biome.TropicalRainForest,
        (    _, > .33f) => Biome.TropicalSeasonalForest,
        (    _, > .16f) => Biome.Grassland,
        (    _,      _) => Biome.SubtropicalDesert,
      };
    }

    /// <summary>주어진 점 집합의 Voronoi Diagram 연산 결과를 반환합니다. 반복 수행 시 점이 보다 균일하게 배열됩니다.</summary>
    public static void RelaxPoints(Vector2[] points, float width, float height) {
      Voronoi v = new(points, null, new Rect(0, 0, width, height));
      for (int i = 0; i < points.Length; i++) {
        var p = points[i];
        var region = v.Region(p);
        points[i] = new Vector2(region.Average(q => q.x), region.Average(q => q.y));
      }
    }
    #endregion
  }
}
