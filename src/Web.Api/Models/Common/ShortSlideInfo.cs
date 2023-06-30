using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Core.Courses.Slides;

namespace Ulearn.Web.Api.Models.Common
{
	[DataContract]
	public class ShortSlideInfo
	{
		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public string Title { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public bool Hide { get; set; }

		/// <summary>
		/// Человекочитаемый фрагмент url для слайда
		/// </summary>
		[DataMember]
		public string Slug { get; set; }

		[DataMember]
		public int MaxScore { get; set; }

		[DataMember]
		public string? ScoringGroup { get; set; }

		[DataMember]
		public SlideType Type { get; set; }

		[DataMember]
		public string ApiUrl { get; set; }

		/// <summary>
		/// Количество вопросов в quiz
		/// </summary>
		[DataMember]
		public int QuestionsCount { get; set; }

		[DataMember]
		public int QuizMaxTriesCount { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string GitEditLink { get; set; }

		[DataMember]
		public bool ContainsVideo { get; set; }

		[DataMember]
		public AdditionalContent AdditionalContentInfo { get; set; }
	}
}