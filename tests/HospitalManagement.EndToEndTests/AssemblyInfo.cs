using Xunit;

// Each browser test owns a PostgreSQL container and an in-process web host.
// Serial execution keeps resource-constrained CI runners deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
