# Open.AsyncToolkit

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Open-NET-Libraries/Open.AsyncToolkit/blob/main/LICENSE)

## Overview

Open.AsyncToolkit offers a set of focused interfaces and implementations for common asynchronous programming patterns. The libraries follow interface segregation principles to enable precise dependency injection and composability.

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [Open.AsyncToolkit.KeyValue](src/Open.AsyncToolkit.KeyValue/README.md) | Core library providing foundational building blocks for asynchronous key-value operations | [![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.KeyValue.svg?label=NuGet)](https://www.nuget.org/packages/Open.AsyncToolkit.KeyValue/) |
| [Open.AsyncToolkit.BlobStorage](src/Open.AsyncToolkit.BlobStorage/README.md) | Composable interfaces for blob storage operations | [![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.BlobStorage.svg?label=NuGet)](https://www.nuget.org/packages/Open.AsyncToolkit.BlobStorage/) |
| [Open.AsyncToolkit.HashedRepository](src/Open.AsyncToolkit.HashedRepository/README.md) | Content-addressable storage built on top of the foundational interfaces | [![NuGet](https://img.shields.io/nuget/v/Open.AsyncToolkit.HashedRepository.svg?label=NuGet)](https://www.nuget.org/packages/Open.AsyncToolkit.HashedRepository/) |

## Key Principles

### 🧱 Building Block Architecture

All packages in Open.AsyncToolkit are built using a foundation of small, focused interfaces that can be composed to create more complex functionality:

- **Interface Segregation**: Granular interfaces allow for precise dependency injection
- **Composability**: Mix and match components to build custom solutions
- **Testability**: Easily mock dependencies in unit tests

### Example

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

## Features

- **Async-first design**: Built around ValueTask-based async patterns for high performance
- **Cross-platform**: Works on all .NET platforms supporting .NET Standard 2.0, 2.1, and .NET 9
- **Modern C# features**: Utilizes the latest C# language features
- **Well-tested**: Comprehensive test coverage ensures reliability

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


