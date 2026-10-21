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
            int size = world.Size;

            string mode = args.Length > 0 ? args[0] : "master";

            if (mode == "master")
            {
                // ==========================================
                // CLIENT-SERVER (Task Farm) STYLE
                // ==========================================
                if (rank == 0)
                {
                    int totalTasks = 20;
                    int tasksServed = 0;
                    Console.WriteLine($"Master distributing {totalTasks} tasks to {size - 1} workers...");
                    
                    // Send initial tasks
                    for (int i = 1; i < size; i++) world.Send(i, i, 0); 
                    
                    while (tasksServed < totalTasks)
                    {
                        Status status = world.Probe(Communicator.anySource, 1);
                        world.Receive<int>(status.Source, 1); // Receive completed task
                        tasksServed++;
                        
                        if (tasksServed + (size - 1) <= totalTasks)
                            world.Send(tasksServed + (size - 1), status.Source, 0);
                        else
                            world.Send(-1, status.Source, 0); // Poison pill (no more tasks)
                    }
                    Console.WriteLine("Master finished all tasks.");
                }
                else
                {
                    int tasksDone = 0;
                    while (true)
                    {
                        int taskId = world.Receive<int>(0, 0);
                        if (taskId == -1) break; // Poison pill received
                        
                        tasksDone++;
                        world.Send(tasksDone, 0, 1); // Send result back to master
                    }
                    Console.WriteLine($"Worker {rank} served {tasksDone} tasks.");
                }
            }
            else if (mode == "ring")
            {
                // ==========================================
                // PEER-TO-PEER (Token Ring) STYLE
                // ==========================================
                int nextRank = (rank + 1) % size;
                int prevRank = (rank - 1 + size) % size;
                int token = 0;
                int rounds = 10;

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < rounds; i++)
                {
                    if (rank == 0)
                    {
                        world.Send(token, nextRank, 0);
                        token = world.Receive<int>(prevRank, 0);
                    }
                    else
                    {
                        token = world.Receive<int>(prevRank, 0);
                        token++;
                        world.Send(token, nextRank, 0);
                    }
                }
                sw.Stop();
                if (rank == 0)
                {
                    double timePerMsg = sw.Elapsed.TotalMilliseconds / (rounds * size);
                    Console.WriteLine($"Ring Time: {sw.Elapsed.TotalMilliseconds:F2} ms for {rounds * size} messages");
                    Console.WriteLine($"Time per message: {timePerMsg:F4} ms");
                }
            }
        }
    }
}