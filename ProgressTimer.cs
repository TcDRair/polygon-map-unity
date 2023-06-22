using System.Linq;
using UnityEngine;

namespace Assets.Util {
public interface IProgressTimerProvider { ProgressTimer Timer { get; } }
public class ProgressTimer
{
  public readonly string name;
  private readonly (string name, float ratio, bool detail)[] steps;
  private (int x, int y) detailStep;
  public int Current { get; private set; } = 0;

  public ProgressTimer(string name, params (string, float, bool)[] steps) {
    this.name = name;
    this.steps = steps.Append(("Finished", 1, false)).ToArray();
  }

  public void Next() => Current += Finished ? 0 : 1;
  public void SetDetail(int x, int y) => detailStep = (x, y);
  public void Reset() => Current = 0;

  public float CurrentRatio
    => steps[Current].detail
      ? steps[Current].ratio + (float)detailStep.x/detailStep.y * (steps[Current+1].ratio - steps[Current].ratio)
      : steps[Current].ratio;
  public override string ToString()
    => steps[Current].detail
      ? $"[{name}] {steps[Current].name}"
      : $"[{name}] {steps[Current].name} ({detailStep.x}/{detailStep.y})";
  
  private static float lastTick = 0;
  private const float TICK = .05f;
  public static bool TimerElapsed
    => Time.realtimeSinceStartup - lastTick > TICK && (lastTick = Time.realtimeSinceStartup) > 0;
  public bool Elapsed => TimerElapsed;
  public bool Finished => Current == steps.Length - 1;
}
}