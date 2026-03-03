```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method       | Mean      | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|------------- |----------:|----------:|----------:|------:|----------:|------------:|
| LinearSearch | 85.896 μs | 0.2424 μs | 0.2148 μs |  1.00 |         - |          NA |
| KDTreeSearch |  2.530 μs | 0.0123 μs | 0.0102 μs |  0.03 |         - |          NA |
