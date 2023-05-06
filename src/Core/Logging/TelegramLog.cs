using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Ulearn.Common.Extensions;
using Ulearn.Core.Telegram;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.Formatting;

namespace Ulearn.Core.Logging
{
	public class TelegramLog : ILog
	{
		private static readonly ErrorsBot errorsBot = new();
		public OutputTemplate OutputTemplate { get; set; } = OutputTemplate.Default;

		public void Log(LogEvent logEvent)
		{
			if (logEvent == null)
				return;
			if (logEvent.Exception is ApiRequestException)
				return;

			var sourceContext = LoggerSetup.GetSourceContext(logEvent);
			var renderedEvent = LogEventFormatter.Format(logEvent, OutputTemplate).EscapeMarkdown();
			var message = $"{logEvent.Level} log message from *{sourceContext}*:\n\n```{renderedEvent}```";

			errorsBot.PostToChannel(message, ParseMode.Markdown);
		}

		public bool IsEnabledFor(LogLevel level)
		{
			return true;
		}

		public ILog ForContext(string context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			return new SourceContextWrapper(this, context);
		}
	}
}