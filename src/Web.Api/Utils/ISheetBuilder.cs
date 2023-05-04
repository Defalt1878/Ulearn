using System;
using OfficeOpenXml.Style;

namespace Ulearn.Web.Api.Utils;

public interface ISheetBuilder
{
	void AddCell<T>(T value, int colspan = 1);
	void GoToNewLine();
	void AddStyleRule(Action<ExcelStyle> styleFunction);
	void PopStyleRule();
	void AddStyleRuleForOneCell(Action<ExcelStyle> styleFunction);
}