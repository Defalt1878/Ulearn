using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.RunCheckerJobApi
{
	[DataContract]
	public class RunningResults
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public Verdict Verdict { get; set; }

		[IgnoreDataMember]
		private string compilationOutput;

		[DataMember]
		[NotNull]
		public string CompilationOutput
		{
			get => compilationOutput ?? "";
			set => compilationOutput = value;
		}

		[IgnoreDataMember]
		private string output;

		// Для вывода пользователю используется GetOutput()
		[DataMember]
		[NotNull]
		public string Output // Для C# это stdout
		{
			get => output ?? "";
			set => output = value;
		}

		[IgnoreDataMember]
		private string error;

		[DataMember]
		[NotNull]
		public string Error // Для C# это stderr
		{
			get => error ?? ""; // Для C# это stdout
			set => error = value;
		}

		[DataMember]
		public float? Points { get; set; }

		[DataMember]
		[CanBeNull]
		public List<StyleError> StyleErrors { get; set; }

		[DataMember]
		[CanBeNull]
		public string[] Logs { get; set; }

		[DataMember]
		public TestResultInfo TestResultInfo { get; set; }

		[IgnoreDataMember]
		private readonly int? timeLimit;

		public RunningResults()
		{
		}

		public RunningResults(string id, Verdict verdict, int? timeLimit = null, string compilationOutput = "", string output = "", string error = "")
		{
			Id = id;
			Verdict = verdict;
			CompilationOutput = compilationOutput;
			Output = output;
			Error = error;
			this.timeLimit = timeLimit;
		}

		public RunningResults(Verdict verdict, int? timeLimit = null, string compilationOutput = "", string output = "", string error = "")
			: this(null, verdict, timeLimit, compilationOutput, output, error)
		{
		}

		public override string ToString()
		{
			string message;
			switch (Verdict)
			{
				case Verdict.CompilationError:
					message = CompilationOutput;
					break;
				case Verdict.SandboxError:
					message = Error;
					break;
				default:
					message = string.Join("\n", new[] { Output, Error }.Where(s => !string.IsNullOrWhiteSpace(s)));
					break;
			}

			var pointsString = Points?.ToString("#.##"); // Печатает только значимые цифры, максимум две, округляет.
			return $"Id: {Id}, Verdict: {Verdict}: {message}" + (pointsString == null ? null : $", Points: {pointsString}");
		}

		public string GetOutput(bool withFullDescription = false)
		{
			var outputLocal = string.Join("\n", new[] { Output, Error }.Where(s => !string.IsNullOrWhiteSpace(s)));

			return Verdict switch
			{
				Verdict.Ok => outputLocal,
				Verdict.TimeLimit => outputLocal +
									(TestResultInfo != null
										? $"\n{TestResultInfo.FormatTestNumber()}"
										: ""
									) +
									"\nВаше решение не успело пройти" +
									(TestResultInfo?.TestNumber == null
										? " все тесты"
										: " тест"
									) +
									(timeLimit == null
										? null
										: $" за {timeLimit} " + timeLimit.Value.SelectPluralWordInRussian(RussianPluralizationOptions.Seconds)
									) +
									(withFullDescription && TestResultInfo != null
										? $"\n{TestResultInfo.FormatTestDescription()}"
										: ""
									),
				Verdict.WrongAnswer => outputLocal +
										(TestResultInfo != null
											? $"\n{TestResultInfo.FormatTestNumber()}"
											: ""
										) +
										"\nНеправильный ответ\n" +
										(withFullDescription && TestResultInfo != null
											? $"\n{TestResultInfo.FormatTestDescription()}"
											: ""
										),
				Verdict.RuntimeError => outputLocal +
										(TestResultInfo != null
											? $"\n{TestResultInfo.FormatTestNumber()}"
											: ""
										) +
										"\nПрограмма завершилась с ошибкой" +
										(withFullDescription && TestResultInfo != null
											? $"\n{TestResultInfo.FormatTestDescription()}"
											: ""
										),
				_ => outputLocal +
					(TestResultInfo != null
						? $"\n{TestResultInfo.FormatTestNumber()}"
						: ""
					) +
					$"\n{Verdict}"
			};
		}

		public string GetLogs()
		{
			return Logs != null
				? string.Join("\n", Logs)
				: "";
		}
	}

	[DataContract]
	public class TestResultInfo
	{
		[DataMember]
		public int? TestNumber { get; set; }

		[DataMember]
		[CanBeNull]
		public string Input { get; set; }

		[DataMember]
		[CanBeNull]
		public string CorrectOutput { get; set; }

		[DataMember]
		[CanBeNull]
		public string StudentOutput { get; set; }


		public string FormatTestDescription()
		{
			return $"Входные данные:\n" +
					$"{Truncate(Input, 500)}\n" +
					$"Ожидаемый результат:\n" +
					$"{Truncate(CorrectOutput, 500)}\n" +
					$"Ваш результат:\n" +
					$"{Truncate(StudentOutput, 500)}\n";
		}

		public string FormatTestNumber()
		{
			return TestNumber == null
				? null
				: $"Тест №{TestNumber}";
		}

		private static string Truncate(string source, int limit)
		{
			return source.Length <= limit
				? source
				: $"{string.Join("", source.Take(limit))}...";
		}
	}
}