using System.Linq;
using UnityEngine;

namespace Assets.Util {
public interface IProgressTimerProvider { ProgressTimer Timer { get; } }
[System.Serializable]
public class ProgressTimer {
  public readonly string name;
  public int Current { get; private set; } = 0;
  readonly string[] label;
  readonly float[] ratio;
  readonly bool[] detail;
  (int x, int y) detailStep;

  public ProgressTimer(string name, params (string label, float ratio, bool detail)[] steps) {
    this.name = name;
    label  = steps.Select(p => p.label ).Append("Finished.").ToArray();
    ratio  = steps.Select(p => p.ratio ).Append(     1     ).ToArray();
    detail = steps.Select(p => p.detail).Append(   false   ).ToArray();
  }

  public void Next() => Current += Finished ? 0 : 1;
  public void SetDetail(int x, int y) => detailStep = (x, y);
  public void Reset() => Current = 0;

  public float CurrentRatio
    => detail[Current]
      ? ratio[Current] + (float)detailStep.x/detailStep.y * (ratio[Current+1] - ratio[Current])
      : ratio[Current];
  public override string ToString()
    => detail[Current]
      ? $"[{name}] {label[Current]} ({detailStep.x}/{detailStep.y})"
      : $"[{name}] {label[Current]}";
  
  private static float lastTick = 0;
  private const float TICK = .05f;
  public static bool TimerElapsed
    => Time.realtimeSinceStartup - lastTick > TICK && (lastTick = Time.realtimeSinceStartup) > 0;
  public bool Elapsed => TimerElapsed;
  public bool Finished => Current == label.Length - 1;
}
}