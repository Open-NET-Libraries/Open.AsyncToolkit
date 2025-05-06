namespace Open.BlobStorageAdapter.FileSystem;

/// <summary>
/// Implementation of <see cref="IBlobStore"/> that uses the file system to store blobs.
/// Each blob is stored as a separate file in a directory structure.
/// </summary>
public class FileSystemBlobStore : IBlobStore
{
	private readonly string _basePath;
	private static readonly char[] InvalidCharacters = Path.GetInvalidFileNameChars();

	/// <summary>
	/// Initializes a new instance of the <see cref="FileSystemBlobStore"/> class.
	/// </summary>
	/// <param name="basePath">The base directory path where blobs will be stored.</param>
	/// <exception cref="ArgumentNullException">Thrown when basePath is null.</exception>
	/// <remarks>
	/// This constructor assumes the directory already exists. Use the static <see cref="Create"/> method 
	/// if you want the directory to be created automatically.
	/// </remarks>
	private FileSystemBlobStore(string basePath)
		=> _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));

	/// <summary>
	/// Creates a new <see cref="FileSystemBlobStore"/> instance, ensuring the base directory exists.
	/// </summary>
	/// <param name="basePath">The base directory path where blobs will be stored.</param>
	/// <returns>A new <see cref="FileSystemBlobStore"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when basePath is null.</exception>
	/// <exception cref="IOException">Thrown when the directory cannot be created.</exception>
	public static FileSystemBlobStore GetOrCreate(string basePath)
	{
		if (basePath == null)
			throw new ArgumentNullException(nameof(basePath));

		// Ensure the directory exists
		Directory.CreateDirectory(basePath);

		return new FileSystemBlobStore(basePath);
	}

	/// <summary>
	/// Gets the full file path for a blob key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <returns>The full file path.</returns>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	private string GetPath(string key)
	{
		if (string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		// Check for invalid characters
		if (key.Any(c => InvalidCharacters.Contains(c)))
			throw new ArgumentException($"Key contains invalid file name characters: {key}", nameof(key));

		return Path.Combine(_basePath, key);
	}

	// Primary synchronous public methods

	/// <summary>
	/// Checks if a blob exists with the specified key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <returns>True if the blob exists; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	public bool Exists(string key) => File.Exists(GetPath(key));

	/// <summary>
	/// Reads a blob with the specified key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <returns>A stream containing the blob's content, or null if the blob doesn't exist.</returns>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	/// <remarks>
	/// The caller is responsible for disposing the returned stream.
	/// </remarks>
	public Stream? Read(string key)
	{
		string path = GetPath(key);

		if (!File.Exists(path))
			return null;

		try
		{
			return new FileStream(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				bufferSize: 4096,
				useAsync: true);
		}
		catch (IOException)
		{
			return null;
		}
	}

	/// <summary>
	/// Asynchronously writes content to a blob with the specified key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty, or writeHandler is null.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	/// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
	/// <remarks>
	/// This method writes to a temporary file first and then atomically replaces the target file,
	/// ensuring that readers always see a complete file.
	/// </remarks>
	public async ValueTask WriteAsync(
		string key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
	{
		if (writeHandler == null)
			throw new ArgumentNullException(nameof(writeHandler));

		cancellationToken.ThrowIfCancellationRequested();

		string path = GetPath(key);
		string? directory = Path.GetDirectoryName(path);

		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		// Create a temporary file for writing
		string tempPath = Path.Combine(
			Path.GetDirectoryName(path) ?? string.Empty,
			$"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}{Path.GetExtension(path)}");

		try
		{
			// Create the stream with FileShare.None to prevent other processes from accessing during write
			using (var stream = new FileStream(
				tempPath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 4096,
				useAsync: true))
			{
				// Use the provided write handler to write to the stream
				await writeHandler(stream, cancellationToken).ConfigureAwait(false);

				// Ensure the stream is fully flushed to disk
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
			}

			// Atomic replacement of the target file with the temporary file
			if (File.Exists(path))
				File.Delete(path);

			File.Move(tempPath, path);
		}
		catch
		{
			// If an error occurs, attempt to delete the temporary file
			try
			{
				if (File.Exists(tempPath))
					File.Delete(tempPath);
			}
			catch
			{
				// Ignore deletion errors on cleanup
			}

			throw;
		}
	}

	/// <summary>
	/// Writes content to a blob with the specified key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <param name="writeAction">An action that writes content to the provided stream.</param>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty, or writeAction is null.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	/// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
	/// <remarks>
	/// This is a synchronous wrapper around the asynchronous <see cref="WriteAsync"/> method.
	/// </remarks>
	public void Write(string key, Action<Stream> writeAction)
	{
		if (writeAction == null)
			throw new ArgumentNullException(nameof(writeAction));

		// Convert synchronous action to asynchronous function and call WriteAsync
		WriteAsync(key, (stream, ct) =>
		{
			writeAction(stream);
			return new ValueTask();
		}).AsTask().GetAwaiter().GetResult();
	}

	/// <summary>
	/// Deletes a blob with the specified key.
	/// </summary>
	/// <param name="key">The blob key.</param>
	/// <returns>True if the blob was deleted successfully; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when key is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	public bool Delete(string key)
	{
		string path = GetPath(key);

		if (File.Exists(path))
		{
			try
			{
				File.Delete(path);
				return true;
			}
			catch
			{
				return false;
			}
		}

		return false;
	}

	// Explicit interface implementations

	/// <inheritdoc />
	ValueTask<bool> IBlobStore<string>.ExistsAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(Exists(key));
	}

	/// <inheritdoc />
	ValueTask<Stream?> IBlobStore<string>.ReadAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<Stream?>(Read(key));
	}

	/// <inheritdoc />
	ValueTask<bool> IBlobStore<string>.DeleteAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(Delete(key));
	}
}