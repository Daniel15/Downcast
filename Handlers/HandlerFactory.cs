using Microsoft.Extensions.DependencyInjection;

namespace Downcast.Handlers;

class HandlerFactory
{
	private readonly IServiceProvider _services;

	public HandlerFactory(IServiceProvider services)
	{
		_services = services;
	}

	/// <summary>
	/// Creates a handler that can handle the specified config.
	/// </summary>
	/// <param name="config">The config to create a handler for</param>
	/// <returns>The handler</returns>
	public IHandler Create(PodcastConfig config)
	{
		var type = typeof(RssHandler);

		if (config.Url.Host.EndsWith("mixcloud.com"))
		{
			type = typeof(MixcloudHandler);
		}

		return (IHandler)ActivatorUtilities.CreateInstance(_services, type);
	}
}
