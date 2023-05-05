namespace Ulearn.Common.Extensions;

public static class IntExtensions
{
	public static int PercentsOf(this int part, int total)
	{
		if (total == 0)
			return 0;
		return part * 100 / total;
	}
}