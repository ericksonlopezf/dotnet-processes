// Copyright © Erickson Lopez. MIT License.
using Xunit;

// Serializes Testcontainers execution across different database engines to prevent Docker resource exhaustion
[assembly: CollectionBehavior(DisableTestParallelization = true)]
