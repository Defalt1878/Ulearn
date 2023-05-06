using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Ulearn.Core.GoogleSheet
{
	public static class RequestCreator
	{
		public static List<Request> GetRequests(GoogleSheetModel googleSheetModel, (int lastColumnIndex, int lastRowIndex)? sizes = null)
		{
			var requests = new List<Request>();
			var rowNumber = 0;
			foreach (var row in googleSheetModel.Cells)
			{
				var values = new List<CellData>();
				foreach (var cell in row)
					values.Add(CreateCellData(cell));

				if (sizes.HasValue && values.Count < sizes.Value.lastColumnIndex) //clear all unfilled cells
					values = values
						.Concat(CreateEmptyCells(sizes.Value.lastColumnIndex - values.Count))
						.ToList();

				requests.Add(CreateRowUpdateRequest(googleSheetModel.ListId, values, rowNumber));
				rowNumber++;
			}

			if (sizes.HasValue && googleSheetModel.Cells.Count < sizes.Value.lastRowIndex) //clear all unfilled rows
				for (var i = 0; i < Math.Max(sizes.Value.lastRowIndex - googleSheetModel.Cells.Count, 2); i++)
				{
					requests.Add(CreateRowUpdateRequest(googleSheetModel.ListId, CreateEmptyCells(sizes.Value.lastColumnIndex), rowNumber));
					rowNumber++;
				}

			return requests;
		}

		private static List<CellData> CreateEmptyCells(int count)
		{
			return Enumerable
				.Range(0, count)
				.Select(_ => CreateCellDataForString("", new CellData
				{
					UserEnteredValue = new ExtendedValue(),
					UserEnteredFormat = new CellFormat()
				}))
				.ToList();
		}

		private static Request CreateRowUpdateRequest(int listId, IList<CellData> values, int rowIndex, string fields = "*")
		{
			return new Request
			{
				UpdateCells = new UpdateCellsRequest
				{
					Start = new GridCoordinate
					{
						SheetId = listId,
						RowIndex = rowIndex
					},
					Rows = new List<RowData> { new() { Values = values } },
					Fields = fields
				}
			};
		}

		private static CellData CreateCellData(IGoogleSheetCell googleSheetCell)
		{
			var data = new CellData
			{
				UserEnteredValue = new ExtendedValue(),
				UserEnteredFormat = new CellFormat()
			};
			data = googleSheetCell switch
			{
				GoogleSheetCell<string> stringCell => CreateCellDataForString(stringCell.Value, data),
				GoogleSheetCell<DateTime> dateCell => CreateCellDataForDate(dateCell.Value, data),
				GoogleSheetCell<double> numberCell => CreateCellDataForNumber(numberCell.Value, data),
				GoogleSheetCell<int> intCell => CreateCellDataForIntNumber(intCell.Value, data),
				_ => data
			};
			return data;
		}

		private static CellData CreateCellDataForDate(DateTime value, CellData cellData)
		{
			cellData.UserEnteredValue.NumberValue = GetDateValue(value);
			cellData.UserEnteredFormat.NumberFormat =
				new NumberFormat { Pattern = "dd.MM.yyyy HH:mm:ss \"UTC\"", Type = "DATE_TIME" };
			return cellData;
		}

		private static CellData CreateCellDataForNumber(double value, CellData cellData)
		{
			cellData.UserEnteredValue.NumberValue = value;
			cellData.UserEnteredFormat.NumberFormat = new NumberFormat { Pattern = "####0.0#", Type = "NUMBER" };
			return cellData;
		}

		private static CellData CreateCellDataForIntNumber(int value, CellData cellData)
		{
			cellData.UserEnteredValue.NumberValue = value;
			cellData.UserEnteredFormat.NumberFormat = new NumberFormat { Pattern = "####0", Type = "NUMBER" };
			return cellData;
		}

		private static CellData CreateCellDataForString(string value, CellData cellData)
		{
			cellData.UserEnteredValue.StringValue = value;
			return cellData;
		}

		private static double? GetDateValue(DateTime dateTimeInLocal)
		{
			var localZone = TimeZoneInfo.Local;
			var currentUtc = TimeZoneInfo.ConvertTimeToUtc(dateTimeInLocal, localZone);
			var startTime = new DateTime(1899, 12, 30, 0, 0, 0, 0, DateTimeKind.Utc);
			var tsInterval = currentUtc.Subtract(startTime);
			return tsInterval.TotalDays;
		}
	}
}