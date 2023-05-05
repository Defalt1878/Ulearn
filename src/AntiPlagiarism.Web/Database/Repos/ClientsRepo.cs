using System;
using System.Threading.Tasks;
using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiPlagiarism.Web.Database.Repos;

public interface IClientsRepo
{
	Task<Client> FindClientByTokenAsync(Guid token);
}

public class ClientsRepo : IClientsRepo
{
	private readonly AntiPlagiarismDb db;

	public ClientsRepo(AntiPlagiarismDb db)
	{
		this.db = db;
	}

	public async Task<Client> FindClientByTokenAsync(Guid token)
	{
		return await db.Clients.FirstOrDefaultAsync(c => c.Token == token && c.IsEnabled).ConfigureAwait(false);
	}
}