using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace DemoMainWindow
{
	public class ObservableLogger : ILogger
	{
		private readonly string _categoryName;
		private readonly ObservableCollection<LogEntry> _logs;
		private readonly string? _sourceId;

		public ObservableLogger(string categoryName, ObservableCollection<LogEntry> logs, string? sourceId = null)
		{
			_categoryName = categoryName;
			_logs = logs;
			_sourceId = sourceId;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;

			var message = formatter(state, exception);
			if (exception != null)
			{
				message += $"\n{exception}";
			}

			Application.Current.Dispatcher.Invoke(() =>
			{
				_logs.Add(new LogEntry
				{
					Timestamp = DateTime.Now,
					Level = logLevel,
					Message = message,
					SourceId = _sourceId
				});

				while (_logs.Count > 1000)
				{
					_logs.RemoveAt(0);
				}
			});
		}
	}
}
