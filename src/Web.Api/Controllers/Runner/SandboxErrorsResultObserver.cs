using System.Threading.Tasks;
using Database.Models;
using Telegram.Bot.Types.Enums;
using Ulearn.Common.Extensions;
using Ulearn.Core.RunCheckerJobApi;
using Ulearn.Core.Telegram;

namespace Ulearn.Web.Api.Controllers.Runner
{
	public class SandboxErrorsResultObserver : IResultObserver
	{
		private readonly ErrorsBot bot;

		public SandboxErrorsResultObserver(ErrorsBot errorsBot)
		{
			bot = errorsBot;
		}

		public async Task ProcessResult(UserExerciseSubmission submission, RunningResults result)
		{
			/* Ignore all verdicts except SandboxError */
			if (result.Verdict != Verdict.SandboxError)
				return;

			var output = result.GetOutput();
			var logs = result.Logs is null ? null : string.Join("\n", result.Logs);
			var message = $"<b>Решение #{submission.Id} не запустилось в песочнице {submission.Sandbox} (SandboxError).</b>\n";
			if (!string.IsNullOrEmpty(output))
				message += $"Вывод:\n<pre>{output.EscapeHtml()}</pre>\n";
			if (!string.IsNullOrEmpty(logs))
				message += $"Логи:\n<pre>{logs.EscapeHtml()}</pre>\n";

			await bot.PostToChannelAsync(message, ParseMode.Html).ConfigureAwait(false);
		}
	}
}