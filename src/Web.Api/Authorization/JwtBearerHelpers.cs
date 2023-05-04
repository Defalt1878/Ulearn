using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ulearn.Web.Api.Authorization;

public static class JwtBearerHelpers
{
	public static SymmetricSecurityKey CreateSymmetricSecurityKey(string secret)
	{
		return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
	}
}