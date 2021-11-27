namespace Downcast.Extensions;

static class StringExtensions
{
	public static string StripPrefix(this string str, string prefix)
	{
		if (str.StartsWith(prefix))
		{
			str = str.Substring(prefix.Length + 1);
		}
		return str;
	}
}
