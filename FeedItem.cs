using Downcast.Extensions;

namespace Downcast;

/// <summary>
/// Represents an item from a feed
/// </summary>
/// <param name="Title">The title of the mix.</param>
/// <param name="Description">A short description of the mix.</param>
/// <param name="Artist">The artist.</param>
/// <param name="PublishedDateTime">The date/time it was published.</param>
/// <param name="PageUrl">A HTML page that describes it.</param>
/// <param name="ImageUrl">An image URL (PNG or JPEG) that can be used as an album cover photo.</param>
/// <param name="Mp3Url">A direct link to an MP3 file, if available.</param>
/// <param name="FileExtension">The file extension to use on disk.</param>
record FeedItem(
	string Title,
	string Description,
	string Artist,
	DateTime PublishedDateTime,
	Uri PageUrl,
	Uri? ImageUrl,
	Uri Mp3Url,
	string FileExtension
)
{
	/// <summary>
	/// Cleans up the title. Currently just removes the artist name.
	/// </summary>
	public string CleanTitle => Title.StripPrefix(Artist + " - ");
}
