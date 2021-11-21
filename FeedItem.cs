using System.Xml.Linq;
using System.Xml.XPath;

namespace Downcast;

record FeedItem(
	string Title,
	string Description,
	string Artist,
	DateTime PublishedDateTime,
	Uri PageUrl,
	Uri ImageUrl,
	Uri Mp3Url
)
{
	public static FeedItem FromXml(XElement rawItem)
	{
		XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
		return new FeedItem(
			Title: rawItem.Element("title")!.Value,
			Description: rawItem.Element("description")!.Value,
			Artist: rawItem.Element(itunes + "author")!.Value,
			PublishedDateTime: DateTime.Parse(rawItem.Element("pubDate")!.Value),
			PageUrl: new Uri(rawItem.Element("link")!.Value),
			ImageUrl: new Uri(rawItem.Element(itunes + "image")!.Attribute("href")!.Value),
			Mp3Url: new Uri(rawItem
				.XPathSelectElement("//enclosure[@type='audio/mpeg']")
				!.Attribute("url")
				!.Value)
		);
	}
}
