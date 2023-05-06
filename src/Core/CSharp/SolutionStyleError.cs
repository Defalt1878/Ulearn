using System;
using Microsoft.CodeAnalysis;
using Ulearn.Common.Extensions;
using SyntaxNodeOrToken = Ulearn.Core.CSharp.Validators.IndentsValidation.SyntaxNodeOrToken;

namespace Ulearn.Core.CSharp
{
	public class SolutionStyleError
	{
		public readonly StyleErrorType ErrorType;
		public readonly FileLinePositionSpan Span;
		public readonly string Message;

		public SolutionStyleError(StyleErrorType errorType, FileLinePositionSpan span, params object[] arguments)
		{
			ErrorType = errorType;
			Span = span;
			Message = string.Format(errorType.GetTemplate(), arguments);
		}

		public SolutionStyleError(StyleErrorType errorType, SyntaxNode node, params object[] arguments)
			: this(errorType, GetFileLinePositionSpan(node), arguments)
		{
		}

		public SolutionStyleError(StyleErrorType errorType, SyntaxToken token, params object[] arguments)
			: this(errorType, GetFileLinePositionSpan(token), arguments)
		{
		}

		public SolutionStyleError(StyleErrorType errorType, SyntaxNodeOrToken nodeOrToken, params object[] arguments)
			: this(errorType, nodeOrToken.GetFileLinePositionSpan(), arguments)
		{
		}

		private static FileLinePositionSpan GetFileLinePositionSpan(SyntaxNode node)
		{
			return node.SyntaxTree.GetLineSpan(node.Span);
		}

		private static FileLinePositionSpan GetFileLinePositionSpan(SyntaxToken token)
		{
			return token.SyntaxTree!.GetLineSpan(token.Span);
		}

		public override string ToString()
		{
			return $"StyleError({ErrorType}. {Message} on {Span})";
		}

		public string GetMessageWithPositions()
		{
			var positionInfo = Span.StartLinePosition.Line == Span.EndLinePosition.Line
				? $"Строка {Span.StartLinePosition.Line + 1}, позиция {Span.StartLinePosition.Character}"
				: $"Строки {Span.StartLinePosition.Line + 1}—{Span.EndLinePosition.Line + 1}";
			return $"{positionInfo}: {Message}";
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((SolutionStyleError)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				// ReSharper disable once ImpureMethodCallOnReadonlyValueField
				return (Span.GetHashCode() * 397) ^ (Message != null ? Message.GetHashCode() : 0);
			}
		}

		private bool Equals(SolutionStyleError other)
		{
			return Equals(Span, other.Span) && string.Equals(Message, other.Message);
		}
	}

	public enum StyleErrorType
	{
		[MessageTemplate("Содержимое парных фигурных скобок ({0}) должно иметь дополнительный отступ.")]
		Indents01,

		[MessageTemplate("Содержимое парных фигурных скобок ({0}) должно иметь одинаковый отступ.")]
		Indents02,

		[MessageTemplate("Парные фигурные скобки ({0}) должны иметь одинаковый отступ.")]
		Indents03,

		[MessageTemplate("Парные фигурные скобки ({0}) должны иметь отступ не меньше, чем у родителя.")]
		Indents04,

		[MessageTemplate("Перед закрывающей фигурной скобкой на той же строке не должно быть кода.")]
		Indents05,

		[MessageTemplate("На верхнем уровне все объявления и инструкции должны иметь одинаковый отступ.")]
		Indents06,

		[MessageTemplate("Добавьте, пожалуйста, дополнительный перенос строки")]
		Indents07,

		[MessageTemplate("Выражение имеет слишком маленький отступ по сравнению с родительским блоком")]
		Indents08,

		[MessageTemplate("Выражение имеет слишком маленький отступ по сравнению с родительским блоком. Рекомендуется ставить 4 пробела")]
		Indents09,

		[MessageTemplate("Не рекомендуется оставлять лишние пустые строки. Пожалуйста, уберите их")]
		Indents10,

		[MessageTemplate("Рекомендуется выровнять таким же отступом, как и родительский блок")]
		Indents11,

