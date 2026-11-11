using System;
using System.Diagnostics;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Intracommunicator world = Communicator.world;
            int rank = world.Rank;
            int[] sizes = { 0, 1, 8, 64, 256, 1024, 4096, 16384, 65536, 262144 };
            int iterations = 100;

            if (rank == 0) Console.WriteLine("Size (Bytes) | Mean RTT (us)");

            foreach (int size in sizes)
            {
                // Min 1 byte allocate karenge taake 0-byte par crash na ho
                int actualSize = Math.Max(1, size);
                byte[] buffer = new byte[actualSize]; 
                byte[] recvBuffer = new byte[actualSize];
                
                world.Barrier(); // Sync before timing

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    if (rank == 0)
                    {
                        world.Send<byte>(buffer, 1, 0);
                        world.Receive<byte>(1, 0, ref recvBuffer);
                    }
                    else if (rank == 1)
                    {
                        world.Receive<byte>(0, 0, ref recvBuffer);
                        world.Send<byte>(buffer, 0, 0);
                    }
                }
                sw.Stop();

                if (rank == 0)
                {
                    double rttMicrosec = (sw.Elapsed.TotalMilliseconds * 1000) / iterations;
                    Console.WriteLine($"{size,-12} | {rttMicrosec:F2}");
                }
            }
        }
    }
}