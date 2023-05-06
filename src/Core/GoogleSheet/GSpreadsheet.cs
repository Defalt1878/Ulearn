using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Ulearn.Core.GoogleSheet;

public class GSpreadsheet
{
	public readonly string SpreadsheetId;
	public readonly SheetsService SheetsService;

	public GSpreadsheet(string spreadsheetId, SheetsService sheetsService)
	{
		SpreadsheetId = spreadsheetId;
		SheetsService = sheetsService;
	}

	public async Task<List<GSheet>> GetSheets()
	{
		try
		{
			var metadata = await SheetsService.Spreadsheets.Get(SpreadsheetId).ExecuteAsync();
			var sheets = metadata.Sheets.Select(x =>
				new GSheet(SpreadsheetId, x.Properties.Title, x.Properties.SheetId ?? 0, SheetsService));
			return sheets.ToList();
		}
		catch (Exception e)
		{
			throw new Exception($"Can't get sheets of {SpreadsheetId}", e);
		}
	}

	public async Task<GSheet> GetSheetById(int sheetId)
	{
		return (await GetSheets()).First(s => s.SheetId == sheetId);
	}

	public async Task<GSheet> GetSheetByName(string sheetName)
	{
		return (await GetSheets()).First(s => s.SheetName == sheetName);
	}

	public async Task CreateNewSheet(string title)
	{
		var requests = new List<Request>
		{
			new()
			{
				AddSheet = new AddSheetRequest
				{
					Properties = new SheetProperties
					{
						Title = title,
						TabColor = new Color { Red = 1 }
					}
				}
			}
		};
		var requestBody = new BatchUpdateSpreadsheetRequest { Requests = requests };
		var request = SheetsService.Spreadsheets.BatchUpdate(requestBody, SpreadsheetId);
		await request.ExecuteAsync();
	}
}