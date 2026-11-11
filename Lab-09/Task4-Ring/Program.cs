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
            int size = world.Size;

            int next = (rank + 1) % size;
            int prev = (rank - 1 + size) % size;

            // 1. Sab ranks pehle se Receive ka wait (Non-Blocking) kar rahe hain
            var recvReq = world.ImmediateReceive<int>(prev, 0);
            
            // 2. Sirf Rank 0 token shuru karega
            if (rank == 0)
            {
                world.Send(999, next, 0);
            }

            // 3. Har rank apna token receive karega
            recvReq.Wait();
            int receivedToken = (int)recvReq.GetValue();
            
            Console.WriteLine($"[Rank {rank}] Prev: {prev} | Next: {next} | Received Token: {receivedToken}");

            // 4. Token ko aage forward karein (Rank 0 ke ilawa, kyunki usne wapas receive kar liya hai)
            if (rank != 0)
            {
                world.Send(receivedToken, next, 0);
            }
            else
            {
                Console.WriteLine("\n[SUCCESS] Ring completed! Token returned to Rank 0.");
            }
        }
    }
}