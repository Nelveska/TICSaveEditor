using System.Runtime.InteropServices;

namespace TICSaveEditor.Core.Records.Layouts;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct UnitSaveDataLayout
{
    // 0x00..0x07 — identity bytes
    public byte Character;
    public byte UnitIndex;
    public byte Job;
    public byte Union;
    public byte Sex;
    public byte Birthday;
    public byte ZodiacSign;
    public byte SecondaryAction;

    // 0x08..0x0D — ability ids (u16 LE)
    public ushort ReactionAbility;
    public ushort SupportAbility;
    public ushort MovementAbility;

    // 0x0E..0x1B — equipment (7 × u16)
    public fixed ushort EquipItem[7];

    // 0x1C..0x1F — progression
    public byte Exp;
    public byte Level;
    public byte StartBcp;
    public byte StartFaith;

    // 0x20..0x31 — base stats (6 × 24-bit LE)
    public fixed byte HpMaxBase[3];
    public fixed byte MpMaxBase[3];
    public fixed byte WtBase[3];
    public fixed byte AtBase[3];
    public fixed byte MatBase[3];
    public fixed byte UnlockedJobs[3];

    // 0x32..0x73 — ability flags (22 × 3 bytes = 66)
    public fixed byte AbilityFlag[66];

    // 0x74..0x7F — job levels (12 × u8)
    public fixed byte JobLevel[12];

    // 0x80..0xAD — job points (23 × u16 = 46)
    public fixed ushort JobPoint[23];

    // 0xAE..0xDB — total job points (23 × u16 = 46)
    public fixed ushort TotalJobPoint[23];

    // 0xDC..0xEB — UnitNickname (16 bytes; player-set rename string, ASCII null-terminated)
    public fixed byte UnitNickname[16];

    // 0xEC..0xFB — CustomJobName (16 bytes; empirically zero-filled across all observed saves)
    public fixed byte CustomJobName[16];

    // 0xFC..0x11B — anonymous trailing region (32 bytes; community struct field_FC)
    public fixed byte UnitNameTrailing[32];

    // 0x11C..0x11D — name_no (locale lookup index)
    public ushort NameNo;

    // 0x11E..0x125 — slot/team metadata
    public byte InTrip;
    public byte Parasite;
    public byte EggColor;
    public byte PspKilledNum;
    public byte UnitOrderId;
    public byte UnitStartingTeam;
    public byte UnitJoinId;
    public byte CurrentCombatSet;

    // 0x126..0x22D — 3 × CombatSetLayout (88 each = 264). Decomposed:
    // Name[16] + NamePadding[50] + Rh/Lh/Head/Armor/Accessory (5 × u16) +
    // Skillsets[4] + Abilities[6] + Job + IsDoubleHand.
    public CombatSetLayout CombatSet0;
    public CombatSetLayout CombatSet1;
    public CombatSetLayout CombatSet2;

    // 0x22E..0x257 — trailing
    public ushort Pad;
    public ushort CharaNameKey;
    public fixed byte Pad2[38];
}
