using Microsoft.Extensions.Logging;

namespace DemoMainWindow
{
	public class LogEntry
	{
		public DateTime Timestamp { get; set; } = DateTime.Now;
		public LogLevel Level { get; set; }
		public string Message { get; set; } = string.Empty;
		public string? SourceId { get; set; }
	}
}
