using System.ComponentModel.DataAnnotations;

namespace Ulearn.Common.Api.Models.Validations;

public class MaxValueAttribute : ValidationAttribute
{
	private readonly int maxValue;

	public MaxValueAttribute(int maxValue)
	{
		this.maxValue = maxValue;
	}

	public override bool IsValid(object value)
	{
		return (int)value <= maxValue;
	}
}