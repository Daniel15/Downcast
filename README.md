# Downcast

This is a downloader for podcast-style feeds, for example from Soundcloud or iTunes. It sorts them into a directory structure suitable for use with Plex (one folder per episode).

# Usage

1. Install .NET 6.0
2. Modify `appsettings.json` to contain the correct URLs. Use https://getrssfeed.com/ to find the RSS URL for a Soundcloud account
3. Run `dotnet run --dry-run` to see what would be downloaded
4. Run `dotnet run` to actually download the files