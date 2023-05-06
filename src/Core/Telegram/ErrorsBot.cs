using System;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Metrics;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Telegram
{
	public class ErrorsBot : TelegramBot
	{
		private readonly string host;
		private readonly MetricSender metricSender;

		public ErrorsBot(string host = "https://ulearn.me")
		{
			var configuration = ApplicationConfiguration.Read<UlearnConfiguration>();
			channel = configuration.Telegram?.Errors?.Channel;
			var serviceName = configuration.GraphiteServiceName ?? Assembly.GetExecutingAssembly().GetName().Name?.ToLower();
			metricSender = new MetricSender(serviceName);
			this.host = host;
		}

		public ErrorsBot(UlearnConfiguration configuration, MetricSender metricSender, string host = "https://ulearn.me")
		{
			channel = configuration.Telegram?.Errors?.Channel;
			this.metricSender = metricSender;
			this.host = host;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(ErrorsBot));

		public async Task PostToChannelAsync(string message, ParseMode parseMode = ParseMode.Default)
		{
			if (!IsBotEnabled)
				return;

			metricSender.SendCount("errors");
			Log.Info($"Отправляю в телеграмм-канал {channel} сообщение об ошибке:\n{message}");
			if (message.Length > MaxMessageSize)
			{
				Log.Info($"Сообщение слишком длинное, отправлю только первые {MaxMessageSize} байтов");
				message = message[..MaxMessageSize];
			}

			try
			{
				await telegramClient.SendTextMessageAsync(channel, message, parseMode, disableWebPagePreview: true).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				/* Not error because it may cause recursive fails */
				Log.Info(e, $"Не могу отправить сообщение в телеграмм-канал {channel}");
			}
		}

		public void PostToChannel(string message, ParseMode parseMode = ParseMode.Default)
		{
			PostToChannelAsync(message, parseMode).Wait(5000);
		}

		public void PostToChannel(string errorId, Exception exception)
		{
			if (!IsBotEnabled)
				return;

			var elmahUrl = host + "/elmah/detail/" + errorId;

			var text = $"*Произошла ошибка {errorId.EscapeMarkdown()}*\n" +
						$"{exception.Message.EscapeMarkdown()}\n\n" +
						$"Подробности: {elmahUrl.EscapeMarkdown()}";

			PostToChannel(text, ParseMode.Markdown);
		}
	}
}