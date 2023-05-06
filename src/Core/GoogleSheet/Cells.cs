namespace Ulearn.Core.GoogleSheet
{
	public interface IGoogleSheetCell
	{
	}

	public class GoogleSheetCell<T> : IGoogleSheetCell
	{
		public readonly T Value;

		public GoogleSheetCell(T value)
		{
			Value = value;
		}
	}
}