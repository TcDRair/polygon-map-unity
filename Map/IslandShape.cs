using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Maps
{
  public class IslandShape
  {
    // This class has factory functions for generating islands of
    // different shapes. The factory returns a function that takes a
    // normalized point (x and y are -1 to +1) and returns true if the
    // point should be on the island, and false if it should be water
    // (lake or ocean).

    // The radial island radius is based on overlapping sine waves 
    public static float ISLAND_FACTOR = 1.07f;  // 1.0 means no small islands; 2.0 leads to a lot
    const float FREQUENCY = 2f;

    public static System.Func<Vector2, bool> MakeRadial()
    {
      var bumps = Random.Range(1, 6);
      var startAngle = Random.value * 2 * Mathf.PI;
      var dipAngle = Random.value * 2 * Mathf.PI;

      var random = Random.value;
      var start = 0.2f;
      var end = 0.7f;

      var dipWidth = (end - start) * random + start;

      return q =>
      {
        var angle = Mathf.Atan2(q.y, q.x);
        var length = 0.5 * (Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y)) + q.magnitude);

        var r1 = 0.5 + 0.40 * Mathf.Sin(startAngle + bumps * angle + Mathf.Cos((bumps + 3) * angle));
        var r2 = 0.7 - 0.20 * Mathf.Sin(startAngle + bumps * angle - Mathf.Sin((bumps + 2) * angle));
        if (Mathf.Abs(angle - dipAngle) < dipWidth
          || Mathf.Abs(angle - dipAngle + 2 * Mathf.PI) < dipWidth
          || Mathf.Abs(angle - dipAngle - 2 * Mathf.PI) < dipWidth)
        {
          r1 = r2 = 0.2;
        }
        var result = length < r1 || (length > r1 * ISLAND_FACTOR && length < r2);
        return result;
      };
    }

    public static float SeaLevel { get; private set; }
    /// <summary>Perlin Noise를 기반으로 섬의 육지를 판별하는 함수를 반환합니다.</summary>
    public static System.Func<Vector2, bool> MakePerlin(float size)
    {
      // 펄린 노이즈 맵은 주어진 위치에 항상 동일한 값을 가지므로 랜덤 오프셋을 시드로 설정합니다.
      Vector2 offset = Random.value * new Vector2(10000, 10000);
      var freq = new Vector2(1/size, 1/size) * FREQUENCY * ZOOM_FACTOR * size / (int)Size.s4;
      //? preset : seed 1404172320 / frequency/width^0.9f

      return q => {
        var p = (q + offset) * freq;
        float perlin = Mathf.PerlinNoise(p.x, p.y); // 0 ~ 1 사이의 값을 가집니다.
        float dist = new Vector2(q.x*2/size - 1, q.y*2/size - 1).sqrMagnitude;
        var ground = MIDPOINT_VALUE * (1 - dist); // 중심에서의 거리에 반비례하는 값을 가집니다.
        return perlin + ground > SeaLevel;
      };
    }
    private static float _gap;
    public static void SetPerlin(float targetRatio, float resultRatio) {
      SeaLevel += (targetRatio < resultRatio) ? _gap : -_gap;
      _gap /= 2;
    }
    public static void ResetPerlin() { SeaLevel = .5f; _gap = .25f; }
    public static float MIDPOINT_VALUE = 0.25f;
    private const float ZOOM_FACTOR = 2.5f;

    // The square shape fills the entire space with land
    public static System.Func<Vector2, bool> MakeSquare() => _ => true;
  }
}
