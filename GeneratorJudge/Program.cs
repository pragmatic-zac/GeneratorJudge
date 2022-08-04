using System;
using System.Diagnostics;

var sw = new Stopwatch();
sw.Start();

const long factorA = 16807;
const long factorB = 48271;

const int divisor = 2147483647;
const uint mask = (1 << 16) - 1;

int matches = 0;

long seedA = 873;
long seedB = 583;

for (int i = 0; i < 40_000_000; i++)
{
    if ((GeneratorA() & mask ^ GeneratorB() & mask) == 0)
    {
        matches++;
    }
}

sw.Stop();
Console.WriteLine($"Matches: {matches} in time: {sw.ElapsedMilliseconds}");
// answer is 631, completing in under 1 second

long GeneratorA()
{
    seedA = (seedA * factorA) % divisor;
    return seedA;
}

long GeneratorB()
{
    seedB = (seedB * factorB) % divisor;
    return seedB;
}