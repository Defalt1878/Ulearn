using System;

namespace Ulearn.Core.Metrics
{
	public class ServiceKeepAliver
	{
		private readonly MetricSender sender;

		public ServiceKeepAliver(MetricSender sender)
		{
			this.sender = sender;
			LastPingTime = DateTime.MinValue;
		}

		public ServiceKeepAliver(string serviceName)
			: this(new MetricSender(serviceName))
		{
		}

		public DateTime LastPingTime { get; private set; }

		public void Ping()
		{
			sender.SendCount("keep_alive.ping");
			LastPingTime = DateTime.Now;
		}

		public void Ping(TimeSpan minDelay)
		{
			var currentTime = DateTime.Now;
			if (currentTime - LastPingTime > minDelay)
				Ping();
		}
	}
}