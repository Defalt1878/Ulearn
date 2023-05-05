using System.Security.Claims;

namespace Ulearn.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
	public static string GetUserId(this ClaimsPrincipal principal)
	{
		return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		//throw new ArgumentNullException(nameof(principal));
	}
}