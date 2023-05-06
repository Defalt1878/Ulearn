using Telegram.Bot;
using Ulearn.Core.Configuration;

namespace Ulearn.Core.Telegram
{
	public class TelegramBot
	{
		protected const int MaxMessageSize = 2048;
		private readonly string token;
		protected string channel;
		protected readonly TelegramBotClient telegramClient;

		protected TelegramBot()
		{
			token = ApplicationConfiguration.Read<UlearnConfiguration>().Telegram?.BotToken;
			if (!string.IsNullOrEmpty(token))
				telegramClient = new TelegramBotClient(token);
		}

		protected bool IsBotEnabled => !string.IsNullOrWhiteSpace(token) && !string.IsNullOrEmpty(channel);
	}
}