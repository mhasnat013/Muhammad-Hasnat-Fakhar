using System;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Communicator world = Communicator.world;
            int rank = world.Rank;
            int size = world.Size;
            
            // Yahan fix kiya gaya hai: System.Environment likha hai
            string hostname = System.Environment.MachineName; 

            // 1. Unordered output (Direct print)
            Console.WriteLine($"[Unordered] Rank {rank} of {size} on {hostname}");

            // 2. Ordered output through Message Passing
            if (rank != 0)
            {
                string msg = $"Rank {rank} of {size} on {hostname}";
                world.Send(msg, 0, 99);
            }
            else
            {
                Console.WriteLine("\n--- Ordered Output ---");
                Console.WriteLine($"Rank 0 of {size} on {hostname}");
                
                for (int i = 1; i < size; i++)
                {
                    string msg = world.Receive<string>(i, 99);
                    Console.WriteLine(msg);
                }
            }
        }
    }
}