using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Map
{
    public class MapTexture
    {
        int _textureScale;
        public MapTexture(int textureScale)
        {
            _textureScale = textureScale;
        }

        public class Progress {
            /// <summary>Graph 작업의 전체적인 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 실제 시간과 일치하지 않을 수 있습니다.</summary>
            public float totalProgress {
                get {
                    //? 아래 기술된 비율은 몇 번의 테스트 이후 대략적인 시간적 비율을 나타내도록 합시다.
                    switch(state) {
                        case State.NotStarted: return 0f;
                        case State.FillingPolygons: return 0.95f * CurrentProgress;
                        case State.DrawingLines: return 0.95f;
                        case State.DrawingRivers: return 0.98f;
                        case State.Finished: return 1f;
                        default: return 1f;
                    }
                }
            }
            /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>0에서 1 사이의 비율로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
            public float CurrentProgress => (float)currentProgressCount.x/currentProgressCount.y;
            /// <summary>현재 진행중인 반복문의 진행도를 나타냅니다.<br/>Vector2(n, m)으로 나타나며, 많은 시간이 걸리는 반복문에만 적용됩니다.</summary>
            public Vector2Int currentProgressCount;
            public enum State {
                NotStarted,
                FillingPolygons, DrawingLines, DrawingRivers, Applying,
                Finished,
            } public State state = State.NotStarted;
            public bool HasStarted => state != State.NotStarted;

            public override string ToString() {
                if (state == State.FillingPolygons)
                    return "[Texture] " + state.ToString().ToNiceString() + " " + currentProgressCount.ToNiceString();
                else return "[Texture] " + state.ToString().ToNiceString();
            }
        } public Progress progress = new();
        public void TimeLog() => Debug.Log(DateTime.Now.ToString("ss.fff"));
        float _prevTime;
        const float DELTA_TIME = 0.01f;
        bool Elapsed {
            get {
                bool e = Time.realtimeSinceStartup - _prevTime > DELTA_TIME;
                if (e) _prevTime = Time.realtimeSinceStartup;
                return e;
            }
        }

        public Texture2D texture { get; private set; }
        /// <summary>주어진 Map 개체에 대한 시각적 정보를 반환합니다.</summary>
        public IEnumerator CreateMapMaterial(Map map)
        {
            int _textureWidth = map.Width * _textureScale;
            int _textureHeight = map.Height * _textureScale;
            texture = new Texture2D(_textureWidth, _textureHeight);
            var lines = map.Graph.main.edges.Where(p => p.v0 != null).Select(p => new[] 
            { 
                p.v0.point.x, p.v0.point.y,
                p.v1.point.x, p.v1.point.y
            }).ToArray();

            progress.currentProgressCount.y = map.Graph.main.centers.Count;
            int count = 0;
            foreach (var c in map.Graph.main.centers) {
                texture.FillPolygon(c.corners.Select(p => new Vector2(p.point.x * _textureScale, p.point.y * _textureScale)).ToArray(), BiomeProperties.Colors[c.biome]);

                progress.currentProgressCount.x = ++count;
                if (Elapsed) {
                    progress.state = Progress.State.FillingPolygons;
                    yield return null;
                }
            }
            
            /*
            //? Some dirty lines are being drawning...
            foreach (var line in lines) {
                DrawLine(texture, line[0], line[1], line[2], line[3], Color.black);
                if (Elapsed) {
                    progress.state = Progress.State.DrawingLines;
                    yield return null;
                }
            }*/
            
            foreach (var line in map.Graph.main.edges.Where(p => p.river > 0 && !p.d0.water && !p.d1.water)) {
                DrawLine(texture, line.v0.point.x, line.v0.point.y, line.v1.point.x, line.v1.point.y, Color.blue);
                if (Elapsed) {
                    progress.state = Progress.State.DrawingRivers;
                    yield return null;
                }
            }
            progress.state = Progress.State.Applying;
            yield return null;
            texture.Apply();
            progress.state = Progress.State.Finished;
        }

        private void DrawLine(Texture2D texture, float x0, float y0, float x1, float y1, Color color)
        {
            texture.DrawLine((int)(x0 * _textureScale), (int)(y0 * _textureScale), (int)(x1 * _textureScale), (int)(y1 * _textureScale), color);
        }
    }
}
