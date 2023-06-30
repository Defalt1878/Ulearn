using System.Collections.Generic;
using System.Threading.Tasks;
using Database.Models;
using JetBrains.Annotations;

namespace Database.Repos.Users
{
	public interface IUsersRepo
	{
		[ItemCanBeNull]
		Task<ApplicationUser> FindUserById(string userId);
		Task<bool> IsUserExist(string userId);
		Task<List<string>> GetSysAdminsIds();
		Task ChangeTelegram(string userId, long? chatId, string chatTitle);
		Task ConfirmEmail(string userId, bool isConfirmed = true);
		Task UpdateLastConfirmationEmailTime(ApplicationUser user);
		Task ChangeEmail(ApplicationUser user, string email);
		Task<ApplicationUser> GetUlearnBotUser();
		Task<string> GetUlearnBotUserId();
		Task CreateUlearnBotUserIfNotExists();
		Task<List<ApplicationUser>> FindUsersByUsernameOrEmail(string usernameOrEmail);
		Task<List<ApplicationUser>> GetUsersByIds(IEnumerable<string> usersIds);
		Task DeleteUserAsync(ApplicationUser user);
		bool IsSystemAdministrator(ApplicationUser user);
		Task<bool> IsSystemAdministrator(string userId);
		Task<List<string>> GetUserIdsWithLmsRole(LmsRoleType lmsRole);
		Task<List<string>> FindUsersBySocialProviderKey(string providerKey);
		Task<List<ApplicationUser>> FindUsersByConfirmedEmails(IEnumerable<string> emails);
		Task<List<ApplicationUser>> FindUsersByConfirmedEmail(string email);
	}
}