using System.Collections.Generic;

namespace Ulearn.Core.GoogleSheet
{
	public class GoogleSheetModel
	{
		public readonly List<List<IGoogleSheetCell>> Cells;
		public readonly int ListId;

		public GoogleSheetModel(int listId)
		{
			ListId = listId;
			Cells = new List<List<IGoogleSheetCell>> { new() };
		}

		public void GoToNewLine() => Cells.Add(new List<IGoogleSheetCell>());

		public void AddCell<T>(int row, T value) => Cells[row].Add(new GoogleSheetCell<T>(value));
	}
}