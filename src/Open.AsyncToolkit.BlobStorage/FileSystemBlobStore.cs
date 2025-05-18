namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Implementation of <see cref="IBlobStore"/> that uses the file system to store blobs.
/// Each blob is stored as a separate file in a directory structure.
/// </summary>
public sealed class FileSystemBlobStore : IBlobStore
{
	private readonly string _basePath;
	private static readonly char[] InvalidCharacters = Path.GetInvalidFileNameChars();

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
		if (basePath is null)
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
		if (key.Any(static c => InvalidCharacters.Contains(c)))
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
	public Stream? Read(
		string key)
	{
		string path = GetPath(key);

		if (!File.Exists(path)) return null;

		// Let any exceptions propagate to the caller
		return new FileStream(
			path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: 4096,
			useAsync: true);
	}

	/// <inheritdoc />
	public async ValueTask<TryReadResult<byte[]>> TryReadBytesAsync(string key, CancellationToken cancellationToken = default)
	{
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1849 // Call async methods when in an async method
		using var stream = Read(key);
#pragma warning restore CA1849 // Call async methods when in an async method
#pragma warning restore IDE0079 // Remove unnecessary suppression

		if (stream is null) return TryReadResult.NotFound<byte[]>();
		if (stream.Length == 0) return TryReadResult.Success(Array.Empty<byte>());

		byte[] buffer = new byte[stream.Length];

		await stream
			.ReadAsync(buffer, cancellationToken)
			.ConfigureAwait(false);

		return TryReadResult.Success(buffer);
	}

