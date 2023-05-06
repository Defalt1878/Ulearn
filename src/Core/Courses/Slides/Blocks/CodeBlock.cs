using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Model.Edx.EdxComponents;
using Component = Ulearn.Core.Model.Edx.EdxComponents.Component;

namespace Ulearn.Core.Courses.Slides.Blocks
{
	[XmlType("code")]
	public class CodeBlock : SlideBlock, IConvertibleToEdx
	{
		private string code;

		public CodeBlock(string code, Language? language)
		{
			Code = code;
			Language = language;
		}

		public CodeBlock()
		{
		}

		[XmlText]
		public string Code
		{
			get => code;
			set => code = value.RemoveCommonNesting().RemoveEmptyLinesFromStart().TrimEnd();
		}

		/* .NET XML Serializer doesn't understand nullable fields, so we use this hack to make Language? field */
		[XmlIgnore]
		public Language? Language { get; set; }

		public Component ToEdxComponent(EdxComponentBuilderContext context)
		{
			var urlName = context.Slide.NormalizedGuid + context.ComponentIndex;
			Debug.Assert(Language != null, nameof(Language) + " != null");
			return new CodeComponent(urlName, context.DisplayName, urlName, Language.Value, Code);
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			Language ??= context.CourseSettings.DefaultLanguage;
			yield return this;
		}

		public override string ToString()
		{
			return $"{Language} code: {Code}";
		}

		public override string TryGetText()
		{
			return Code;
		}

		#region NullableLanguageHack

		[XmlAttribute("language")]
		[Browsable(false)] [EditorBrowsable(EditorBrowsableState.Never)]
		public Language LanguageSerialized
		{
			get
			{
				Debug.Assert(Language != null, nameof(Language) + " != null");
				return Language.Value;
			}
			set => Language = value;
		}

		[Browsable(false)] [EditorBrowsable(EditorBrowsableState.Never)]
		public bool ShouldSerializeLanguageSerialized()
		{
			return Language.HasValue;
		}

		#endregion
	}
}