using AntiPlagiarism.Web.Configuration;
using Ulearn.Core.Configuration;

namespace AntiPlagiarism.UpdateDb.Configuration;

public class AntiPlagiarismUpdateDbConfiguration : UlearnConfigurationBase
{
	public string Database { get; set; }

	public HostLogConfiguration HostLog { get; set; }

	public string GraphiteServiceName { get; set; }

	public AntiPlagiarismConfigurationContent AntiPlagiarism { get; set; }
}