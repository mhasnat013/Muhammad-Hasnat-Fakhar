// 232436 - Muhammad Hasnat Fakhar
// Lab Task 02

using System;
using System.Threading;

class Program
{
    static void Worker(int threadNumber)
    {
        int cpu = Thread.GetCurrentProcessorId();

        Console.WriteLine(
            $"Thread {threadNumber}: running on logical CPU {cpu}"
        );
    }

    static void Main()
    {
        int numCores = Environment.ProcessorCount;

        Console.WriteLine("Detected logical cores: " + numCores);

        Thread[] threads = new Thread[numCores];

        for (int i = 0; i < numCores; i++)
        {
            int threadNumber = i;

            threads[i] = new Thread(() => Worker(threadNumber));
            threads[i].Start();
        }

        for (int i = 0; i < numCores; i++)
        {
            threads[i].Join();
        }

        Console.WriteLine();
        Console.WriteLine("All " + numCores + " threads completed.");
        Console.WriteLine("Total threads created: " + numCores);
    }
}