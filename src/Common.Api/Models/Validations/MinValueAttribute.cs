using System.ComponentModel.DataAnnotations;

namespace Ulearn.Common.Api.Models.Validations;

public class MinValueAttribute : ValidationAttribute
{
	private readonly int minValue;

	public MinValueAttribute(int minValue)
	{
		this.minValue = minValue;
	}

	public override bool IsValid(object value)
	{
		return (int)value >= minValue;
	}
}