using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MPI;

class Program
{
    static void Main(string[] args)
    {
        const int N = 10_000_000; // 1 crore elements
        double[] data = new double[N];
        for (int i = 0; i < N; i++) data[i] = 1.0;

        using (new MPI.Environment(ref args))
        {
            // YAHAN FIX KIYA HAI: Communicator ki jagah Intracommunicator likha hai
            Intracommunicator world = Communicator.world;
            
            int rank = world.Rank;
            int size = world.Size;
            int chunkSize = N / size;
            int start = rank * chunkSize;
            int end = start + chunkSize;
            double localSum = 0;

            // --- 1. MPI Collective (Reduce) ---
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = start; i < end; i++) localSum += data[i];
            
            // Ab yeh error nahi dega kyunki Intracommunicator mein Reduce hota hai
            double totalSum = world.Reduce(localSum, Operation<double>.Add, 0);
            
            sw.Stop();
            if (rank == 0) Console.WriteLine($"MPI Collective: {sw.Elapsed.TotalMilliseconds:F2} ms (Sum: {totalSum})");

            // --- 2. MPI Point-to-Point ---
            sw.Restart();
            localSum = 0;
            for (int i = start; i < end; i++) localSum += data[i];

            if (rank == 0)
            {
                double p2pTotal = localSum;
                for (int i = 1; i < size; i++)
                    p2pTotal += world.Receive<double>(i, 0);
                sw.Stop();
                Console.WriteLine($"MPI P2P:        {sw.Elapsed.TotalMilliseconds:F2} ms (Sum: {p2pTotal})");
            }
            else
            {
                world.Send(localSum, 0, 0);
            }

            // --- 3. Shared Memory (TPL) - Run only on Rank 0 ---
            if (rank == 0)
            {
                sw.Restart();
                double tplSum = 0;
                object gate = new object();
                
                Parallel.For(0, size, new ParallelOptions { MaxDegreeOfParallelism = size }, r =>
                {
                    double local = 0;
                    int s = r * chunkSize;
                    int e = s + chunkSize;
                    for (int i = s; i < e; i++) local += data[i];
                    lock (gate) { tplSum += local; }
                });
                sw.Stop();
                Console.WriteLine($"Shared Mem(TPL):{sw.Elapsed.TotalMilliseconds:F2} ms (Sum: {tplSum})\n");
            }
        }
    }
}