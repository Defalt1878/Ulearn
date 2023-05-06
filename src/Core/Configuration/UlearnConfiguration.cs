using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Ulearn.Core.Configuration
{
	public static class ApplicationConfiguration
	{
		private static readonly Lazy<IConfigurationRoot> configuration = new(GetConfiguration);

		public static T Read<T>() where T : UlearnConfigurationBase
		{
			var r = configuration.Value.Get<T>();
			return r;
		}

		public static IConfigurationRoot GetConfiguration()
		{
			var applicationPath = string.IsNullOrEmpty(Utils.WebApplicationPhysicalPath)
				? AppDomain.CurrentDomain.BaseDirectory
				: Utils.WebApplicationPhysicalPath;
			var configurationBuilder = new ConfigurationBuilder()
				.SetBasePath(applicationPath);
			configurationBuilder.AddEnvironmentVariables();
			BuildAppSettingsConfiguration(configurationBuilder);
			return configurationBuilder.Build();
		}

		public static void BuildAppSettingsConfiguration(IConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.AddJsonFile("appsettings.json", false, true);
			var environmentName = Environment.GetEnvironmentVariable("UlearnEnvironmentName");
			if (environmentName != null && environmentName.ToLower().Contains("local"))
				configurationBuilder.AddJsonFile("appsettings.local.json", true, true);
		}
	}

	public class UlearnConfigurationBase
	{
		// Достаточно поверхностного копирования. Потому что важно только сохранить ссылку на объект конфигурации целиком
		public void SetFrom(UlearnConfigurationBase other)
		{
			var thisProperties = GetType().GetProperties();
			var otherProperties = other.GetType().GetProperties();

			foreach (var otherProperty in otherProperties)
			foreach (var thisProperty in thisProperties)
				if (otherProperty.Name == thisProperty.Name && otherProperty.PropertyType == thisProperty.PropertyType)
				{
					thisProperty.SetValue(this, otherProperty.GetValue(other));
					break;
				}
		}
	}

	public class HostLogConfiguration
	{
		public bool Console { get; set; } // Печатать ли логи на консоль

		public string DropRequestRegex { get; set; } // Какие логи запросов не логировать (например notifications)

		public bool ErrorLogsToTelegram { get; set; } // Отправлять ли логи в телеграм

		public string PathFormat { get; set; } // Путь до файла с логами

		public string MinimumLevel { get; set; } // Минимальный уровень логирования

		public string DbMinimumLevel { get; set; } // Минимальный уровень логирования событий, связанных с базой данных. Debug заставляет вываодть SQL код отправленных запросов
	}

	public class UlearnConfiguration : UlearnConfigurationBase
	{
		public TelegramConfiguration Telegram { get; set; }

		[CanBeNull]
		public string BaseUrl { get; set; } // Адрес, на котором запущен Ulearn.Web

		[CanBeNull]
		public string BaseUrlApi { get; set; } // Адрес, на котором запущен Web.Api

		public string CoursesDirectory { get; set; } // Папка, где ulearn хранит курсы

		public string ExerciseStudentZipsDirectory { get; set; } // Папка, где ulearn хранит кэш архивов с практиками, которые скачиваются со слайдов задач

		public string ExerciseCheckerZipsDirectory { get; set; } // Папка, где ulearn хранит кэш архивов с чеккерами

		public bool ExerciseCheckerZipsCacheDisabled { get; set; } // Отключает кэш архивов с чеккерами

		public CertificateConfiguration Certificates { get; set; }

		public string GraphiteServiceName { get; set; } // Имя сервиса. Используется в метриках и др.

		public string Database { get; set; } // Connection string к базе

		public GitConfiguration Git { get; set; }

		public string StatsdConnectionString { get; set; } // ConnectionString для подключения к Graphite-relay в формате "address=graphite-relay.com;port=8125;prefixKey=ulearn.local". Можно оставить пустой, чтобы не отправлять метрики

		public string SubmissionsUrl { get; set; } // Url to Ulearn.Web instance. I.E. https://ulearn.me Используется в RunCsJob и RunCheckerJob

		public string RunnerToken { get; set; } // Must be equal on Ulearn.Web and RunC***Job instance

		public int? KeepAliveInterval { get; set; } // Некоторые сервисы регулярно посылают пинг в сборщик метрик, по отсутствию пингов можно определить, что сервис умер

		public HostLogConfiguration HostLog { get; set; } // Настроки логирования

		public int? Port { get; set; }

		public bool? ForceHttps { get; set; }

		public string Environment { get; set; } // Имя окружения. Например, чтобы отличать логи и метрики тетсовых сервисов от боевых

		public HerculesSinkConfiguration Hercules { get; set; } // Настройки сборки логов в Контур Геркулес

		[CanBeNull] public AntiplagiarismClientConfiguration AntiplagiarismClient { get; set; }

		[CanBeNull] public VideoAnnotationsClientConfiguration VideoAnnotationsClient { get; set; }

		[CanBeNull] public XQueueWatcherConfiguration XQueueWatcher { get; set; }

		public bool DisableKonturServices { get; set; } // Нужно поставить true, если вы запускаетесь вне сети Контура, тогда при запуске не будет попыток найти сервисы контура

		public string GoogleAccessCredentials { get; set; } // Авторизация в гугл, чтобы синхронизировать ведомость курса с гугл-документом

		public string TempDirectory { get; set; } // Папка со временными файлами, которые можно безопасно удалять, по умолчанию системный temp пользователя

		[CanBeNull] public string PythonVisualizerEndpoint { get; set; } // Url сервиса PythonVisualizerApi
	}

	public class VideoAnnotationsClientConfiguration
	{
		public string Endpoint { get; set; } // Адрес, куда делать запросы к VideoAnnotations
	}

	public class AntiplagiarismClientConfiguration
	{
		public bool Enabled { get; set; } // Включает отправку задач в антиплагиат
		public string Endpoint { get; set; } // Адрес, куда делать запросы к VideoAnnotations
		public string Token { get; set; } // Токен авторизации сервиса в сервисе антиплагиата, соответствует значению в таюблице Antiplagiarism.Clients
	}

	public class XQueueWatcherConfiguration
	{
		public bool Enabled { get; set; }
	}

	public class HerculesSinkConfiguration
	{
		public string ApiKey { get; set; }
		public string Stream { get; set; }
	}

	public class TelegramConfiguration
	{
		public string BotToken { get; set; }

		public ErrorsTelegramConfiguration Errors { get; set; }
	}

	public class ErrorsTelegramConfiguration
	{
		public string Channel { get; set; }
	}

	public class CertificateConfiguration
	{
		public string Directory { get; set; }
	}

	public class GitConfiguration
	{
		public GitWebhookConfiguration Webhook { get; set; } // При коммите в курс, если настроена интеграция, ulearn будет получать событие.
	}

	public class GitWebhookConfiguration
	{
		public string Secret { get; set; }
	}
}