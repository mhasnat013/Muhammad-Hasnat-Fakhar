using System;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Intracommunicator world = Communicator.world;
            int rank = world.Rank;
            int other = (rank == 0) ? 1 : 0;
            
            // 8MB payload (2 million ints)
            int[] buffer = new int[2 * 1024 * 1024]; 
            int[] recvBuffer = new int[2 * 1024 * 1024]; // Buffer to receive raw data

            // 3A: Reordered Blocking (Raw Array Overload - No Serializer)
            Console.WriteLine($"[Rank {rank}] Starting 3A (Reordered)...");
            if (rank == 0) {
                world.Send(buffer, other, 0);
                world.Receive(other, 0, ref recvBuffer); // Raw receive
            } else {
                world.Receive(other, 0, ref recvBuffer); // Raw receive
                world.Send(buffer, other, 0);
            }
            Console.WriteLine($"[Rank {rank}] 3A Done.");

            // 3B: Non-Blocking (Raw Array Overload)
            Console.WriteLine($"[Rank {rank}] Starting 3B (Non-Blocking)...");
            var recvReq = world.ImmediateReceive<int>(other, 1, recvBuffer);
            var sendReq = world.ImmediateSend(buffer, other, 1);
            
            recvReq.Wait(); 
            sendReq.Wait(); 
            Console.WriteLine($"[Rank {rank}] 3B Done.");
        }
    }
}