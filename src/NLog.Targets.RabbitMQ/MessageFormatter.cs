using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using NLog.Layouts;
using Newtonsoft.Json;

namespace NLog.Targets
{
	public static class MessageFormatter
	{
		private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		static string _hostName;
		private static string HostName
		{
			get { return _hostName = (_hostName ?? Dns.GetHostName()); }
		}

		public static string GetMessageInner(bool useJSON, Layout layout, LogEventInfo info)
		{
			if (!useJSON)
				return layout.Render(info);

			var logLine = new LogLine
				{
					TimeStampISO8601 = info.TimeStamp.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
					Message = info.FormattedMessage,
					Level = info.Level.Name,
					Type = "amqp",
					Source = new Uri(string.Format("nlog://{0}/{1}", HostName, info.LoggerName))
				};

			logLine.AddField("exception", info.Exception);

			if (info.Properties.Count > 0 && info.Properties.ContainsKey("fields"))
				foreach (var kv in (IEnumerable<KeyValuePair<string, object>>) info.Properties["fields"])
					logLine.AddField(kv.Key, kv.Value);

			if (info.Properties.Count > 0 && info.Properties.ContainsKey("tags"))
				foreach (var tag in (IEnumerable<string>) info.Properties["tags"])
					logLine.AddTag(tag);

			logLine.EnsureADT();

			return JsonConvert.SerializeObject(logLine);
		}

		public static long GetEpochTimeStamp(LogEventInfo @event)
		{
			return Convert.ToInt64((@event.TimeStamp - _epoch).TotalSeconds);
		}
	}
}