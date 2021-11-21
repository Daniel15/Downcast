namespace Downcast;

record PodcastConfig
{
	public string Name { get; init; } = default!;

	public Uri Url { get; init; } = default!;

	public string Directory { get; init; } = default!;

	public bool OneFolderPerEpisode { get; init; } = true;
}
