namespace GhostBind.Core.Mapping;

// PS5 adaptive trigger feedback modes. Values match the DualSense USB output
// report's trigger-mode byte. Documented in DualSense-Windows (Ohjurot, MIT) and
// the community PS5 protocol notes.
public enum TriggerEffectMode : byte
{
    Off = 0x00,
    Rigid = 0x01,    // continuous resistance throughout pull (param: start, force)
    Section = 0x02,  // resistance only between two positions (param: start, end)
    Weapon = 0x25,   // gun-trigger feel: resist until breakthrough (params: start, end, force)
}
