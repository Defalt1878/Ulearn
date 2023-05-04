using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using Database.Repos.Groups;
using Microsoft.Extensions.Options;
using Ulearn.Core.GoogleSheet;
using Ulearn.Web.Api.Utils.SuperGroup;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Utils;

public class SuperGroupManager
{
	private readonly SuperGroupGoogleSheetCache cache;
	private readonly string googleAccessCredentials;
	private readonly IGroupsRepo groupsRepo;

	public SuperGroupManager(IOptions<WebApiConfiguration> options, IGroupsRepo groupsRepo, SuperGroupGoogleSheetCache cache)
	{
		this.groupsRepo = groupsRepo;
		this.cache = cache;
		googleAccessCredentials = options.Value.GoogleAccessCredentials;
	}

	private async Task<(string groupName, string studentName)[]> GetSpreadSheetGroups(string spreadsheetUrl)
	{
		if (googleAccessCredentials is null)
			throw new ConfigurationErrorsException("GoogleAccessCredentials are null");

		var client = new GoogleApiClient(googleAccessCredentials);
		var spreadSheet = await client.GetSheetByUrl(spreadsheetUrl);
		var range = spreadSheet.ReadRange("A:B");

		var unFilledRows = range
			.Select((p, index) => p.Count != 2 || p.Any(s => s == "")
				? new { columns = p, rawIndex = index }
				: null)
			.Where(p => p is not null)
			.ToList();
		if (unFilledRows.Count > 0)
			throw new GoogleSheetFormatException { RawsIndexes = unFilledRows.Select(r => r.rawIndex).ToList() };

		return range
			.Select(pair => (StringUtils.RemoveSpacesDuplicates(pair[1]), StringUtils.RemoveSpacesDuplicates(pair[0])))
			.ToArray();
	}

	/// <summary>
	///     Finding namesake users in groups
	/// </summary>
	/// <param name="groups"></param>
	/// <returns></returns>
	public static Dictionary<string, string[]> GetGroupsByUserName(IEnumerable<(string groupName, string studentName)> groups)
	{
		return groups
			.GroupBy(g => g.studentName)
			.Where(g => g
				.Select(d => d.groupName)
				.Distinct()
				.Count() > 1)
			.ToDictionary(
				g => g.Key,
				g => g
					.Select(d => d.groupName)
					.Distinct()
					.ToArray());
	}

	public async Task<
		(List<SingleGroup> createdGroups,
		(string groupName, string studentName)[] spreadSheetGroups)
	> GetSheetGroupsAndCreatedGroups(string spreadsheetUrl, int superGroupId, bool useCache = true)
	{
		var cached = cache.TryGet(spreadsheetUrl, out var cachedSpreadSheetGroups);
		if (useCache && cached)
			return (await groupsRepo.FindGroupsBySuperGroupIdAsync(superGroupId, true), cachedSpreadSheetGroups);

		var spreadSheetGroupsTask = GetSpreadSheetGroups(spreadsheetUrl);
		var createdGroupsTask = groupsRepo.FindGroupsBySuperGroupIdAsync(superGroupId, true);

		await Task.WhenAll(spreadSheetGroupsTask, createdGroupsTask);

		var spreadSheetGroups = await spreadSheetGroupsTask;
		var createdGroups = await createdGroupsTask;

		if (cached)
			cache.Replace(spreadsheetUrl, spreadSheetGroups);
		else
			cache.Add(spreadsheetUrl, spreadSheetGroups);

		return (createdGroups, spreadSheetGroups);
	}
}