namespace AntiPlagiarism.Web.CodeAnalyzing.Hashers;

public interface ISequenceHasher
{
	void Enqueue(object obj);
	void Dequeue();
	int GetCurrentHash();
	void Reset();
}