		[MessageTemplate("После открывающей фигурной скобки на той же строке не должно быть кода.")]
		Indents12,


		[MessageTemplate("В названии метода отсутствует глагол.")]
		VerbInMethod01,


		[MessageTemplate("Неэффективный код. GetLength вызывается в цикле для константы и возвращает каждый раз одно и то же. Лучше вынести результат выполнения метода в переменную за цикл.")]
		ArrayLength01,


		[MessageTemplate("Слишком длинный блок инструкций. Попытайтесь разбить его на вспомогательные методы.")]
		BlockLength01,


		[MessageTemplate("Ненужное сравнение с переменной типа `bool`. Вместо `x == true` лучше писать просто `x`, а вместо `x != true` лучше писать `!x`.")]
		BoolCompare01,

		[MessageTemplate("Не следует выделять возвращаемое выражение скобками.")]
		Brackets01,


		[MessageTemplate("После открывающей скобки не должно быть лишнего переноса строки.")]
		ExcessLines01,

		[MessageTemplate("Перед закрывающей скобкой не должно быть лишнего переноса строки.")]
		ExcessLines02,

		[MessageTemplate("Между объявлением и открывающей скобкой не должно быть лишнего переноса строки.")]
		ExcessLines03,

		[MessageTemplate("После закрывающей скобки нужно оставить одну пустую строку.")]
		ExcessLines04,


		[MessageTemplate("Неэффективный код. Если число нужно возвести в квадрат или куб, лучше сделать это с помощью умножения, не используя более общий, но менее быстрый `Math.Pow`.")]
		Exponentiation01,


		[MessageTemplate("Решение должно быть корректным определением статического метода.")]
		Static01,

		[MessageTemplate("Решение должно состоять ровно из одного метода.")]
		Static02,


		[MessageTemplate("Решение этой задачи должно быть в одно выражение 'return ...'")]
		Single01,


		[MessageTemplate("Слишком длинная строка. Не заставляйте людей использовать горизонтальный скролл")]
		LineLength01,


		[MessageTemplate("Имя должно начинаться с маленькой буквы.")]
		NamingCase01,

		[MessageTemplate("Имя должно начинаться с большой буквы.")]
		NamingCase02,


		[MessageTemplate("`Get`-метод без возвращаемого значения — это бессмыслица.")]
		NamingStyle01,

		[MessageTemplate("`Set`-метод без аргументов — это бессмыслица.")]
		NamingStyle02,

		[MessageTemplate("Называйте методы простыми глаголами! Например, `Move`, а не `Moving`.")]
		NamingStyle03,

		[MessageTemplate("Называйте методы глаголами! Например, `Convert`, а не `Conversion`.")]
		NamingStyle04,


		[MessageTemplate("Пустое решение?!")]
		NotEmpty01,


		[MessageTemplate("Решение должно быть рекурсивным")]
		Recursion01,

		[MessageTemplate("Решение должно быть нерекурсивным")]
		Recursion02,


		[MessageTemplate("Используйте return вместо if. Конструкцию `if (expr) return true; else return false;` можно записать проще: `return expr`.")]
		RedundantIf01,


		[MessageTemplate("Разрешено передавать через `ref` только примитивные типы.")]
		RefArguments01,


		[MessageTemplate("Используйте `var` при инициализации локальной переменной.")]
		VarInVariableDeclaration01,


		[MessageTemplate("В блоке `if` всегда происходит выход из функции, поэтому `else` можно убрать.")]
		RedundantElse01,

		[MessageTemplate("Возможно, в слове {0} есть ошибка в написании. {1}")]
		Misspelling01
	}

	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class MessageTemplateAttribute : Attribute
	{
		public string Template { get; set; }

		public MessageTemplateAttribute(string template)
		{
			Template = template;
		}
	}

	public static class StyleErrorTypeExtensions
	{
		public static string GetTemplate(this StyleErrorType type)
		{
			var attribute = type.GetAttribute<MessageTemplateAttribute>();
			if (attribute == null)
				throw new ArgumentNullException($"Bad StyleErrorType {type}, it has no MessageTemplate attribute");
			return attribute.Template;
		}
	}
}