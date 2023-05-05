using System.ComponentModel.DataAnnotations;

namespace Ulearn.Common.Api.Models.Validations;

public class NotEmptyAttribute : ValidationAttribute
{
	public bool CanBeNull { get; set; }

	public override bool IsValid(object value)
	{
		var stringValue = value as string;
		if (CanBeNull && value == null)
			return true;
		return !string.IsNullOrEmpty(stringValue);
	}
}