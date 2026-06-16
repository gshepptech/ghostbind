using GhostBind.Core.Mapping;

namespace GhostBind.Core.Output;

// Abstraction over X360 / DS4 virtual controllers. MappingEngine writes via this
// so swapping output type doesn't ripple into the mapping code.
public interface IVirtualPad : IDisposable
{
    void Reset();                                            // clear all buttons (called every frame)
    void SetButton(OutputButton button, bool pressed);
    void SetThumb(bool right, short x, short y);             // false = left
    void SetTrigger(bool right, byte value);
    byte LargeMotor { get; }                                 // game-driven rumble (low frequency)
    byte SmallMotor { get; }                                 // game-driven rumble (high frequency)
    void Submit();
}
