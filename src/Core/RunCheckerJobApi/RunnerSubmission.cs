using System.ComponentModel;
using System.Runtime.Serialization;
using Ulearn.Common;

namespace Ulearn.Core.RunCheckerJobApi
{
	[DataContract]
	public abstract class RunnerSubmission
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public int TimeLimit { get; set; } = 10; // Суммарно на все тесты

		public override string ToString()
		{
			return $"Id: {Id}";
		}
	}

	[DataContract]
	public abstract class CsRunnerSubmission : RunnerSubmission
	{
		[DataMember]
		public string Input { get; set; }

		[DataMember]
		public bool NeedRun { get; set; } // Только проверить факт, что компилируется

		public override string ToString()
		{
			return $"Id: {Id}, NeedRun: {NeedRun}";
		}
	}

	[DataContract]
	[DisplayName("file")]
	public class FileRunnerSubmission : CsRunnerSubmission
	{
		[DataMember]
		public string Code { get; set; }
	}

	[DataContract]
	[DisplayName("proj")]
	public class ProjRunnerSubmission : CsRunnerSubmission
	{
		[DataMember]
		public byte[] ZipFileData { get; set; }

		[DataMember]
		public string ProjectFileName { get; set; }
	}

	[DataContract]
	[DisplayName("command")]
	public class CommandRunnerSubmission : RunnerSubmission
	{
		[DataMember]
		public byte[] ZipFileData { get; set; }

		[DataMember]
		public Language Language { get; set; }

		[DataMember]
		public string DockerImageName { get; set; }

		[DataMember]
		public string RunCommand { get; set; }

		[DataMember]
		public InterpretNonJsonOutputType InterpretNonJsonOutputAs { get; set; }
	}
}