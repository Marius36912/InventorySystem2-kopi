using System;
using System.Globalization;

namespace InventorySystem2.Models;

public static class RobotPositions
{
    // -------------------------------------------------------
    // Location IDs (match ItemEntity.InventoryLocation in DB)
    // -------------------------------------------------------
    // 1 = Location A (fx White Shell / Base stack)
    // 2 = Location B (fx Black Shell / Lid stack)
    // 3 = Location C (fx Electronics White)
    // 4 = Location D (fx Electronics Black)
    //
    // 5 = Assembly (robot target)
    // 6 = Output (robot target)
    //
    // I kan ændre betydningen, bare I holder numrene konsistente
    // mellem DbSeeder og RobotPositions mapping.
    // -------------------------------------------------------
    public const uint Loc1 = 1;
    public const uint Loc2 = 2;
    public const uint Loc3 = 3;
    public const uint Loc4 = 4;
    public const uint AssemblyLoc = 5;
    public const uint OutputLoc = 6;

    // -------------------------------------------------------
    // Calibrated XY positions (meters in base frame)
    // Juster disse når I kender jeres URSim/robot layout.
    // -------------------------------------------------------
    public static readonly (double x, double y) P1 = (0.25, -0.30); // loc 1
    public static readonly (double x, double y) P2 = (0.25, -0.10); // loc 2
    public static readonly (double x, double y) P3 = (0.35, -0.20); // loc 3
    public static readonly (double x, double y) P4 = (0.35,  0.20); // loc 4
    public static readonly (double x, double y) Assembly = (0.45, 0.00);
    public static readonly (double x, double y) Output   = (0.55, 0.00);

    // -------------------------------------------------------
    // Z / orientation defaults
    // -------------------------------------------------------
    private const double ZApproach = 0.20; // sikker rejsehøjde
    private const double ZPickBase = 0.02; // pick/drop baseline (tune!)
    private const double StackPitch = 0.02; // hvor meget “next item” ændrer z

    // Tool orientation (typisk ned mod bordet)
    private const double RX = 0.0;
    private const double RY = 3.1415;
    private const double RZ = 0.0;

    // Motion parameters (tune)
    private const double AJ = 1.2, VJ = 0.6;
    private const double AL = 0.4, VL = 0.2;

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    public static (double x, double y) FromLocation(uint loc) => loc switch
    {
        Loc1 => P1,
        Loc2 => P2,
        Loc3 => P3,
        Loc4 => P4,
        AssemblyLoc => Assembly,
        OutputLoc => Output,
        _ => Assembly
    };

    private static double ZForStack(int index) => ZPickBase + (index * StackPitch);

    // IMPORTANT:
    // Robot.SendProgram() wrapper bruger en-US culture, men vi sikrer det her også.
    private static string F(double v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

    // -------------------------------------------------------
    // Ready-made test program (vinker)
    // Returnerer BODY (uden def/end)
    // -------------------------------------------------------
    public static string Wave() => @"
  textmsg(""PING/WAVE"")
  home = [0, -1.57, 0, -1.57, 0, 0]
  left =  p[0.25, -0.25, 0.20, 0, -3.1415, 0]
  right = p[0.25,  0.25, 0.20, 0, -3.1415, 0]
  movej(home, a=1.2, v=0.6)
  i = 0
  while (i < 3):
    movej(get_inverse_kin(left),  a=1.2, v=0.6)
    movej(get_inverse_kin(right), a=1.2, v=0.6)
    i = i + 1
  end
";

    // -------------------------------------------------------
    // Core: pick & place generator
    // Returnerer BODY (uden def/end)
    // -------------------------------------------------------
    public static string PickAndPlace(
        (double x, double y) from,
        (double x, double y) to,
        double zPick,
        double zPlace,
        bool useGripperIo = false,
        int gripperDo = 0)
    {
        // IO er optional: hvis I ikke har gripper/vacuum i URSim endnu,
        // så kan I lade useGripperIo = false.
        var gripOn = useGripperIo
            ? $"\n  set_digital_out({gripperDo}, True)\n  sleep(0.2)\n"
            : "\n  # TODO: grip ON (set_digital_out / vacuum)\n  # sleep(0.2)\n";

        var gripOff = useGripperIo
            ? $"\n  set_digital_out({gripperDo}, False)\n  sleep(0.2)\n"
            : "\n  # TODO: grip OFF\n  # sleep(0.2)\n";

        return $@"
  textmsg(""PICK & PLACE"")

  home = [0, -1.57, 0, -1.57, 0, 0]
  movej(home, a={F(AJ)}, v={F(VJ)})

  p_from_app = p[{F(from.x)}, {F(from.y)}, {F(ZApproach)}, {F(RX)}, {F(RY)}, {F(RZ)}]
  p_from     = p[{F(from.x)}, {F(from.y)}, {F(zPick)},     {F(RX)}, {F(RY)}, {F(RZ)}]

  p_to_app   = p[{F(to.x)},   {F(to.y)},   {F(ZApproach)}, {F(RX)}, {F(RY)}, {F(RZ)}]
  p_to       = p[{F(to.x)},   {F(to.y)},   {F(zPlace)},    {F(RX)}, {F(RY)}, {F(RZ)}]

  # --- above FROM ---
  movej(get_inverse_kin(p_from_app), a={F(AJ)}, v={F(VJ)})
  movel(p_from, a={F(AL)}, v={F(VL)})
{gripOn}
  movel(p_from_app, a={F(AL)}, v={F(VL)})

  # --- above TO ---
  movej(get_inverse_kin(p_to_app), a={F(AJ)}, v={F(VJ)})
  movel(p_to, a={F(AL)}, v={F(VL)})
{gripOff}
  movel(p_to_app, a={F(AL)}, v={F(VL)})
";
    }

    // -------------------------------------------------------
    // High-level: location -> location
    // fromStackIndex/toStackIndex gør det nemt at tage fra stak
    // -------------------------------------------------------
    public static string MoveLocationToLocation(
        uint fromLocation,
        uint toLocation,
        int fromStackIndex = 0,
        int toStackIndex = 0,
        bool useGripperIo = false,
        int gripperDo = 0)
    {
        var from = FromLocation(fromLocation);
        var to = FromLocation(toLocation);

        var zPick = ZForStack(fromStackIndex);
        var zPlace = ZForStack(toStackIndex);

        return PickAndPlace(from, to, zPick, zPlace, useGripperIo, gripperDo);
    }

    // Convenience helpers
    public static string PickToAssembly(uint fromLocation, int fromStackIndex = 0)
        => MoveLocationToLocation(fromLocation, AssemblyLoc, fromStackIndex);

    public static string AssemblyToOutput()
        => MoveLocationToLocation(AssemblyLoc, OutputLoc);
}
