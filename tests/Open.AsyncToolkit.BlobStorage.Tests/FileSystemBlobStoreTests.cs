using Open.AsyncToolkit.BlobStorage;

namespace Open.AsyncToolkit.BlobStorage.Tests;

[InheritsTests]
internal class FileSystemBlobStoreTests : BlobStoreTestsBase<FileSystemBlobStore>
{
    private readonly string _testDirectory
        = Path.Combine(Path.GetTempPath(), "FileSystemBlobStoreTests", Guid.NewGuid().ToString());
        
    private FileSystemBlobStore? _blobStore;

    protected override FileSystemBlobStore CreateBlobStore()
    {
        // Ensure the test directory exists
        Directory.CreateDirectory(_testDirectory);
        
        _blobStore = FileSystemBlobStore.GetOrCreate(_testDirectory);
        return _blobStore;
    }

    protected override async Task CleanupBlobStoreAsync()
    {
        try
        {
            // Attempt to delete the temporary directory if it exists
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore any failures during cleanup
        }
        
        await Task.CompletedTask;
    }
}
