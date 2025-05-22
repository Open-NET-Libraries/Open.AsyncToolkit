using System.Diagnostics;
using System.Globalization;

namespace Open.AsyncToolkit.BlobStorage.Tests;

/// <summary>
/// Specialized tests for FileSystemBlobStore that focus on specific file system concerns
/// such as different path separators and handling CSV files.
/// </summary>
internal sealed class FileSystemBlobStoreSpecializedTests
{
	private string _testDirectory = string.Empty;
	private FileSystemBlobStore? _blobStore;

	[Before(Test)]
	public void Setup()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "FileSystemBlobStoreSpecializedTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_testDirectory);
		_blobStore = FileSystemBlobStore.GetOrCreate(_testDirectory);
	}

	[After(Test)]
	public async Task Teardown()
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

	#region CSV File Tests

	[Test]
	public async Task CsvFile_CanBeCreatedReadAndUpdated()
	{
		// Arrange
		const string key = "data.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);

		// Verify CSV structure
		var rows = CsvTestHelper.ParseCsv(content);
		await Assert.That(rows).HasCount(3);
		await Assert.That(rows[0]["Id"]).IsEqualTo("1");
		await Assert.That(rows[0]["Name"]).IsEqualTo("Item1");
		await Assert.That(rows[0]["Value"]).IsEqualTo("10");

		// Act - Update with more rows
		string updatedCsvContent = CsvTestHelper.GenerateCsvContent(5);
		bool updated = await _blobStore.UpdateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(updatedCsvContent);
			await writer.FlushAsync();
		});

		// Assert - Update
		await Assert.That(updated).IsTrue();

		// Read again to verify update
		string updatedContent;
		using (var updatedStream = await _blobStore.ReadAsync(key))
		{
			await Assert.That(updatedStream).IsNotNull();
			using var updatedReader = new StreamReader(updatedStream!, Encoding.UTF8);
			updatedContent = await updatedReader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(updatedContent).IsEqualTo(updatedCsvContent);

		// Verify updated CSV structure
		var updatedRows = CsvTestHelper.ParseCsv(updatedContent);
		await Assert.That(updatedRows).HasCount(5);
	}

	[Test]
	public async Task LargeCsvFile_CanBeHandled()
	{
		// Arrange
		const string key = "large-data.csv";
		string largeCsvContent = CsvTestHelper.GenerateCsvContent(1000);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(largeCsvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(largeCsvContent);

		// Verify CSV structure (sampling)
		var rows = CsvTestHelper.ParseCsv(content);
		await Assert.That(rows).HasCount(1000);
		await Assert.That(rows[0]["Id"]).IsEqualTo("1");
		await Assert.That(rows[999]["Id"]).IsEqualTo("1000");

		// Also check file size to ensure it was written properly
		string filePath = Path.Combine(_testDirectory, key);
		var fileInfo = new FileInfo(filePath);
		await Assert.That(fileInfo.Exists).IsTrue();
		await Assert.That(fileInfo.Length).IsGreaterThan(1000); // Should be well over 1000 bytes
	}

	[Test]
	public async Task CsvFile_WithQuotedFields_IsHandledCorrectly()
	{
		// Arrange
		const string key = "quoted-fields.csv";
		const string csvContent = "Id,Name,Value,Description\n" +
								  "1,\"Item, with comma\",10,\"Description with \"\"quotes\"\" inside\"\n" +
								  "2,\"Line\nbreak\",20,\"Multiple\nline\nbreaks\"";

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);

		// Parse and verify complex CSV structure
		var rows = CsvTestHelper.ParseCsv(content);
		await Assert.That(rows).HasCount(2);
		await Assert.That(rows[0]["Name"]).IsEqualTo("Item, with comma");
		await Assert.That(rows[0]["Description"]).Contains("quotes");
	}

	[Test]
	public async Task CsvFile_WithUtf8Bom_IsHandledCorrectly()
	{
		// Arrange
		const string key = "utf8bom.csv";
		const string csvContent = "Id,Name,Value\n1,Item with é and ñ,100\n2,Item with ü and ç,200";

		// Act - Create with UTF8 BOM
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			// Use UTF8 encoding with BOM
			using var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read (should correctly handle UTF8 with BOM)
			await Assert.That(readStream).IsNotNull();
			// Use a detection-based reader to properly handle BOM
			using var reader = new StreamReader(readStream!, detectEncodingFromByteOrderMarks: true);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);

		// Check that special characters are preserved
		await Assert.That(content).Contains("é");
		await Assert.That(content).Contains("ñ");
		await Assert.That(content).Contains("ü");
		await Assert.That(content).Contains("ç");
	}

	[Test]
	public async Task CsvFile_ReadAndParseThenModify_WorksCorrectly()
	{
		// Arrange
		const string key = "modify-csv.csv";
		string initialCsvContent = CsvTestHelper.GenerateCsvContent(5);

		// Act - Create initial CSV
		await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(initialCsvContent);
			await writer.FlushAsync();
		});

		// Act - Read and parse
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		var rows = CsvTestHelper.ParseCsv(content);

		// Modify the parsed data
		rows.Add(new Dictionary<string, string>
		{
			["Id"] = "6",
			["Name"] = "New Item",
			["Value"] = "60",
			["Description"] = "Added programmatically"
		});

		// Reconstruct CSV
		var sb = new StringBuilder();
		sb.AppendLine("Id,Name,Value,Description");

		foreach (var row in rows)
		{
			sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
				"{0},{1},{2},\"{3}\"",
				row["Id"],
				row["Name"],
				row["Value"],
				row["Description"]));
		}

		string modifiedCsvContent = sb.ToString();

		// Act - Update with modified content
		bool updated = await _blobStore.UpdateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(modifiedCsvContent);
			await writer.FlushAsync();
		});

		// Assert
		await Assert.That(updated).IsTrue();

		// Verify the update
		string updatedContent;
		using (var updatedStream = await _blobStore.ReadAsync(key))
		{
			using var updatedReader = new StreamReader(updatedStream!, Encoding.UTF8);
			updatedContent = await updatedReader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		var updatedRows = CsvTestHelper.ParseCsv(updatedContent);

		await Assert.That(updatedRows).HasCount(6);
		await Assert.That(updatedRows[5]["Id"]).IsEqualTo("6");
		await Assert.That(updatedRows[5]["Name"]).IsEqualTo("New Item");
	}

	#endregion

	#region Path Separator Tests

	[Test]
	public async Task ForwardSlashInKey_WorksCorrectly()
	{
		// Arrange
		const string key = "folder/subfolder/data.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check that file exists with correct directory structure
		string normalizedPath = Path.Combine(_testDirectory, "folder", "subfolder", "data.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	[Test]
	public async Task BackslashInKey_WorksCorrectly()
	{
		// Arrange
		const string key = @"folder\subfolder\data.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check that file exists with correct directory structure
		string normalizedPath = Path.Combine(_testDirectory, "folder", "subfolder", "data.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	[Test]
	public async Task MixedSeparatorsInKey_WorksCorrectly()
	{
		// Arrange
		const string key = @"folder1/subfolder1\subfolder2/data.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		Debug.Assert(_blobStore is not null);
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check that file exists with correct directory structure
		string normalizedPath = Path.Combine(_testDirectory, "folder1", "subfolder1", "subfolder2", "data.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read with original key (mixed separators)
		string content;
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);

		// Act - Try to read with normalized paths (all forward slashes)
		string forwardSlashKey = "folder1/subfolder1/subfolder2/data.csv";
		bool forwardSlashExists;
		using (var readStream2 = await _blobStore.ReadAsync(forwardSlashKey))
		{
			// Assert - Should still find the file
			forwardSlashExists = readStream2 != null;
		} // Ensure stream is closed before continuing

		await Assert.That(forwardSlashExists).IsTrue();

		// Act - Try to read with normalized paths (all backslashes)
		string backslashKey = @"folder1\subfolder1\subfolder2\data.csv";
		bool backslashExists;
		using (var readStream3 = await _blobStore.ReadAsync(backslashKey))
		{
			// Assert - Should still find the file
			backslashExists = readStream3 != null;
		} // Ensure stream is closed before continuing

		await Assert.That(backslashExists).IsTrue();
	}

	[Test]
	public async Task DeeplyNestedFolders_WorkCorrectly()
	{
		// Arrange
		const string key = "level1/level2/level3/level4/level5/level6/data.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check that file exists with correct nested directory structure
		string normalizedPath = Path.Combine(
			_testDirectory, "level1", "level2", "level3", "level4", "level5", "level6", "data.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	#endregion

	#region Special Character Tests

	[Test]
	public async Task FileNameWithSpaces_IsHandledCorrectly()
	{
		// Arrange
		const string key = "folder/file with spaces.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check file exists
		string normalizedPath = Path.Combine(_testDirectory, "folder", "file with spaces.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	[Test]
	public async Task FileNameWithSpecialCharacters_IsHandledCorrectly()
	{
		// Arrange - Use characters that are valid in file names
		const string key = "folder/file-with_special.characters(1).csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check file exists
		string normalizedPath = Path.Combine(_testDirectory, "folder", "file-with_special.characters(1).csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	#endregion

	#region File Extension Tests

	[Test]
	public async Task MultipleFileExtensions_AreHandledCorrectly()
	{
		// Arrange
		const string key = "data.backup.csv";
		string csvContent = CsvTestHelper.GenerateCsvContent(3);

		// Act - Create
		bool created = await _blobStore!.CreateAsync(key, async stream =>
		{
			using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
			await writer.WriteAsync(csvContent);
			await writer.FlushAsync();
		});

		// Assert - Create
		await Assert.That(created).IsTrue();

		// Check file exists
		string normalizedPath = Path.Combine(_testDirectory, "data.backup.csv");
		await Assert.That(File.Exists(normalizedPath)).IsTrue();

		// Act - Read
		string content;
		Debug.Assert(_blobStore is not null);
		using (var readStream = await _blobStore.ReadAsync(key))
		{
			// Assert - Read
			await Assert.That(readStream).IsNotNull();
			using var reader = new StreamReader(readStream!, Encoding.UTF8);
			content = await reader.ReadToEndAsync();
		} // Ensure stream is closed before continuing

		await Assert.That(content).IsEqualTo(csvContent);
	}

	#endregion
}