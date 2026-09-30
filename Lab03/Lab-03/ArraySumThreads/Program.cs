using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
    const int Iterations = 50;

    static void Main()
    {
        // .NET 9 output folder — matches the SDK on this machine
        string childExe = @"C:\Users\ausoft\Documents\ProcessLab\bin\Release\net9.0\ProcessLab.exe";

        if (!File.Exists(childExe))
        {
            Console.WriteLine($"ERROR: Cannot find child exe at {childExe}");
            return;
        }

        // ============================================================
        // 1) Measure PROCESS creation overhead
        // ============================================================
        var processStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            var psi = new ProcessStartInfo
            {
                FileName = childExe,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--child");

            using Process p = Process.Start(psi)!;
            p.WaitForExit();
        }
        processStopwatch.Stop();

        // ============================================================
        // 2) Measure THREAD creation overhead
        // ============================================================
        var threadStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++)
        {
            Thread t = new Thread(() => { /* trivial work — empty body */ });
            t.Start();
            t.Join();
        }
        threadStopwatch.Stop();

        // ============================================================
        // 3) Report averages and ratio
        // ============================================================
        double avgProcessMs = processStopwatch.Elapsed.TotalMilliseconds / Iterations;
        double avgThreadMs  = threadStopwatch.Elapsed.TotalMilliseconds  / Iterations;

        Console.WriteLine($"Iterations                    : {Iterations}");
        Console.WriteLine($"Average process creation time : {avgProcessMs:F3} ms");
        Console.WriteLine($"Average thread  creation time : {avgThreadMs:F3} ms");
        Console.WriteLine($"Process creation was {(avgProcessMs / avgThreadMs):F1}x more expensive than thread creation.");
    }
}