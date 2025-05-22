using System.Globalization;

namespace Open.AsyncToolkit.BlobStorage.Tests;

/// <summary>
/// Utility class for working with CSV files in blob storage tests.
/// </summary>
internal static class CsvTestHelper
{
	/// <summary>
	/// Generates sample CSV content with the specified number of rows.
	/// </summary>
	/// <param name="rowCount">The number of rows to generate.</param>
	/// <param name="includeHeader">Whether to include a header row.</param>
	/// <returns>A string containing CSV content.</returns>
	public static string GenerateCsvContent(int rowCount, bool includeHeader = true)
	{
		var sb = new StringBuilder();

		if (includeHeader)
		{
			sb.AppendLine("Id,Name,Value,Description");
		}

		for (int i = 1; i <= rowCount; i++)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"{i},Item{i},{i * 10},\"Description for item {i}\"");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Parses CSV content into a list of dictionaries where each dictionary represents a row.
	/// </summary>
	/// <param name="csvContent">The CSV content to parse.</param>
	/// <returns>A list of dictionaries, each representing a row with column name keys.</returns>
	public static List<Dictionary<string, string>> ParseCsv(string csvContent)
	{
		var result = new List<Dictionary<string, string>>();

		// Handle different line ending formats and split into lines
		string[] lines = csvContent.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length == 0)
			return result;

		// Get headers from the first line
		string[] headers = ParseCsvLine(lines[0]);

		// Process each data row
		for (int i = 1; i < lines.Length; i++)
		{
			// Skip empty lines
			if (string.IsNullOrWhiteSpace(lines[i]))
				continue;

			// Handle multi-line values that might be inside quotes
			string lineToProcess = lines[i];
			int j = i;
			while (CountQuotes(lineToProcess) % 2 != 0 && j + 1 < lines.Length)
			{
				// This line has unclosed quotes, append next line
				j++;
				lineToProcess += "\n" + lines[j];
				i = j;  // Skip these lines in next iteration
			}

			string[] values = ParseCsvLine(lineToProcess);
			var row = new Dictionary<string, string>();

			for (int k = 0; k < Math.Min(headers.Length, values.Length); k++)
			{
				row[headers[k]] = values[k];
			}

			result.Add(row);
		}

		return result;
	}

	/// <summary>
	/// Counts the number of unescaped quotes in a string.
	/// </summary>
	private static int CountQuotes(string line)
	{
		int count = 0;

		for (int i = 0; i < line.Length; i++)
		{
			if (line[i] == '"')
			{
				if (i + 1 < line.Length && line[i + 1] == '"')
				{
					// This is an escaped quote (""), skip the next quote
					i++;
					continue;
				}

				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Parses a single CSV line, handling quoted values.
	/// </summary>
	/// <param name="line">The CSV line to parse.</param>
	/// <returns>An array of values from the line.</returns>
	private static string[] ParseCsvLine(string line)
	{
		if (string.IsNullOrEmpty(line))
			return [];

		var result = new List<string>();
		bool inQuotes = false;
		int startIndex = 0;

		for (int i = 0; i < line.Length; i++)
		{
			// Handle escaped quotes ("") within quoted fields
			if (line[i] == '"')
			{
				if (i + 1 < line.Length && line[i + 1] == '"')
				{
					// Skip the escaped quote
					i++;
					continue;
				}

				inQuotes = !inQuotes;
				continue;
			}

			if (line[i] == ',' && !inQuotes)
			{
				result.Add(CleanValue(line.Substring(startIndex, i - startIndex)));
				startIndex = i + 1;
			}
		}

		// Add the last value
		if (startIndex <= line.Length)
		{
			result.Add(CleanValue(line.Substring(startIndex)));
		}

		return result.ToArray();
	}

	/// <summary>
	/// Cleans a CSV value by removing quotes and trimming whitespace.
	/// </summary>
	/// <param name="value">The value to clean.</param>
	/// <returns>The cleaned value.</returns>
	private static string CleanValue(string value)
	{
		value = value.Trim();

		// Remove surrounding quotes if present
		if (value.StartsWith('"') &&
			value.EndsWith('"') &&
			value.Length >= 2)
		{
			value = value.Substring(1, value.Length - 2);
			// Replace escaped quotes with single quotes
			value = value.Replace("\"\"", "\"", StringComparison.Ordinal);
		}

		return value;
	}
}