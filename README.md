# Open.AsyncToolkit

.NET libraries providing composable, asynchronous interfaces for key-value storage, blob operations, and more.

[![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.KeyValue.svg)](https://www.nuget.org/packages/Open.AsyncToolkit.KeyValue/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE)

## Overview

Open.AsyncToolkit offers a set of focused interfaces and implementations for common asynchronous programming patterns. The libraries follow interface segregation principles to enable precise dependency injection and composability.

The toolkit supports .NET Standard 2.0 and above, including the latest .NET versions.

## Libraries

The Open.AsyncToolkit collection is organized as a set of foundational building blocks that snap together to create powerful, composable systems.

### Open.AsyncToolkit.KeyValue

A core library providing the foundational building blocks for asynchronous key-value operations.

#### 🧱 Foundation Blocks

These are the foundational interfaces for asynchronous access to entities and resources:

- `IReadAsync<TKey, TValue>` - Fundamental read operations (exists, read)
- `ICreateAsync<TKey, TValue>` - Interface for creating new entries
- `IUpdateAsync<TKey, TValue>` - Interface for updating existing entries
- `IDeleteAsync<TKey>` - Interface for deleting entries
- `ICreateOrUpdate<TKey, TValue>` - Combined interface for inserting or updating entries

These interfaces allow for fine-grained control over dependencies, making your code more focused and testable:

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

This approach allows you to:
- Depend only on the exact operations your class needs
- Easily mock dependencies in unit tests
- Swap implementations without changing your business logic
- Compose larger interfaces from these building blocks

#### 🏗️ Composite Interfaces

These interfaces build upon the foundation blocks to provide more comprehensive functionality:

- `IAsyncDictionary<TKey, TValue>` - Full-featured async dictionary interface combining read, create, update, and delete operations
- `ISynchronizedAsyncDictionary<TKey, TValue>` - Provides synchronized, exclusive leased access to dictionary entries to prevent concurrency conflicts

#### 🔧 Implementations

Ready-to-use implementations built from the foundational components:

- `MemoryAsyncDictionary<TKey, TValue>` - In-memory implementation of async dictionary
- `SynchronizedAsyncDictionary<TKey, TValue>` - Synchronized wrapper for any async dictionary

### Open.AsyncToolkit.BlobStorage

Building on the same principles as the KeyValue library, this package provides composable interfaces for blob storage operations.

#### 🧱 Foundation Blocks

These interfaces provide safe, asynchronous access to binary data (blobs) through Stream operations:

- `IReadBlobs<TKey>` - Interface for safely reading blob data, returning a managed Stream
- `ICreateBlobs<TKey>` - Interface for creating new blobs through a delegate-based Stream API
- `IUpdateBlobs<TKey>` - Interface for updating existing blobs with proper Stream lifecycle management
- `IDeleteBlobs<TKey>` - Interface for deleting blobs

The blob interfaces are specifically designed to manage the Stream lifecycle properly, ensuring resources are disposed correctly through delegate patterns rather than requiring developers to manage streams manually. This approach prevents resource leaks and improper stream handling.

#### 🏗️ Composite Interfaces

- `IBlobRepo<TKey>` - Read-only blob repository interface
- `IMutableBlobRepo<TKey>` - Repository interface with mutation capabilities
- `IBlobStore<TKey>` - Complete blob storage interface with full CRUD operations

### Open.AsyncToolkit.BlobStorage.FileSystem

A concrete implementation of the blob storage interfaces using the file system.

- `FileSystemBlobStore` - Stores blobs as files in a directory structure, implementing the full `IBlobStore` interface

### Open.AsyncToolkit.BlobStorage.HashedRepository

Content-addressable storage built on top of the foundational interfaces.

- `IHashProvider` - Interface for generating content hashes
- `IIdempotentRepository<TKey>` - Repository that ensures uniqueness based on content
- `HashedBlobRepository` - Implementation that deduplicates blobs based on content hash
- `Sha256HashProvider` - SHA-256 implementation of the hash provider

## Features

- **Building block architecture**: Small, focused interfaces that can be combined to create complex systems
- **Async-first design**: Built around Task-based async patterns with ValueTask support for high performance
- **Interface segregation**: Granular interfaces allowing precise dependency injection
- **Cross-platform**: Works on all .NET platforms supporting .NET Standard 2.0 and above
- **Highly composable**: Mix and match components to build custom solutions
- **Modern C# features**: Utilizes the latest C# language features where appropriate
- **Well-tested**: Comprehensive test coverage ensures reliability

## Getting Started

Install the packages you need from NuGet:

```bash
dotnet add package Open.AsyncToolkit.KeyValue
dotnet add package Open.AsyncToolkit.BlobStorage
dotnet add package Open.AsyncToolkit.BlobStorage.FileSystem
dotnet add package Open.AsyncToolkit.BlobStorage.HashedRepository
```

### Using the Building Blocks

The toolkit is designed to let you implement or use exactly what you need:

```csharp
// Implement only the specific interfaces you need
public class MyCustomStore
  : IReadAsync<string, string>, 
    IUpdateAsync<string, string>
{
    // Implementation details...
}

// Or use the pre-built implementations
var dictionary = new MemoryAsyncDictionary<string, string>()
    .AsSynchronized();
await dictionary.CreateAsync("key", "value");

// File system blob storage
var blobStore = FileSystemBlobStore.GetOrCreate("./blobs");
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.


