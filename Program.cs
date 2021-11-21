using System.Text.RegularExpressions;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TagLib.Id3v2;

namespace Downcast;

class Program
{
	private static readonly Regex _invalidPathChars = new("[^a-zA-Z0-9_ ]+", RegexOptions.Compiled);
	private readonly IConfiguration _config;

	private readonly HttpClient _client = new()
	{
		DefaultRequestHeaders =
		{
			{
				"Accept",
				"application/rss+xml, application/rdf+xml;q=0.8, application/atom+xml;q=0.6, application/xml;q=0.4, text/xml;q=0.4"
			},
			{"User-Agent", "Downcast/1.0 (https://d.sb/downcast)"},
		},
	};

	[Option(Description = "Just output what would be done, without actually doing it")]
	public bool DryRun { get; } = false;

	public static Task Main(string[] args)
		=> Host.CreateDefaultBuilder(args).RunCommandLineApplicationAsync<Program>(args);

	public Program(IConfiguration config)
	{
		_config = config;
	}

	private async Task OnExecute()
	{
		var podcastConfigs = _config.GetSection("Podcasts").Get<IList<PodcastConfig>>();

		foreach (var podcastConfig in podcastConfigs)
		{
			await ProcessPodcastAsync(podcastConfig);
		}
	}

	async Task ProcessPodcastAsync(PodcastConfig config)
	{
		Console.WriteLine($"Processing {config.Name}");
		await using var stream = await _client.GetStreamAsync(config.Url);
		var xml = XElement.Load(stream);
		var items = xml.Descendants("item");

		foreach (var rawItem in items)
		{
			var item = FeedItem.FromXml(rawItem);
			await ProcessItemAsync(config, item);
		}
	}

	async Task ProcessItemAsync(PodcastConfig config, FeedItem item)
	{
		var mp3Path = BuildPath(config, item.Title);
		var alreadyExists = File.Exists(mp3Path);

		Console.WriteLine($"{item.Title} by {item.Artist}");

		if (alreadyExists)
		{
			Console.WriteLine($"----> {mp3Path} already exists. Skipping.");
			Console.WriteLine();
			return;
		}

		if (DryRun)
		{
			Console.WriteLine($"----> Would be downloaded to {mp3Path}");
			Console.WriteLine();
			return;
		}

		Console.WriteLine("----> Downloading");

		// Download MP3 and cover image in parallel
		var tempMp3PathTask = DownloadToTempAsync(item.Mp3Url, "mp3");
		var tempCoverPathTask = DownloadToTempAsync(
			item.ImageUrl, 
			Path.GetExtension(item.ImageUrl.LocalPath)
		);
		var tempMp3Path = await tempMp3PathTask;
		var tempCoverPath = await tempCoverPathTask;

		SetTags(item, tempMp3Path, tempCoverPath);
		File.Delete(tempCoverPath);

		Console.WriteLine($"----> Moving to {mp3Path}");
		Directory.CreateDirectory(Path.GetDirectoryName(mp3Path));
		File.Move(tempMp3Path, mp3Path);

		Console.WriteLine();
	}

	void SetTags(FeedItem item, string mp3Path, string coverPath)
	{
		Console.WriteLine("----> Setting tags");
		var tagFile = TagLib.File.Create(mp3Path);
		tagFile.RemoveTags(TagLib.TagTypes.Id3v1);
		var tag = (Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, create: true);
		tag.Title = item.Title;
		tag.Album = item.Title;
		tag.Performers = new[] { item.Artist };
		tag.AlbumArtists = new[] { item.Artist };
		tag.Year = (uint)item.PublishedDateTime.Year;
		tag.Description = item.Description;
		tag.SetTextFrame("WOAF", item.PageUrl.ToString());
		tag.SetTextFrame("WOAS", item.PageUrl.ToString());
		tag.Pictures = new TagLib.IPicture[]
		{
			new TagLib.Picture(coverPath)
		};
		tagFile.Save();
	}

	async Task<string> DownloadToTempAsync(Uri url, string extension)
	{
		var tempFile = Path.Combine(
			Path.GetTempPath(), 
			Path.ChangeExtension(Guid.NewGuid().ToString(), extension)
		);
		//Console.WriteLine($"----> Writing {tempFile}");

		await using var downloadStream = await _client.GetStreamAsync(url);
		await using var fileStream = File.Create(tempFile);
		await downloadStream.CopyToAsync(fileStream);
		return tempFile;
	}

	string BuildPath(PodcastConfig config, string episodeTitle)
	{
		var dir = config.Directory;
		var episodeFileName = _invalidPathChars.Replace(episodeTitle, "");
		if (config.OneFolderPerEpisode)
		{
			dir = Path.Combine(dir, episodeFileName);
		}

		return Path.Combine(dir, episodeFileName + ".mp3");
	}
}
