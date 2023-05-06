using System;

namespace Ulearn.Core
{
	public static class GitUtils
	{
		public static string RepoUrlToCommitLink(string repoUrl, string hash)
		{
			return RepoUrlToRepoLink(repoUrl) + $"/commit/{hash}";
		}

		public static string GetSlideEditLink(string repoUrl, string branch, string courseXmlPath, string filePathRelativeCourseXml)
		{
			var unsafeUrl = RepoUrlToRepoLink(repoUrl) + "/edit/" + branch + "/" + (courseXmlPath + "/" + filePathRelativeCourseXml).Replace('\\', '/');
			return Uri.EscapeDataString(unsafeUrl);
		}

		private static string RepoUrlToRepoLink(string repoUrl)
		{
			var link = repoUrl[4..];
			link = link[..^4];
			link = link.Replace(':', '/');
			return "https://" + link;
		}
	}
}