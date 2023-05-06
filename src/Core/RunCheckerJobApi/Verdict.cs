namespace Ulearn.Core.RunCheckerJobApi
{
	public enum Verdict
	{
		NA = 0,

		// ExerciseType.CheckExitCode. В этом режиме задача засчитывается, если Ok.
		// ExerciseType.CheckOutput. Означает, что всё штатно протестировалось. Возвращается в том числе если тесты не прошли. Задача принимается при Ok после сравнения stdout.
		// ExerciseType.CheckPoints. Означает, что всё штатно протестировалось. Возвращается в том числе если тесты не прошли. Задача принимается при Ok при подходящем Points.
		Ok = 1,
		CompilationError = 2,
		RuntimeError = 3,
		SecurityException = 4,
		SandboxError = 5, // Означает ошибку чеккера при проверке, как внутри сандбокса, так и при работе самого RunCheckerJob
		OutputLimit = 6,
		TimeLimit = 7,
		MemoryLimit = 8,
		WrongAnswer = 9
	}
}