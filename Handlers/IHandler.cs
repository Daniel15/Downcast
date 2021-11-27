namespace Downcast.Handlers;

/// <summary>
/// Represents a handler for a feed
/// </summary>
interface IHandler
{
	/// <summary>
	/// Parses the feed into a list of feed items
	/// </summary>
	/// <param name="config">The feed config</param>
	/// <returns>The feed items</returns>
	public Task<IEnumerable<FeedItem>> ParseFeed(PodcastConfig config);

	/// <summary>
	/// Downloads a feed item into a temporary directory
	/// </summary>
	/// <param name="item">The feed item</param>
	/// <returns></returns>
	public Task<string> DownloadToTempAsync(FeedItem item);
}
