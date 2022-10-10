using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Map
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
        public static float SEA_LEVEL;
        static float SEA_LEVEL_GAP;
        const float frequency = 2f;

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

        // The Perlin-based island combines perlin noise with the radius
        /// <summary>
        /// Perlin Noise를 기반으로 섬의 형태(외곽)을 생성하는 함수를 반환합니다.<br/>
        /// 크기가 커지면 형태가 단순해지므로 저밀도의 마스크를 별도로 사용하는 것을 권장합니다.
        /// </summary>
        public static System.Func<Vector2, bool> MakePerlin(float width, float height, bool? gapDirection = null)
        {
            // 펄린 노이즈 맵은 주어진 위치에 항상 동일한 값을 가지므로 랜덤 오프셋을 시드로 설정합니다.
            float offset = Random.value * 10000;
            Vector2 freq = new Vector2(1/width, 1/height) * frequency * ZOOM_FACTOR * (Mathf.Min(width, height) / (int)Size.s4);
            //? preset : seed 1404172320 / frequency/width^0.9f

            if (gapDirection == null) { SEA_LEVEL = 0.5f; SEA_LEVEL_GAP = 0.25f; }
            else {
                SEA_LEVEL += (bool)gapDirection ? SEA_LEVEL_GAP : -SEA_LEVEL_GAP;
                SEA_LEVEL_GAP /= 2f;
            }

            return q => {
                float perlin = Mathf.PerlinNoise((q.x + offset)*freq.x, (q.y + offset)*freq.y); // 이 값은 0 ~ 1 사이의 값을 가지게 됩니다.
                Vector2 dist = new(q.x*2/width - 1, q.y*2/height - 1); // distance from center (in ratio) : -1 ~ 1 사이의 값을 가집니다.
                var ground = PERLIN_CHECK_VALUE - PERLIN_CHECK_VALUE * dist.sqrMagnitude; // 이 값은 적절한 비율로 조정됩니다.
                return perlin + ground > SEA_LEVEL;
            };
        }
        public static float PERLIN_CHECK_VALUE = 0.3f;
        private const float ZOOM_FACTOR = 2.5f;

        // The square shape fills the entire space with land
        public static System.Func<Vector2, bool> MakeSquare() => _ => true;
    }
}
