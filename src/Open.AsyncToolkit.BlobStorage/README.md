# Open.AsyncToolkit.BlobStorage

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.BlobStorage.svg?label=Open.AsyncToolkit.BlobStorage)](https://www.nuget.org/packages/Open.AsyncToolkit.BlobStorage/)  

Building on the same principles as the KeyValue library, this package provides composable interfaces for blob storage operations.

## 🧱 Foundation Blocks

These interfaces provide safe, asynchronous access to binary data (blobs) in multiple ways:

- `IReadBlobs<TKey>` - Interface for safely reading blob data
- `ICreateBlobs<TKey>` - Interface for creating new blobs 
- `IUpdateBlobs<TKey>` - Interface for updating existing blobs
- `IDeleteBlobs<TKey>` - Interface for deleting blobs

The blob interfaces offer multiple ways to interact with binary data:

1. **Using byte arrays** - Directly pass byte[] for simple operations
2. **Using ReadOnlyMemory<byte>** - High-performance memory-efficient operations 
3. **Using Stream delegate patterns** - For more complex streaming scenarios

The Stream-based APIs are specifically designed to manage the Stream lifecycle properly, ensuring resources are disposed correctly through delegate patterns rather than requiring developers to manage streams manually.

## 🏗️ Composite Interfaces

- `IBlobRepo<TKey>` - Read-only blob repository interface
- `IMutableBlobRepo<TKey>` - Repository interface with mutation capabilities
- `IBlobStore<TKey>` - Complete blob storage interface with full CRUD operations

## 🔧 Simple Implementations

Ready-to-use implementations for common blob storage scenarios:

- `MemoryBlobStore` - Stores blobs as bytes in memory
- `FileSystemBlobStore` - Stores blobs as files in a directory structure

## 💻 Example Usage

### Basic Setup

```csharp
// Create a file system blob store
var fileStore = new FileSystemBlobStore("D:/data/blobs");

// Or use an in-memory store
var memoryStore = new MemoryBlobStore();
```

And of course you can implement your own to use with any storage medium.


### Creating & Updating Blobs

```csharp
// Create a blob using byte array
string blobId = "document-123";
byte[] data = Encoding.UTF8.GetBytes("Hello, world!");

// Returns `true` if the blob was added/created;
// otherwise returns `false` if the file already exists.
bool wasCreated
    = await blobStore.CreateAsync(blobId, data);

// Returns `true` if the blob exists and was updated/modified.
bool wasExistingModified
    = await blobStore.UpdateAsync(blobId, data);

// Returns `true` if the created or modified.
bool wasCreatedOrModified
    = await blobStore.CreateOrUpdateAsync(blobId, data);

// Stream-based update for more complex scenarios.
await blobStore.CreateOrUpdateAsync("document-123",
    async (outputStream, cancellationToken) => {
        // Could apply more fine grained stream logic here.
        await outputStream.WriteAsync(data, cancellationToken);
    });
```

### Reading Blobs

```csharp
// Try-based pattern returns success/failure status
var bytesResult = await blobStore.TryReadBytesAsync("document-123");
if (result.Success)
{
    ReadOnlyMemory<byte> bytes = result.Value;
    // Use the bytes...
}

// Try to get a stream instead.
var result = await blobStore.TryReadAsync("document-123");
if (result.Success)
{
    using Stream resultStream = result.Value;
    // Use the stream...
}


// Or compare against null.
using var stream = await blobStore.ReadAsync("document-123");
if (stream is not null)
{
    using var reader = new StreamReader(stream);
    string content = await reader.ReadToEndAsync();
    // Outputs: Hello, world!
    Console.WriteLine(content);
}
```

### Deleting Blobs

```csharp
// Delete a blob
bool deleted = await blobStore.DeleteAsync("document-123");
```

## ⚡ Features

- Multiple API options: `byte[]`, `ReadOnlyMemory<byte>`, and `Stream` based operations
- Proper Stream lifecycle management 
- Async-first API design with ValueTask support
- Composable interfaces for flexibility
- File system and in-memory implementations included

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE) file for details.
