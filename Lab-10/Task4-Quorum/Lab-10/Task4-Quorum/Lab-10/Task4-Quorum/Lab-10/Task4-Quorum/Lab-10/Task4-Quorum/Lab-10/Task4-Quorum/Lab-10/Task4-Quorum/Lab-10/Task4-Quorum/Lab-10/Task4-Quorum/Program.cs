using System;
using MPI;

class QuorumDemo
{
    const int Keys = 3;
    const int OpStop = 0, OpWrite = 1, OpRead = 2;

    static int[] PickSubset(Random rnd, int n, int k)
    {
        int[] idx = new int[n];
        for (int i = 0; i < n; i++) idx[i] = i + 1;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            int t = idx[i]; idx[i] = idx[j]; idx[j] = t;
        }
        int[] set = new int[k];
        Array.Copy(idx, set, k);
        return set;
    }

    static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            Intracommunicator comm = Communicator.world;
            int rank = comm.Rank, size = comm.Size;
            int n = size - 1; 
            
            if (n < 2)
            {
                if (rank == 0) Console.WriteLine("Run with at least 3 ranks");
                return;
            }

            int w = args.Length > 0 ? int.Parse(args[0]) : n / 2 + 1; 
            int r = args.Length > 1 ? int.Parse(args[1]) : n / 2 + 1; 
            int trials = args.Length > 2 ? int.Parse(args[2]) : 300;

            if (w < 1 || w > n || r < 1 || r > n)
            {
                if (rank == 0) Console.WriteLine($"W and R must be between 1 and N={n}");
                return;
            }

            if (rank != 0) 
            {
                int[] value = new int[Keys], version = new int[Keys];
                while (true)
                {
                    // FIX: Allocate a 4-element buffer and use ref
                    int[] req = new int[4];
                    comm.Receive(0, 0, ref req); 
                    
                    if (req[0] == OpStop) break;
                    
                    if (req[0] == OpWrite && req[3] > version[req[1]])
                    {
                        value[req[1]] = req[2]; 
                        version[req[1]] = req[3];
                    }
                    
                    int[] reply = new int[] { value[req[1]], version[req[1]] };
                    comm.Send(reply, 0, 1); 
                }
            }
            else 
            {
                Random rnd = new Random(12345);
                int[] latestVer = new int[Keys];
                int counter = 0, stale = 0, fresh = 0;

                for (int t = 0; t < trials; t++)
                {
                    int key = t % Keys, ver = ++counter, val = 1000 + t;
                    int[] set = PickSubset(rnd, n, w); 
                    
                    // --- WRITE ---
                    foreach (int replica in set)
                        comm.Send(new int[] { OpWrite, key, val, ver }, replica, 0);
                    
                    foreach (int replica in set)
                    {
                        int[] ack = new int[2];
                        comm.Receive(replica, 1, ref ack); // FIX: Use ref array
                    }
                    
                    latestVer[key] = ver;
                    
                    // --- READ ---
                    set = PickSubset(rnd, n, r); 
                    int gotVer = -1;
                    
                    foreach (int replica in set)
                        comm.Send(new int[] { OpRead, key, 0, 0 }, replica, 0);
                    
                    foreach (int replica in set)
                    {
                        int[] reply = new int[2];
                        comm.Receive(replica, 1, ref reply); // FIX: Use ref array
                        if (reply[1] > gotVer) gotVer = reply[1]; 
                    }

                    if (gotVer == latestVer[key]) fresh++; else stale++;
                }

                for (int i = 1; i <= n; i++)
                    comm.Send(new int[] { OpStop, 0, 0, 0 }, i, 0);

                string rel = w + r > n ? ">" : "<=";
                Console.WriteLine($"[quorum] N={n} W={w} R={r} W+R={w + r} {rel} N");
                Console.WriteLine($"trials={trials} fresh_reads={fresh} stale_reads={stale} guarantee_holds={(stale == 0 ? "YES" : "NO")}");
            }
        }
    }
}