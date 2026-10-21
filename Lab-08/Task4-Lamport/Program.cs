using System;
using System.Collections.Generic;
using System.Linq;
using MPI;

// Custom class to store our log events
[Serializable]
public class LamportEvent
{
    public int Clock { get; set; }
    public int Rank { get; set; }
    public string EventType { get; set; } // "Local", "Send", "Receive"
    public string Details { get; set; }
}

class Program
{
    static int clock = 0; // Each process has its own Lamport clock
    static List<LamportEvent> localLog = new List<LamportEvent>();

    static void LogEvent(string type, string details)
    {
        localLog.Add(new LamportEvent
        {
            Clock = clock,
            Rank = Communicator.world.Rank,
            EventType = type,
            Details = details
        });
    }

    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Intracommunicator world = Communicator.world;
            int rank = world.Rank;
            int size = world.Size;

            // 1. Local Event
            clock++; 
            LogEvent("Local", "Starting work");

            // 2. Ring Communication (0 sends to 1, 1 sends to 2, etc.)
            if (rank == 0)
            {
                // Send event
                clock++;
                LogEvent("Send", $"Sent message to Rank 1");
                world.Send(clock, 1, 0);
            }
            else if (rank < size - 1)
            {
                // Receive event (Rule: clock = max(local, received) + 1)
                int receivedClock = world.Receive<int>(rank - 1, 0);
                clock = Math.Max(clock, receivedClock) + 1;
                LogEvent("Receive", $"Received from Rank {rank - 1}");

                // Send event to next rank
                clock++;
                LogEvent("Send", $"Sent message to Rank {rank + 1}");
                world.Send(clock, rank + 1, 0);
            }
            else
            {
                // Last rank only receives
                int receivedClock = world.Receive<int>(rank - 1, 0);
                clock = Math.Max(clock, receivedClock) + 1;
                LogEvent("Receive", $"Received from Rank {rank - 1}");
            }

            // 3. Another Local Event
            clock++;
            LogEvent("Local", "Finished work");

            // --- MERGE LOGS at Rank 0 ---
            if (rank != 0)
            {
                world.Send(localLog.ToArray(), 0, 99);
            }
            else
            {
                List<LamportEvent> mergedLog = new List<LamportEvent>(localLog);
                for (int i = 1; i < size; i++)
                {
                    LamportEvent[] rLog = world.Receive<LamportEvent[]>(i, 99);
                    mergedLog.AddRange(rLog);
                }

                // Sort by Clock, then by Rank (to break ties)
                mergedLog.Sort((a, b) => {
                    if (a.Clock != b.Clock) return a.Clock.CompareTo(b.Clock);
                    return a.Rank.CompareTo(b.Rank);
                });

                // Print merged log
                Console.WriteLine("\n--- Merged Lamport Log ---");
                foreach (var e in mergedLog)
                {
                    Console.WriteLine($"Clock: {e.Clock:D2} | Rank: {e.Rank} | {e.EventType,-7} | {e.Details}");
                }

                // Check for violations (Send clock must be < Receive clock)
                int violations = 0;
                for (int i = 0; i < mergedLog.Count; i++)
                {
                    if (mergedLog[i].EventType == "Receive")
                    {
                        // Just a simple check for our specific ring pattern
                        int sendClock = mergedLog[i - 1].Clock; 
                        if (sendClock >= mergedLog[i].Clock) violations++;
                    }
                }
                Console.WriteLine($"\nCausal violations: {violations}");
            }
        }
    }
}