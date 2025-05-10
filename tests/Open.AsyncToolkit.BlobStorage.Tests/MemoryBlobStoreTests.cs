using Open.AsyncToolkit.BlobStorage;

namespace Open.AsyncToolkit.BlobStorage.Tests;

[InheritsTests]
internal class MemoryBlobStoreTests : BlobStoreTestsBase<MemoryBlobStore>
{
    private MemoryBlobStore? _blobStore;

    protected override MemoryBlobStore CreateBlobStore()
    {
        _blobStore = new MemoryBlobStore();
        return _blobStore;
    }

    protected override Task CleanupBlobStoreAsync()
    {
        _blobStore = null;
        return Task.CompletedTask;
    }
}
