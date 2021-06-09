using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GeneratorJudge
{
    class Program
    {
        // shared data structure for pairs
        private static BlockingCollection<Tuple<bool[], bool[]>> pairs =
            new BlockingCollection<Tuple<bool[], bool[]>>(new ConcurrentQueue<Tuple<bool[], bool[]>>());
        
        // thread safe total number of matches
        private static int matches;

        private static CancellationTokenSource cts = new CancellationTokenSource();
        
        static void Main(string[] args)
        {
            Console.WriteLine("Starting.");

            Task.Factory.StartNew(ThreadedOrchestrator, cts.Token);

                Console.ReadKey();
            cts.Cancel();
        }

        // single threaded - executes in 6 seconds
        static void Orchestrator()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            int matches = 0;
            
            long startingA = 65;
            long startingB = 8921;
            
            // have Gen A and B each produce their next number
            int iteration = 0;
            while (iteration < 40_000_000)
            {
                var a = GeneratorA(startingA);
                var b = GeneratorB(startingB);
                
                // reset starting values
                startingA = a.Item1;
                startingB = b.Item1;

                if (ArraysEqual(a.Item2, b.Item2))
                {
                    matches += 1;
                }
                
                iteration++;
            }
            
            sw.Stop();

            Console.WriteLine($"Total matches: {matches} in {sw.ElapsedMilliseconds} milliseconds.");
            
            Console.WriteLine("Complete.");
        }

        // multi threaded, producer/consumer - executes in 11 seconds
        static void ThreadedOrchestrator()
        {
            var producer = Task.Factory.StartNew(Producer);
            var consumer = Task.Factory.StartNew(Consumer);

            var sw = new Stopwatch();
            sw.Start();
            
            try
            {
                Task.WhenAll(producer, consumer).ContinueWith((a) =>
                {
                    sw.Stop();
                    Console.WriteLine($"Found {matches} matches in {sw.ElapsedMilliseconds} milliseconds.");
                });
            }
            catch (AggregateException e)
            {
                e.Handle(e => true);
            }
        }

        static void Consumer()
        {
            foreach (var pair in pairs.GetConsumingEnumerable())
            {
                if (ArraysEqual(pair.Item1, pair.Item2))
                {
                    Interlocked.Increment(ref matches);
                }
            }
        }

        static void Producer()
        {
            long startingA = 65;
            long startingB = 8921;
            
            int iteration = 0;
            while (iteration < 40_000_000)
            {
                var a = GeneratorA(startingA);
                var b = GeneratorB(startingB);
                
                pairs.Add(new Tuple<bool[], bool[]>(a.Item2, b.Item2));
                
                startingA = a.Item1;
                startingB = b.Item1;
                
                iteration++;
            }
            
            pairs.CompleteAdding();
        }
        
        static Tuple<long, bool[]> GeneratorA(long startingNumber)
        {
            int factor = 16807;
            int divisor = 2147483647;

            long remainder = (startingNumber * factor) % divisor;

            var binary = ConvertToBinary16(remainder);
            return new Tuple<long, bool[]>(remainder, binary);
        }
        
        static Tuple<long, bool[]> GeneratorB(long startingNumber)
        {
            int factor = 48271;
            int divisor = 2147483647;

            long remainder = (startingNumber * factor) % divisor;

            var binary = ConvertToBinary16(remainder);
            return new Tuple<long, bool[]>(remainder, binary);
        }

        // faster than SequenceEquals by about 1.5 seconds
        static bool ArraysEqual(bool[] a, bool[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        // only need the last 16 bits, and much faster than built in ConvertTo
        static bool[] ConvertToBinary16(long n)
        {
            var toReturn = new bool[16];
            
            for (int i = 0; i <= 15; i++) {
                long k = n >> i;
                if ((k & 1) > 0)
                    toReturn[i] = true;
                else
                    toReturn[i] = false;
            }

            Array.Reverse(toReturn);

            return toReturn;
        }
        
    }
}