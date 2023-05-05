namespace AntiPlagiarism.Web.CodeAnalyzing.Hashers;

public class DefaultObjectHasher : IObjectHasher
{
	public int GetHashCode(object o)
	{
		return o.GetHashCode();
	}
}