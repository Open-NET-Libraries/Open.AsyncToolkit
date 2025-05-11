# Open.AsyncToolkit.KeyValue

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.KeyValue.svg?label=Open.AsyncToolkit.KeyValue)](https://www.nuget.org/packages/Open.AsyncToolkit.KeyValue/)  

A core library providing the foundational building blocks for asynchronous key-value operations.

## 🧱 Foundation Blocks

These are the foundational interfaces for asynchronous access to entities and resources:

- `IReadAsync<TKey, TValue>` - Fundamental read operations (exists, read)
- `ICreateAsync<TKey, TValue>` - Interface for creating new entries
- `IUpdateAsync<TKey, TValue>` - Interface for updating existing entries
- `IDeleteAsync<TKey>` - Interface for deleting entries
- `ICreateOrUpdate<TKey, TValue>` - Combined interface for inserting or updating entries

These interfaces allow for fine-grained control over dependencies, making your code more focused and testable.

## 🏗️ Composite Interfaces

These interfaces build upon the foundation blocks to provide more comprehensive functionality:

- `IAsyncDictionary<TKey, TValue>` - Full-featured async dictionary interface combining read, create, update, and delete operations
- `ISynchronizedAsyncDictionary<TKey, TValue>` - Provides synchronized, exclusive leased access to dictionary entries to prevent concurrency conflicts

## 🔧 Implementations

Ready-to-use implementations built from the foundational components:

- `MemoryAsyncDictionary<TKey, TValue>` - In-memory implementation of async dictionary
- `SynchronizedAsyncDictionary<TKey, TValue>` - Synchronized wrapper for any async dictionary

## 💻 Example Usage

```csharp
// Only depend on the operations you actually need
public class UserProfileService
{
    private readonly IReadAsync<string, UserProfile> _profileReader;
    private readonly IUpdateAsync<string, UserProfile> _profileUpdater;

    public UserProfileService(
        IReadAsync<string, UserProfile> profileReader,
        IUpdateAsync<string, UserProfile> profileUpdater)
    {
        _profileReader = profileReader;
        _profileUpdater = profileUpdater;
    }

    public async Task<UserProfile> GetUserProfileAsync(string userId)
        => await _profileReader.ReadAsync(userId);

    public async Task UpdateUserEmailAsync(string userId, string newEmail)
    {
        var profile = await _profileReader.ReadAsync(userId);
        profile.Email = newEmail;
        await _profileUpdater.UpdateAsync(userId, profile);
    }
}
```

## ⚡ Features

- Interface segregation principles for precise dependency injection
- Async-first design with ValueTask support for high performance
- Thread-safe implementations for concurrent scenarios
- Comprehensive test coverage for reliability

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE) file for details.
