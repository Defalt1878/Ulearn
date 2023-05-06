using System.Runtime.Serialization;

namespace Ulearn.Core.RunCheckerJobApi
{
	[DataContract]
	public class StyleError
	{
		/// <summary>
		/// id типа ошибки
		/// </summary>
		[DataMember]
		public string ErrorType;

		[DataMember]
		public StyleErrorSpan Span;

		/// <summary>
		/// Текст, показываемый пользователю
		/// </summary>
		[DataMember]
		public string Message;
	}

	[DataContract]
	public class StyleErrorSpan
	{
		/// <summary>
		/// Позиция первого символа ошибочного кода в строке
		/// </summary>
		[DataMember]
		public StyleErrorSpanPosition StartLinePosition;

		/// <summary>
		/// Позиция последнего символа ошибочного кода в строке
		/// </summary>
		[DataMember]
		public StyleErrorSpanPosition EndLinePosition;
	}

	[DataContract]
	public class StyleErrorSpanPosition
	{
		/// <summary>
		/// Номер строки с нуля
		/// </summary>
		[DataMember]
		public int Line;

		/// <summary>
		/// Номер символа в строке с нуля
		/// </summary>
		[DataMember]
		public int Character;
	}
}