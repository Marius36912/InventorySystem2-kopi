using System.Globalization;

namespace InventorySystem2.Models;

public static class RobotPositions
{
    // -------------------------------------------------------
    // Location IDs (match ItemEntity.InventoryLocation in DB)
    // -------------------------------------------------------
    public const uint Loc1 = 1; // White Shell
    public const uint Loc2 = 2; // Black Shell
    public const uint Loc3 = 3;
    public const uint Loc4 = 4;
    public const uint AssemblyLoc = 5;
    public const uint OutputLoc = 6;

    // -------------------------------------------------------
    // Helpers (format)
    // -------------------------------------------------------
    private static string F(double v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

    // -------------------------------------------------------
    // Pose data (fra Itemsorterrobot.cs)
    // -------------------------------------------------------
    // Start
    private static readonly Pose P_START = new(0.12500, -0.30000, 0.10000, 3.14, -0.00, -0.00);

    // A (White Shell)
    private static readonly Pose P_A_10 = new(0.42500, 0.025, -0.025, 2.225, 2.225, -0.00);
    private static readonly Pose P_A_0  = new(0.42500, 0.025, -0.125, 2.225, 2.225, -0.00);

    // B (Black Shell)
    private static readonly Pose P_B_10 = new(0.42500, -0.125, -0.025, 2.225, 2.225, -0.00);
    private static readonly Pose P_B_0  = new(0.42500, -0.125, -0.125, 2.225, 2.225, -0.00);

    // C
    private static readonly Pose P_C_10 = new(0.22500, -0.275, -0.025, 2.225, 2.225, -0.00);
    private static readonly Pose P_C_0  = new(0.22500, -0.275, -0.125, 2.225, 2.225, -0.00);
    private static readonly Pose P_C_2  = new(0.22500, -0.275, -0.115, 2.225, 2.225, -0.00);

    // D
    private static readonly Pose P_D_10 = new(0.42500, -0.275, -0.025, 2.225, 2.225, -0.00);
    private static readonly Pose P_D_0  = new(0.42500, -0.275, -0.125, 2.225, 2.225, -0.00);
    
    // E
    private static readonly Pose P_E_10 = new(-0.0250, -0.4750, -0.025, 2.225, 2.225, -0.00);
    private static readonly Pose P_E_0  = new(-0.0250, -0.4750, -0.125, 2.225, 2.225, -0.00);

    // -------------------------------------------------------
    // Public: Test program (ping)
    // Returnerer BODY (Robot.SendProgram wrapper def/end selv)
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
    // Gripper functions: SIM vs REAL
    // -------------------------------------------------------
    public static string GripperFunctions(bool sim)
        => sim ? GripperFunctions_Sim() : GripperFunctions_Real();

    private static string GripperFunctions_Sim() => @"
  # --- Gripper stub (URSim) ---
  def rg_grip(width, force = 10):
    sleep(0.05)
  end
";

    private static string GripperFunctions_Real() => @"
  # --- RG2 gripper via XML-RPC (OnRobot) ---
  global RPC = rpc_factory(""xmlrpc"", ""http://localhost:41414"")
  global TOOL_INDEX = 0

  def rg_is_busy():
    return RPC.rg_get_busy(TOOL_INDEX)
  end

  # width: [0..110], force: [0..40]
  def rg_grip(width, force = 10):
    RPC.rg_grip(TOOL_INDEX, width + .0, force + .0)
    sleep(0.01)
    while (rg_is_busy()):
      # wait
    end
  end
";

    // -------------------------------------------------------
    // ItemSorter-sekvenser (SLAVISK)
    // White Shell  => A
    // Black Shell  => B
    //
    // Skal køre:
    // Start → A10/B10 → A0/B0 → A10/B10 → C10 → C0 → C10 → D10 → D0 → D10 → C10 → C2 → Start
    // -------------------------------------------------------
    public static string ItemSorter_WhiteShell(bool sim) => ItemSorterSequenceLoop(sim, useB: false, repeats: 1);
    public static string ItemSorter_BlackShell(bool sim) => ItemSorterSequenceLoop(sim, useB: true, repeats: 1);

    public static string ItemSorter_WhiteShell(bool sim, int repeats) => ItemSorterSequenceLoop(sim, useB: false, repeats: repeats);
    public static string ItemSorter_BlackShell(bool sim, int repeats) => ItemSorterSequenceLoop(sim, useB: true, repeats: repeats);
    public static string ItemSorter_Mix(bool sim, int repeats) => ItemSorterMixLoop(sim, repeats);

    private static string ItemSorterSequenceLoop(bool sim, bool useB, int repeats)
{
    if (repeats < 1) repeats = 1;

    var pPick10 = useB ? P_B_10 : P_A_10;
    var pPick0  = useB ? P_B_0  : P_A_0;
    var label   = useB ? "BLACK SHELL (B)" : "WHITE SHELL (A)";

    return $@"
  textmsg(""ItemSorter LOOP: {label} x {repeats}"")

{GripperFunctions(sim)}

  # Gripper parametre
  open_mm       = 60
  open_pick_mm  = 85
  close_mm      = 31
  f_open        = 30
  f_close       = 30

  # --- Poses ---
  p_start  = {P_START.ToUrScript()}

  p_pick_10 = {pPick10.ToUrScript()}
  p_pick_0  = {pPick0.ToUrScript()}

  p_C_10 = {P_C_10.ToUrScript()}
  p_C_0  = {P_C_0.ToUrScript()}
  p_C_2  = {P_C_2.ToUrScript()}

  p_D_10 = {P_D_10.ToUrScript()}
  p_D_0  = {P_D_0.ToUrScript()}

  p_E_10 = {P_E_10.ToUrScript()}
  p_E_0  = {P_E_0.ToUrScript()}

  # Start-position og sikker gripper
  rg_grip(close_mm, f_close)
  movel(p_start, a=0.3, v=0.15)

  i = 0
  while (i < {repeats}):

    textmsg(""Run #"" + to_str(i+1) + "" / {repeats}"")

    # A10/B10
    movel(p_pick_10, a=0.4, v=0.2)
    # A0/B0
    movel(p_pick_0,  a=0.2, v=0.1)

    rg_grip(open_pick_mm, f_open)  # ÅBN 85 mm

    # A10/B10
    movel(p_pick_10, a=0.2, v=0.1)

    # C10 -> C0 -> C10
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_C_0,  a=0.2, v=0.1)
    rg_grip(open_mm, f_open)       # SLIP ved C0
    movel(p_C_10, a=0.2, v=0.1)

    # D10 -> D0 -> D10
    movel(p_D_10, a=0.4, v=0.2)
    movel(p_D_0,  a=0.2, v=0.1)
    rg_grip(close_mm, f_close)     # GRIP ved D0
    movel(p_D_10, a=0.2, v=0.1)

    # C10 -> C2
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_C_2,  a=0.2, v=0.1)
    rg_grip(open_pick_mm, f_open)  # ÅBN 85 mm ved C2

    # C10 -> E10 -> E0 (ingen D10 her)
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_E_10, a=0.4, v=0.2)
    movel(p_E_0,  a=0.2, v=0.1)

    rg_grip(open_mm, f_open)       # SLIP ved E0 (60 mm)

    # E10 -> Start
    movel(p_E_10, a=0.4, v=0.2)
    movel(p_start, a=0.4, v=0.2)

    i = i + 1
  end

  textmsg(""DONE LOOP: {label}"")
";
}
private static string ItemSorterMixLoop(bool sim, int repeats)
{
    if (repeats < 1) repeats = 1;

    // Mix = (A + B) pr. repeat
    // Sikker version: returnerer til start mellem runs.
    return $@"
  textmsg(""ItemSorter LOOP: MIX (A+B) x {repeats}"")

{GripperFunctions(sim)}

  # Gripper parametre
  open_mm       = 60
  open_pick_mm  = 85
  close_mm      = 31
  f_open        = 30
  f_close       = 30

  # --- Poses ---
  p_start  = {P_START.ToUrScript()}

  p_A_10 = {P_A_10.ToUrScript()}
  p_A_0  = {P_A_0.ToUrScript()}

  p_B_10 = {P_B_10.ToUrScript()}
  p_B_0  = {P_B_0.ToUrScript()}

  p_C_10 = {P_C_10.ToUrScript()}
  p_C_0  = {P_C_0.ToUrScript()}
  p_C_2  = {P_C_2.ToUrScript()}

  p_D_10 = {P_D_10.ToUrScript()}
  p_D_0  = {P_D_0.ToUrScript()}

  p_E_10 = {P_E_10.ToUrScript()}
  p_E_0  = {P_E_0.ToUrScript()}

  # Start-position og sikker gripper
  rg_grip(close_mm, f_close)
  movel(p_start, a=0.3, v=0.15)

  # Fælles run-sekvens som URScript-funktion
  def do_run(p_pick_10, p_pick_0):

    movel(p_pick_10, a=0.4, v=0.2)
    movel(p_pick_0,  a=0.2, v=0.1)

    rg_grip(open_pick_mm, f_open)  # ÅBN 85 mm

    movel(p_pick_10, a=0.2, v=0.1)

    # C10 -> C0 -> C10
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_C_0,  a=0.2, v=0.1)
    rg_grip(open_mm, f_open)       # SLIP ved C0
    movel(p_C_10, a=0.2, v=0.1)

