using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace Downcast.Handlers;

class MixcloudHandler : IHandler
{
	private readonly HttpClient _client;
	private readonly IConfiguration _config;

	public MixcloudHandler(HttpClient client, IConfiguration config)
	{
		_client = client;
		_config = config;
	}

	public async Task<IEnumerable<FeedItem>> ParseFeed(PodcastConfig config)
	{
		var apiUrl = new UriBuilder(config.Url)
		{
			Host = "api.mixcloud.com",
			Path = config.Url.AbsolutePath.TrimEnd('/') + "/cloudcasts/",
			Query = "limit=100",

		}.Uri;
		await using var stream = await _client.GetStreamAsync(apiUrl);
		var json = await JsonSerializer.DeserializeAsync<MixcloudFeed>(stream, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		});

		return json.Data.Select(rawItem => new FeedItem(
			Title: rawItem.Name,
			Description: "",
			Artist: rawItem.User.Name,
			PublishedDateTime: rawItem.CreatedTime,
			PageUrl: rawItem.Url,
			ImageUrl: rawItem.Pictures.ExtraLarge2 ?? rawItem.Pictures.ExtraLarge,
			Mp3Url: rawItem.Url,
			FileExtension: "m4a"
		));
	}

	public async Task<string> DownloadToTempAsync(FeedItem item)
	{
		var tempFile = Path.Combine(
			Path.GetTempPath(),
			Path.ChangeExtension(Guid.NewGuid().ToString(), item.FileExtension)
		);
		var process = Process.Start(new ProcessStartInfo
		{
			FileName = "youtube-dl", // _config.GetValue<string>("YoutubeDLPath")
			Arguments = ArgumentEscaper.EscapeAndConcatenate(new []
			{
				"-o",
				tempFile,
				item.PageUrl.ToString(),
			}),
		});
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			throw new Exception("Download failed");
		}

		return tempFile;
	}

	private record MixcloudFeed(
		IEnumerable<MixcloudItem> Data
	);

	private record MixcloudItem(
		[property: JsonPropertyName("created_time")]
		DateTime CreatedTime,
		string Name,
		MixcloudPictures Pictures,
		Uri Url,
		MixcloudUser User
	);

	private record MixcloudUser(
		string Name
	);

	private record MixcloudPictures(
		[property: JsonPropertyName("extra_large")]
		Uri? ExtraLarge,
		[property: JsonPropertyName("1024wx1024h")]
		Uri? ExtraLarge2
	);
}
