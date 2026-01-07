using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace InventorySystem2.Models;

public sealed class Robot
{
    private readonly string _host;
    private readonly int _urscriptPort;
    private readonly int _dashboardPort;

    public Robot(string host = "localhost", int urscriptPort = 30002, int dashboardPort = 29999)
    {
        _host = host;
        _urscriptPort = urscriptPort;
        _dashboardPort = dashboardPort;

        // Sørg for at URScript bruger punktum som decimal
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
    }

    private static void SendString(string host, int port, string message)
    {
        using var client = new TcpClient(host, port);
        using var stream = client.GetStream();
        var bytes = Encoding.ASCII.GetBytes(message);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Sender et URScript program til secondary interface (default 30002).
    /// 'program' skal være BODY (uden def/end), da vi wrapper det her.
    /// </summary>
    public void SendProgram(string program, uint itemId = 0)
    {
        try
        {
            // Wrap i def/end, så UR forstår det som et program
            // Program-body må gerne indeholde indentation og flere linjer
            var fullProgram =
                $"def pick_item_{itemId}():\n" +
                $"{program}\n" +
                "end\n";

            SendString(_host, _urscriptPort, fullProgram);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Robotfejl (URScript): {ex.Message}");
        }
    }

    /// <summary>
    /// Sender en dashboard-kommando (default 29999). Husk newline.
    /// Eksempler: "brake release", "play", "stop", "power on".
    /// </summary>
    public void SendDashboard(string command)
    {
        try
        {
            if (!command.EndsWith("\n"))
                command += "\n";

            SendString(_host, _dashboardPort, command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Robotfejl (Dashboard): {ex.Message}");
        }
    }

    // Convenience helpers
    public void ReleaseBrakes() => SendDashboard("brake release");
    public void StopProgram() => SendDashboard("stop");
    public void PlayProgram() => SendDashboard("play");
    public void PowerOn() => SendDashboard("power on");
}
