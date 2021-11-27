using System.Xml.Linq;
using System.Xml.XPath;
using Downcast.Extensions;

namespace Downcast.Handlers;

class RssHandler : IHandler
{
	private readonly HttpClient _client;

	private static readonly XNamespace itunesNs = "http://www.itunes.com/dtds/podcast-1.0.dtd";

	public RssHandler(HttpClient client)
	{
		_client = client;
	}

	public async Task<IEnumerable<FeedItem>> ParseFeed(PodcastConfig config)
	{
		await using var stream = await _client.GetStreamAsync(config.Url);
		var xml = XElement.Load(stream);

		return xml.Descendants("item").Select(rawItem => new FeedItem(
			Title: rawItem.Element("title")!.Value,
			Description: rawItem.Element("description")!.Value,
			Artist: rawItem.Element(itunesNs + "author")!.Value,
			PublishedDateTime: DateTime.Parse(rawItem.Element("pubDate")!.Value),
			PageUrl: new Uri(rawItem.Element("link")!.Value),
			ImageUrl: new Uri(rawItem.Element(itunesNs + "image")!.Attribute("href")!.Value),
			Mp3Url: new Uri(rawItem
				.XPathSelectElement("//enclosure[@type='audio/mpeg']")
				!.Attribute("url")
				!.Value),
			FileExtension: "mp3"
		));
	}

	public async Task<string> DownloadToTempAsync(FeedItem item)
	{
		return await _client.DownloadToTempAsync(item.Mp3Url, item.FileExtension);
	}
}
