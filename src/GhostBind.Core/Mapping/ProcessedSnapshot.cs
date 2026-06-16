using GhostBind.Core.Input;

namespace GhostBind.Core.Mapping;

public struct ProcessedSnapshot
{
    public DualSenseState Raw;

    public double LeftRawX;
    public double LeftRawY;
    public double LeftX;
    public double LeftY;

    public double RightRawX;
    public double RightRawY;
    public double RightX;
    public double RightY;

    public double LeftTriggerRaw;
    public double RightTriggerRaw;
    public double LeftTrigger;
    public double RightTrigger;
}
