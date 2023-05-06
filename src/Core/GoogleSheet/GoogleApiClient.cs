using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Ulearn.Core.GoogleSheet
{
	public class GoogleApiClient
	{
		public static readonly Regex urlRegex = new("https://docs.google.com/spreadsheets/d/(.+)/edit#gid=(.+)", RegexOptions.Compiled);
		private readonly SheetsService service;

		public GoogleApiClient(string credentials)
		{
			service = new SheetsService(new BaseClientService.Initializer
			{
				HttpClientInitializer = GoogleCredential.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(credentials))).CreateScoped(SheetsService.Scope.Spreadsheets)
			});
		}

		public void FillSpreadSheet(string spreadsheetId, GoogleSheetModel googleSheetModel)
		{
			Update(spreadsheetId, googleSheetModel);

			var requests = RequestCreator.GetRequests(googleSheetModel);

			service.Spreadsheets
				.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
				.Execute();
		}


		public async Task<GSheet> GetSheetByUrl(string url)
		{
			var match = urlRegex.Match(url);
			var spreadsheetId = match.Groups[1].Value;
			var sheetId = int.Parse(match.Groups[2].Value);
			return await GetSpreadsheet(spreadsheetId).GetSheetById(sheetId);
		}

		public GSpreadsheet GetSpreadsheet(string spreadsheetId) => new(spreadsheetId, service);

		public async Task<List<GSheet>> GetSheets(string spreadsheetId)
		{
			try
			{
				var metadata = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
				var sheets = metadata.Sheets.Select(x =>
					new GSheet(spreadsheetId, x.Properties.Title, x.Properties.SheetId ?? 0, service));
				return sheets.ToList();
			}
			catch (Exception e)
			{
				throw new Exception($"Can't get sheets of {spreadsheetId}", e);
			}
		}

		private void Update(string spreadsheetId, GoogleSheetModel googleSheetModel, bool removeUnused = false)
		{
			var spreadsheet = service.Spreadsheets.Get(spreadsheetId).Execute();
			var listId = googleSheetModel.ListId;
			var sheet = spreadsheet.Sheets.FirstOrDefault(e => e.Properties.SheetId == googleSheetModel.ListId);

			if (sheet == null) throw new ArgumentException($"No sheets in spreadsheet {spreadsheetId}");

			var width = googleSheetModel.Cells.Max(r => r.Count);
			var height = googleSheetModel.Cells.Count;
			var oldWidth = sheet.Properties.GridProperties.ColumnCount - 1 ?? width;
			var oldHeight = sheet.Properties.GridProperties.RowCount - 1 ?? height;

			var requests = new List<Request>();

			if (width - oldWidth > 0)
				requests.Add(CreateInsertDimensionRequest("COLUMNS", listId, oldWidth, width));
			if (removeUnused && width - oldWidth < 0)
				requests.Add(CreateDeleteDimensionRequest("COLUMNS", listId, width, oldWidth));

			if (height - oldHeight > 0)
				requests.Add(CreateInsertDimensionRequest("ROWS", listId, oldHeight, height));
			if (removeUnused && height - oldHeight < 0)
				requests.Add(CreateDeleteDimensionRequest("ROWS", listId, height, oldHeight));

			if (requests.Count > 0)
				service.Spreadsheets
					.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, spreadsheetId)
					.Execute();
		}

		private static Request CreateInsertDimensionRequest(string dimensionName, int sheetId, int startIndex, int endIndex) => new()
		{
			InsertDimension = new InsertDimensionRequest
			{
				Range = new DimensionRange
				{
					Dimension = dimensionName,
					StartIndex = startIndex,
					EndIndex = endIndex,
					SheetId = sheetId
				}
			}
		};

		private static Request CreateDeleteDimensionRequest(string dimensionName, int sheetId, int startIndex, int endIndex) => new()
		{
			DeleteDimension = new DeleteDimensionRequest
			{
				Range = new DimensionRange
				{
					Dimension = dimensionName,
					StartIndex = startIndex,
					EndIndex = endIndex,
					SheetId = sheetId
				}
			}
		};
	}

	public class GSheet
	{
		public GSheet(string spreadsheetId, string sheetName, int sheetId, SheetsService sheetsService)
		{
			SpreadsheetId = spreadsheetId;
			SheetName = sheetName;
			SheetId = sheetId;
			SheetsService = sheetsService;
		}

		public readonly string SpreadsheetId;
		public readonly string SheetName;
		public readonly int SheetId;
		public readonly SheetsService SheetsService;

		public string ReadCell(ValueTuple<int, int> cellCoords)
		{
			return Read(cellCoords);
		}

		public List<List<string>> ReadRange((int top, int left) rangeStart, (int top, int left) rangeEnd)
		{
			var (top, left) = rangeStart;
			var (bottom, right) = rangeEnd;
			left++;
			top++;
			right++;
			bottom++;
			var range = $"R{top}C{left}:R{bottom}C{right}";
			return ReadRange(range);
		}

		public List<List<string>> ReadRange(string range)
		{
			var fullRange = $"{SheetName}!{range}";
			var request = SheetsService.Spreadsheets.Values.Get(SpreadsheetId, fullRange);
			var response = request.Execute();
			var values = response.Values ?? new List<IList<object>>();
			var res = values.Select(l => l?.Select(o => o?.ToString() ?? "").ToList() ?? new List<string>()).ToList();
			return res;
		}

		public void ClearRange(string sheetName, (int top, int left) rangeStart, (int top, int left) rangeEnd)
		{
			var (top, left) = rangeStart;
			var (bottom, right) = rangeEnd;
			left++;
			top++;
			right++;
			bottom++;
			var range = $"R{top}C{left}:R{bottom}C{right}";
			var fullRange = $"{sheetName}!{range}";
			var requestBody = new ClearValuesRequest();
			var deleteRequest = SheetsService.Spreadsheets.Values.Clear(requestBody, SpreadsheetId, fullRange);
			deleteRequest.Execute();
		}

		private string Read((int top, int left) rangeStart)
		{
			var (top, left) = rangeStart;
			left++;
			top++;
			var range = $"R{top}C{left}";
			var values = ReadRange(range);
			return values.First().First();
		}
	}
}