using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Responses.Exercise
{
	public enum AutomaticExerciseCheckingProcessStatus
	{
		ServerError, // Ошибка на сервере или статус SandoxError в чеккере
		Done, // Проверена
		Waiting, // В очереди на проверку
		Running, // Проверяется
		WaitingTimeLimitExceeded // Задача выкинута из очереди, потому что её никто не взял на проверку
	}

	public enum AutomaticExerciseCheckingResult
	{
		NotChecked,
		CompilationError,
		RightAnswer,
		WrongAnswer,
		RuntimeError
	}

	[DataContract]
	public class ExerciseAutomaticCheckingResponse
	{
		[DataMember]
		public AutomaticExerciseCheckingProcessStatus ProcessStatus;

		[DataMember]
		public AutomaticExerciseCheckingResult Result;

		[DataMember]
		public string? Output;

		[DataMember]
		public string? CheckerLogs;

		[DataMember]
		public List<ReviewInfo>? Reviews;

		public static ExerciseAutomaticCheckingResponse Build(AutomaticExerciseChecking checking,
			List<ReviewInfo>? botReviews, bool showCheckerLogs)
		{
			var result = new ExerciseAutomaticCheckingResponse
			{
				ProcessStatus = AutomaticExerciseCheckingProcessStatus.ServerError,
				Result = AutomaticExerciseCheckingResult.NotChecked
			};
			result.ProcessStatus = GetProcessStatus(checking);
			result.Output = checking.Output?.Text;
			result.CheckerLogs = showCheckerLogs ? checking.DebugLogs?.Text : null;
			if (result.ProcessStatus != AutomaticExerciseCheckingProcessStatus.Done)
			{
				if (result.Output == null)
				{
					if (result.ProcessStatus == AutomaticExerciseCheckingProcessStatus.WaitingTimeLimitExceeded)
						result.Output ??= "К сожалению, мы не смогли проверить ваше решение. Попробуйте отправить его снова";
					else if (result.ProcessStatus == AutomaticExerciseCheckingProcessStatus.Running)
						result.Output ??= "Ваше решение уже проверяется";
					else if (result.ProcessStatus == AutomaticExerciseCheckingProcessStatus.Waiting)
						result.Output ??= "Решение ждет своей очереди на проверку, мы будем пытаться проверить его еще 10 минут";
				}
				return result;
			}
			if (checking.IsCompilationError)
			{
				result.Result = AutomaticExerciseCheckingResult.CompilationError;
				result.Output = checking.CompilationError?.Text;
				return result;
			}
			result.Result = checking.IsRightAnswer ? AutomaticExerciseCheckingResult.RightAnswer : AutomaticExerciseCheckingResult.WrongAnswer;
			if(checking.IsRightAnswer)
				result.Reviews = botReviews;
			return result;
		}

		private static AutomaticExerciseCheckingProcessStatus GetProcessStatus(AutomaticExerciseChecking checking)
		{
			var timeLimit = TimeSpan.FromMinutes(15);
			var isOldChecking = checking.Timestamp < DateTime.Now.Subtract(timeLimit);
			return checking.Status switch
			{
				AutomaticExerciseCheckingStatus.Done => AutomaticExerciseCheckingProcessStatus.Done,
				AutomaticExerciseCheckingStatus.Waiting =>
					isOldChecking
						? AutomaticExerciseCheckingProcessStatus.WaitingTimeLimitExceeded
						: AutomaticExerciseCheckingProcessStatus.Waiting,
				AutomaticExerciseCheckingStatus.Running =>
					isOldChecking
						? AutomaticExerciseCheckingProcessStatus.ServerError
						: AutomaticExerciseCheckingProcessStatus.Running,
				AutomaticExerciseCheckingStatus.RequestTimeLimit => AutomaticExerciseCheckingProcessStatus.WaitingTimeLimitExceeded,
				AutomaticExerciseCheckingStatus.Error => AutomaticExerciseCheckingProcessStatus.ServerError,
				_ => AutomaticExerciseCheckingProcessStatus.ServerError
			};
		}
	}
}