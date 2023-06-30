using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Responses.Exercise
{
	[DataContract]
	public class ExerciseManualCheckingResponse
	{
		[DataMember]
		public int? Percent; // null только когда ревью не оценено

		[DataMember]
		public List<ReviewInfo> Reviews;

		public static ExerciseManualCheckingResponse? Build(ManualExerciseChecking? manualExerciseChecking, List<ReviewInfo> reviewInfos)
		{
			if (manualExerciseChecking == null)
				return null;
			return new ExerciseManualCheckingResponse
			{
				Percent = manualExerciseChecking.Percent,
				Reviews = reviewInfos,
			};
		}
	}
}