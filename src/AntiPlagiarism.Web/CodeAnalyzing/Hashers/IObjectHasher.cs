namespace AntiPlagiarism.Web.CodeAnalyzing.Hashers;

public interface IObjectHasher
{
	int GetHashCode(object o);
}