    # D10 -> D0 -> D10
    movel(p_D_10, a=0.4, v=0.2)
    movel(p_D_0,  a=0.2, v=0.1)
    rg_grip(close_mm, f_close)     # GRIP ved D0
    movel(p_D_10, a=0.2, v=0.1)

    # C10 -> C2
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_C_2,  a=0.2, v=0.1)
    rg_grip(open_pick_mm, f_open)  # ÅBN 85 mm ved C2

    # C10 -> E10 -> E0 (ingen D10 her)
    movel(p_C_10, a=0.4, v=0.2)
    movel(p_E_10, a=0.4, v=0.2)
    movel(p_E_0,  a=0.2, v=0.1)

    rg_grip(open_mm, f_open)       # SLIP ved E0 (60 mm)

    # E10 -> Start
    movel(p_E_10, a=0.4, v=0.2)
    movel(p_start, a=0.4, v=0.2)

  end

  i = 0
  while (i < {repeats}):

    textmsg(""MIX repeat "" + to_str(i+1) + "" / {repeats}"")

    # A (White)
    do_run(p_A_10, p_A_0)

    # B (Black)
    do_run(p_B_10, p_B_0)

    i = i + 1
  end

  textmsg(""DONE LOOP: MIX"")
";
}

public static string AssemblyToOutput(bool sim)
{
    return $@"
  textmsg(""Assembly -> Output"")

{GripperFunctions(sim)}

  open_mm  = 60
  close_mm = 24
  f_open   = 30
  f_close  = 30

  # Proxy: Assembly = C, Output = D
  p_asm_10 = {P_C_10.ToUrScript()}
  p_asm_0  = {P_C_0.ToUrScript()}
  p_out_10 = {P_D_10.ToUrScript()}
  p_out_0  = {P_D_0.ToUrScript()}

  rg_grip(open_mm, f_open)

  # Pick at Assembly (C)
  movel(p_asm_10, a=0.4, v=0.2)
  movel(p_asm_0,  a=0.2, v=0.1)
  rg_grip(close_mm, f_close)
  movel(p_asm_10, a=0.2, v=0.1)

  # Place at Output (D)
  movel(p_out_10, a=0.4, v=0.2)
  movel(p_out_0,  a=0.2, v=0.1)
  rg_grip(open_mm, f_open)
  movel(p_out_10, a=0.2, v=0.1)

  textmsg(""DONE: Assembly -> Output"")
";
}

// -------------------------------------------------------
// Small record for readability
// -------------------------------------------------------
private readonly record struct Pose(double X, double Y, double Z, double RX, double RY, double RZ)
{
    public string ToUrScript()
        => $"p[{F(X)}, {F(Y)}, {F(Z)}, {F(RX)}, {F(RY)}, {F(RZ)}]";
}
}
