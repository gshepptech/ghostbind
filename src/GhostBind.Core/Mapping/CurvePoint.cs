namespace GhostBind.Core.Mapping;

public struct CurvePoint
{
    public double Input;   // 0..1, where the stick magnitude lands
    public double Output;  // 0..1, what we send to the game at that input
}
