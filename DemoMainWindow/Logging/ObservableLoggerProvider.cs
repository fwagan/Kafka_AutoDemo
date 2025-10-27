using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace DemoMainWindow
{
	public class ObservableLoggerProvider : ILoggerProvider
	{
		private readonly ObservableCollection<LogEntry> _logs;

		public ObservableCollection<LogEntry> Logs => _logs;

		public ObservableLoggerProvider()
		{
			_logs = new ObservableCollection<LogEntry>();
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new ObservableLogger(categoryName, _logs);
		}

		public ILogger CreateLogger(string categoryName, string sourceId)
		{
			return new ObservableLogger(categoryName, _logs, sourceId);
		}

		public void Dispose()
		{
		}
	}
}
