# Open.AsyncToolkit.HashedRepository

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.HashedRepository.svg?label=Open.AsyncToolkit.HashedRepository)](https://www.nuget.org/packages/Open.AsyncToolkit.HashedRepository/)  

Content-addressable storage built on top of the Open.AsyncToolkit.BlobStorage interfaces. This library provides tools for managing blob data based on its content hash, ensuring data deduplication and integrity.

## 🧩 Core Components

- `IHashProvider<TKey>` - Interface for generating content hashes
- `IIdempotentRepository<TKey>` - Repository that ensures uniqueness based on content
- `HashedBlobRepository<TKey>` - Implementation that deduplicates blobs based on content hash
- `Sha256HashProvider` - SHA-256 implementation of the hash provider

## 🌟 Key Features

- **Content-Addressable Storage**: Store data once, reference it many times
- **Automatic Deduplication**: Identical content is stored only once, regardless of how many times it's uploaded
- **Data Integrity**: Content is verified against its hash
- **Space Efficiency**: Eliminates redundant storage of identical content
- **Built on AsyncToolkit Patterns**: Follows the same design principles as the other AsyncToolkit libraries

## 💻 Example Usage

```csharp
// Create the content-addressable repository
var blobStore = new FileSystemBlobStore("C:/data/blobs");
var hashProvider = new Sha256HashProvider();
var repository = new HashedBlobRepository<string>(blobStore, hashProvider);

// Store content and get its hash
byte[] data = Encoding.UTF8.GetBytes("Hello, world!");
string contentHash = await repository.PutAsync(data);

// Later, retrieve the content using its hash
using var contentStream = await repository.GetAsync(contentHash);
using var reader = new StreamReader(contentStream);
string retrievedContent = await reader.ReadToEndAsync();
Console.WriteLine(retrievedContent); // Outputs: Hello, world!

// Store the same content again - it will be deduplicated
byte[] sameData = Encoding.UTF8.GetBytes("Hello, world!");
string sameContentHash = await repository.PutAsync(sameData);
Console.WriteLine(sameContentHash == contentHash); // Outputs: true
```

## 💡 Use Cases

- **File deduplication**: Store files efficiently by eliminating duplicates
- **Caching systems**: Use content hashes as cache keys
- **Data validation**: Verify data integrity against known hashes
- **Immutable content storage**: Create systems where content cannot be changed, only referenced

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE) file for details.
