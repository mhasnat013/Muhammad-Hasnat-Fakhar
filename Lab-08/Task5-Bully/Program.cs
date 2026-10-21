using System;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        int crashedRank = 4; 
        int initiatorRank = 1; 
        
        if (args.Length >= 2) {
            crashedRank = int.Parse(args[0]);
            initiatorRank = int.Parse(args[1]);
        }

        using (new MPI.Environment(ref args))
        {
            Intracommunicator world = Communicator.world;
            int rank = world.Rank;
            int size = world.Size;

            // 1. Simulate Crash
            if (rank == crashedRank) {
                Console.WriteLine($"[Rank {rank}] CRASHED (No response).");
                // Yahan fix kiya hai:
                System.Environment.Exit(0); 
                return; 
            }

            int highestSurvivor = size - 1;
            if (highestSurvivor == crashedRank) highestSurvivor--;

            // 2. Initiator
            if (rank == initiatorRank) {
                Console.WriteLine($"[Rank {rank}] Initiates ELECTION.");
                for (int i = rank + 1; i < size; i++) {
                    if (i != crashedRank) world.Send("ELECTION", i, 1);
                }
                string coord = world.Receive<string>(highestSurvivor, 3);
                Console.WriteLine($"[Rank {rank}] Agrees: {coord}");
            }
            // 3. Higher ranks
            else if (rank > initiatorRank && rank <= highestSurvivor) {
                Status status = world.Probe(Communicator.anySource, 1);
                world.Receive<string>(status.Source, 1);
                world.Send("OK", status.Source, 2);
                Console.WriteLine($"[Rank {rank}] Got ELECTION, sent OK.");

                if (rank == highestSurvivor) {
                    Console.WriteLine($"[Rank {rank}] Highest survivor. Sending COORDINATOR.");
                    for (int i = 0; i < size; i++) {
                        if (i != crashedRank) world.Send($"Rank {rank} is leader", i, 3);
                    }
                } else {
                    string coord = world.Receive<string>(Communicator.anySource, 3);
                    Console.WriteLine($"[Rank {rank}] Agrees: {coord}");
                }
            }
            // 4. Lower ranks
            else if (rank < initiatorRank) {
                string coord = world.Receive<string>(highestSurvivor, 3);
                Console.WriteLine($"[Rank {rank}] Agrees: {coord}");
            }
        }
    }
}