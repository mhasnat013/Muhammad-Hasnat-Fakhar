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
            
            // 8MB Byte Array
            byte[] buffer = new byte[8 * 1024 * 1024]; 
            byte[] recvBuffer = new byte[8 * 1024 * 1024];

            // 3A: Reordered Blocking
            Console.WriteLine($"[Rank {rank}] Starting 3A (Reordered)...");
            if (rank == 0) {
                world.Send<byte>(buffer, other, 0); 
                world.Receive<byte>(other, 0, ref recvBuffer); 
            } else {
                world.Receive<byte>(other, 0, ref recvBuffer); 
                world.Send<byte>(buffer, other, 0); 
            }
            Console.WriteLine($"[Rank {rank}] 3A Done.");

            // 3B: Non-Blocking
            Console.WriteLine($"[Rank {rank}] Starting 3B (Non-Blocking)...");
            var recvReq = world.ImmediateReceive<byte>(other, 1, recvBuffer); 
            var sendReq = world.ImmediateSend<byte>(buffer, other, 1); 
            
            recvReq.Wait(); 
            sendReq.Wait(); 
            Console.WriteLine($"[Rank {rank}] 3B Done.");
        }
    }
}