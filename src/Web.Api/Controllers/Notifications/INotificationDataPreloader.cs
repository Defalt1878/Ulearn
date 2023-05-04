using System.Collections.Generic;
using System.Threading.Tasks;
using Database.Models;

namespace Ulearn.Web.Api.Controllers.Notifications
{
	public interface INotificationDataPreloader
	{
		Task<NotificationDataStorage> LoadAsync(IEnumerable<Notification> notifications);
	}
}