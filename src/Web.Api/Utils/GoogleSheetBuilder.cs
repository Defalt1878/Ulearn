using System;
using OfficeOpenXml.Style;
using Ulearn.Core.GoogleSheet;

namespace Ulearn.Web.Api.Utils
{
	public class GoogleSheetBuilder : ISheetBuilder
	{
		private readonly GoogleSheetModel googleSheetModel;

		// private readonly List<Action<ExcelStyle>> styleRules = new List<Action<ExcelStyle>>();

		public int ColumnsCount;

		private int currentColumn;
		private int currentRow;

		public GoogleSheetBuilder(GoogleSheetModel googleSheetModel)
		{
			this.googleSheetModel = googleSheetModel;
			currentRow = 0;
			currentColumn = 0;
			ColumnsCount = 0;
		}

		public void AddCell<T>(T value, int colspan = 1)
		{
			if (colspan < 1)
				return;
			googleSheetModel.AddCell(currentRow, value);
			for (var i = 1; i < colspan; i++)
				googleSheetModel.AddCell(currentRow, "");

			currentColumn += colspan;
			ColumnsCount = Math.Max(ColumnsCount, currentColumn);
		}

		public void GoToNewLine()
		{
			googleSheetModel.GoToNewLine();
			currentRow += 1;
			currentColumn = 0;
		}

		public void AddStyleRule(Action<ExcelStyle> styleFunction)
		{
		}

		public void PopStyleRule()
		{
		}

		public void AddStyleRuleForOneCell(Action<ExcelStyle> styleFunction)
		{
		}

		public GoogleSheetModel Build() => googleSheetModel;
	}
}