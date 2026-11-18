using System;
using System.Diagnostics;
using MPI;

class ArraySum
{
    const int N = 10_000_003; 

    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Intracommunicator comm = Communicator.world;
            int rank = comm.Rank, size = comm.Size;
            
            int chunk = (N + size - 1) / size; 
            long padded = (long)chunk * size; 
            
            int[][] chunks = null; 
            long refSum = 0;
            int refMax = int.MinValue;

            if (rank == 0)
            {
                int[] data = new int[padded];
                for (int i = 0; i < N; i++) data[i] = (int)(((long)i * 7919) % 100003);
                
                for (int i = 0; i < N; i++)
                {
                    refSum += data[i];
                    if (data[i] > refMax) refMax = data[i];
                }
                
                chunks = new int[size][];
                for (int r = 0; r < size; r++)
                {
                    chunks[r] = new int[chunk];
                    Array.Copy(data, (long)r * chunk, chunks[r], 0, chunk);
                }
            }

            comm.Barrier();
            Stopwatch sw = Stopwatch.StartNew();

            int[] mine = (rank == 0) ? comm.Scatter(chunks, 0) : comm.Scatter<int[]>(0);

            long localSum = 0;
            int localMax = int.MinValue;
            for (int i = 0; i < mine.Length; i++)
            {
                localSum += mine[i];
                if (mine[i] > localMax) localMax = mine[i];
            }

            long total = comm.Reduce(localSum, Operation<long>.Add, 0);
            int gmax = comm.Reduce(localMax, Operation<int>.Max, 0);

            comm.Barrier();
            double ms = sw.Elapsed.TotalMilliseconds;

            if (rank == 0)
            {
                Console.WriteLine($"[array-sum] ranks={size} n={N} chunk={chunk} padding={padded - N}");
                bool ok = total == refSum && gmax == refMax;
                Console.WriteLine($"sum={total} reference={refSum} max={gmax} reference_max={refMax} correct={(ok ? "YES" : "NO")} time={ms:F2} ms");
            }
        }
    }
}