using System.Text.RegularExpressions;
using Downcast.Extensions;
using Downcast.Handlers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TagLib;
using File = System.IO.File;

namespace Downcast;

class Program
{
	private static readonly Regex _invalidPathChars = new("[^a-zA-Z0-9_ ]+", RegexOptions.Compiled);
	private readonly IConfiguration _config;
	private readonly HandlerFactory _handlerFactory;
	private readonly HttpClient _client;

	[Option(Description = "Just output what would be done, without actually doing it")]
	public bool DryRun { get; } = false;

	public static Task<int> Main(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureServices(services =>
			{
				services.AddSingleton<HandlerFactory>();
				services.AddSingleton(new HttpClient
				{
					DefaultRequestHeaders =
					{
						{"User-Agent", "Downcast/1.0 (https://d.sb/downcast)"},
					},
				});
			})
			.RunCommandLineApplicationAsync<Program>(args);
	}

	public Program(IConfiguration config, HandlerFactory handlerFactory, HttpClient client)
	{
		_config = config;
		_handlerFactory = handlerFactory;
		_client = client;
	}

	// ReSharper disable once UnusedMember.Local
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
		var handler = _handlerFactory.Create(config);
		foreach (var item in await handler.ParseFeed(config))
		{
			await ProcessItemAsync(config, item, handler);
		}
	}

	async Task ProcessItemAsync(PodcastConfig config, FeedItem item, IHandler handler)
	{
		var mp3Path = BuildPath(config, item);
		var alreadyExists = File.Exists(mp3Path);

		Console.WriteLine($"{item.CleanTitle} by {item.Artist}");

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
		// TODO: Handle if ImageUrl is null
		var tempMp3PathTask = handler.DownloadToTempAsync(item);
		var tempCoverPathTask = _client.DownloadToTempAsync(
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
		tagFile.RemoveTags(TagTypes.Id3v1);

		var tagType = tagFile switch
		{
			TagLib.Mpeg4.File => TagTypes.Apple,
			TagLib.Mpeg.AudioFile => TagTypes.Id3v2,
			_ => throw new Exception($"Unsupported file type {tagFile.GetType().Name}")
		};

		var tag = tagFile.GetTag(tagType, create: true);
		tag.Album = item.CleanTitle;
		tag.AlbumArtists = new[] { item.Artist };
		tag.Description = item.Description;
		if (tag.Genres.Length == 1 && tag.Genres[0] == "Blues")
		{
			tag.Genres = Array.Empty<string>();
		}
		tag.Performers = new[] { item.Artist };
		tag.Pictures = new IPicture[]
		{
			new Picture(coverPath)
		};
		tag.Title = item.CleanTitle;
		tag.Year = (uint)item.PublishedDateTime.Year;

		if (tag is TagLib.Id3v2.Tag id3Tag)
		{
			id3Tag.SetTextFrame("TDAT", item.PublishedDateTime.ToString("ddMM"));
			id3Tag.SetTextFrame("TIME", item.PublishedDateTime.ToString("HHmm"));
			id3Tag.SetTextFrame("WOAF", item.PageUrl.ToString());
			id3Tag.SetTextFrame("WOAS", item.PageUrl.ToString());
		}

		if (tag is TagLib.Mpeg4.AppleTag appleTag)
		{
			appleTag.SetText("day", item.PublishedDateTime.ToString("yyyy-MM-dd"));
		}

		tagFile.Save();
	}

	string BuildPath(PodcastConfig config, FeedItem item)
	{
		var dir = config.Directory;
		var episodeFileName = _invalidPathChars.Replace(item.CleanTitle, "");
		if (config.OneFolderPerEpisode)
		{
			dir = Path.Combine(dir, episodeFileName);
		}

		return Path.Combine(dir, episodeFileName + "." + item.FileExtension);
	}
}
