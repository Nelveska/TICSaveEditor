using System.Runtime.InteropServices;

namespace TICSaveEditor.Core.Records.Layouts;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct EquipSetLayout
{
    public fixed byte Name[66];
    public fixed byte ItemBytes[10];
    public fixed byte AbilityBytes[10];
    public byte Job;
    public byte IsDoubleHand;
}