	async ValueTask<TryReadResult<ReadOnlyMemory<byte>>> IReadBlobs<string>.TryReadBytesAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var result = await TryReadBytesAsync(key, cancellationToken).ConfigureAwait(false);
		return result.Success
			? TryReadResult.Success<ReadOnlyMemory<byte>>(result.Value)
			: TryReadResult.NotFound<ReadOnlyMemory<byte>>();
	}

	/// <returns><see langword="true"/> if successful; otherwise <see langword="false"/> if not found.</returns>
	/// <inheritdoc cref="IReadAsync{TKey, TValue}.TryReadAsync(TKey, CancellationToken)" />
	public bool TryRead(
		string key,
#if NETSTANDARD2_0
#else
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
#endif
		out Stream? stream)
	{
		stream = Read(key);
		return stream is not null;
	}

	/// <exception cref="ArgumentNullException">Thrown when key is null or empty, or writeHandler is null.</exception>
	/// <exception cref="ArgumentException">Thrown when key contains invalid file system characters.</exception>
	/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
	/// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
	/// <remarks>
	/// This method writes to a temporary file first and then atomically replaces the target file,
	/// ensuring that readers always see a complete file.
	/// </remarks>
	/// <inheritdoc />
	public async ValueTask<bool> WriteAsync(
		string key,
		bool overwrite,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
	{
		if (writeHandler is null)
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

		if (!overwrite && File.Exists(path))
			return false;

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
			{
				if (!overwrite)
					return false;

				if (await FileEqualsAsync(path, tempPath).ConfigureAwait(false))
				{
					File.Delete(tempPath);
					return false;
				}

				File.Delete(path);
			}

			File.Move(tempPath, path);
			return true;
		}
		finally
		{
			// If an error occurs, attempt to delete the temporary file
			try
			{
				if (File.Exists(tempPath))
					File.Delete(tempPath);
			}
			catch (IOException)
			{
				// Ignore deletion errors on cleanup
			}
		}
	}

	private static async ValueTask<bool> FileEqualsAsync(string path, string tempPath)
	{
		// Quick check: Compare file lengths first
		var originalInfo = new FileInfo(path);
		var tempInfo = new FileInfo(tempPath);

		if (originalInfo.Length != tempInfo.Length)
			return false;

		// If both files are empty, they're equal
		if (originalInfo.Length == 0)
			return true;

		// Buffer size for streaming comparison (4KB is a common file system buffer size)
		const int bufferSize = 4096;

		// Stream both files and compare their contents
		using var originalStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
		using var tempStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);

		byte[] buffer1 = new byte[bufferSize];
		byte[] buffer2 = new byte[bufferSize];

		int bytesRead1, bytesRead2;
		do
		{
#if NETSTANDARD2_0
			bytesRead1 = await originalStream
				.ReadAsync(buffer1, 0, buffer1.Length)
				.ConfigureAwait(false);
			bytesRead2 = await tempStream
				.ReadAsync(buffer2, 0, buffer2.Length)
				.ConfigureAwait(false);
#else
                bytesRead1 = await originalStream
					.ReadAsync(buffer1)
					.ConfigureAwait(false);
                bytesRead2 = await tempStream
					.ReadAsync(buffer2)
					.ConfigureAwait(false);
#endif

			// If we read different amounts, files are different
			if (bytesRead1 != bytesRead2)
				return false;

			// Compare the buffers
			for (int i = 0; i < bytesRead1; i++)
			{
				if (buffer1[i] != buffer2[i])
					return false;
			}
		}
		while (bytesRead1 > 0); // Continue until we've read the whole file

		return true;
	}

	private static Func<Stream, CancellationToken, ValueTask> GetWriteHandler(
		ReadOnlyMemory<byte> data)
		=> (stream, ct) =>
		{
			ct.ThrowIfCancellationRequested();
			return stream.WriteAsync(data, ct);
		};

	/// <inheritdoc />
	ValueTask<bool> IUpdateBlobs<string>.UpdateAsync(
		string key, ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
		=> UpdateAsync(key, cancellationToken, GetWriteHandler(data));

	/// <inheritdoc />
	public ValueTask<bool> CreateAsync(
		string key, ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> CreateAsync(key, cancellationToken, GetWriteHandler(data));

	/// <inheritdoc />
	public ValueTask<bool> CreateOrUpdateAsync(
		string key, ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> CreateOrUpdateAsync(key, cancellationToken, GetWriteHandler(data));

	/// <inheritdoc />
	public ValueTask<bool> CreateAsync(
		string key,
		CancellationToken cancellationToken,
		Func<Stream, CancellationToken, ValueTask> writeHandler)
		=> WriteAsync(key, false, writeHandler, cancellationToken);

	/// <inheritdoc />
	public ValueTask<bool> CreateOrUpdateAsync(
		string key,
		CancellationToken cancellationToken,
		Func<Stream, CancellationToken, ValueTask> writeHandler)
		=> WriteAsync(key, true, writeHandler, cancellationToken);

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

		if (!File.Exists(path))
			return false;

		try
		{
			File.Delete(path);
			return true;
		}
		catch (IOException)
		{
			return false;
		}
	}

	// Explicit interface implementations

	/// <inheritdoc />
	ValueTask<bool> IReadAsync<string, Stream>.ExistsAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(Exists(key));
	}

	/// <inheritdoc />
#pragma warning disable IDE0079 // Remove unnecessary suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Reliability", "CA2000:Dispose objects before losing scope",
		Justification = "Intentional to provide the stream to the caller.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
	ValueTask<TryReadResult<Stream>> IReadAsync<string, Stream>.TryReadAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		TryRead(key, out var stream);
		return new ValueTask<TryReadResult<Stream>>(
			stream is not null
				? TryReadResult.Success(stream)
				: TryReadResult.NotFound<Stream>());
	}

	/// <inheritdoc />
	ValueTask<bool> IDeleteAsync<string>.DeleteAsync(
		string key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(Delete(key));
	}

	/// <inheritdoc />
	public ValueTask<bool> UpdateAsync(
		string key,
		CancellationToken cancellationToken,
		Func<Stream, CancellationToken, ValueTask> writeHandler)
		=> Exists(key) ? WriteAsync(key, true, writeHandler, cancellationToken) : new(false);
}