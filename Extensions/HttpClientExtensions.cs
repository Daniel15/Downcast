namespace Downcast.Extensions;

static class HttpClientExtensions
{
	/// <summary>
	/// Downloads a file from the specified URL, and saves it to a temporary file.
	/// </summary>
	/// <param name="client">HTTP client to use</param>
	/// <param name="url">URL to download</param>
	/// <param name="extension">File extension to use</param>
	/// <returns>Temporary file name</returns>
	public static async Task<string> DownloadToTempAsync(
		this HttpClient client,
		Uri url,
		string extension
	)
	{
		var tempFile = Path.Combine(
			Path.GetTempPath(),
			Path.ChangeExtension(Guid.NewGuid().ToString(), extension)
		);
		//Console.WriteLine($"----> Writing {tempFile}");

		await using var downloadStream = await client.GetStreamAsync(url);
		await using var fileStream = File.Create(tempFile);
		await downloadStream.CopyToAsync(fileStream);
		return tempFile;
	}
}
