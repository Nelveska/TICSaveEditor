using System.Runtime.InteropServices;

namespace TICSaveEditor.Core.Records.Layouts;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct CombatSetLayout
{
    public fixed byte Name[16];           // 0x00..0x0F — 16-byte fixed-length ASCII (community, no null terminator within)
    public fixed byte NamePadding[50];    // 0x10..0x41 — preserved opaque (community)
    public fixed byte ItemBytes[10];      // 0x42..0x4B — 5 × u16 (RHWeapon/LHShield/Head/Armor/Accessory)
    public fixed byte Skillsets[4];       // 0x4C..0x4F — 2 × i16 (community decomposition, 2026-05-01)
    public fixed byte Abilities[6];       // 0x50..0x55 — 3 × u16 (Reaction/Support/Movement)
    public byte Job;                      // 0x56
    public byte IsDoubleHand;             // 0x57
}
