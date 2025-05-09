# SynchronizedAsyncDictionary Tests

This folder contains the organized test suite for the `SynchronizedAsyncDictionary<TKey, TValue>` class.

## Structure

The tests have been organized using partial classes with a shared base class:

- **SynchronizedAsyncDictionaryTestBase.cs**: Contains the shared setup, teardown, and utility methods used by all test classes.
- **SynchronizedAsyncDictionaryBasicTests.cs**: Contains basic functionality tests for core operations.
- **SynchronizedAsyncDictionaryAdvancedTests.cs**: Contains advanced concurrency tests focusing on different aspects of parallel operations.
- **SynchronizedAsyncDictionaryRaceConditionTests.cs**: Contains tests specifically targeting race conditions and deadlock prevention.
- **SynchronizedAsyncDictionaryLeaseTests.cs**: Contains tests focusing on lease operations and their behavior.
- **SynchronizedAsyncDictionaryStressTests.cs**: Contains comprehensive stress tests with diverse operation patterns.
- **SynchronizedAsyncDictionaryWorkloadTests.cs**: Contains tests with different workload patterns (read-heavy, write-heavy, mixed).

## Test Categories

The tests cover several categories:

1. **Basic Functionality**: Verifies that core operations work correctly.
2. **Lease Operations**: Verifies the behavior of lease operations under different conditions.
3. **Concurrency**: Verifies that operations can run concurrently when appropriate.
4. **Race Conditions**: Verifies that operations are properly synchronized to prevent race conditions.
5. **Resource Management**: Verifies that resources are properly managed and cleaned up.
6. **Deadlock Prevention**: Verifies that operations don't deadlock in complex scenarios.
7. **Stress Testing**: Puts the dictionary under extreme load to verify robustness.
8. **Workload Patterns**: Tests different operation distribution patterns.

## Running the Tests

All tests are part of the standard test suite and can be run using the normal test runner.

For performance benchmarking, look at the `CompareDifferentWorkloads_ShouldShowPerformanceCharacteristics` test in `SynchronizedAsyncDictionaryWorkloadTests.cs`.
