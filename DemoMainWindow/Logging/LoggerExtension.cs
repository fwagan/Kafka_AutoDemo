using Microsoft.Extensions.Logging;

namespace DemoMainWindow
{
	public static class LoggerExtension
	{
		public static void LogInformation(this ILogger logger, ILoggerDataSource dataSource, string message)
		{
			logger.LogInformation("[{dataSource}] {message}", dataSource.Name, message);
		}

		public static void LogWarning(this ILogger logger, ILoggerDataSource dataSource, string message)
		{
			logger.LogWarning("[{dataSource}] {message}", dataSource.Name, message);
		}

		public static void LogError(this ILogger logger, ILoggerDataSource dataSource, string message)
		{
			logger.LogError("[{dataSource}] {message}", dataSource.Name, message);
		}

		public static void LogError(this ILogger logger, ILoggerDataSource dataSource, Exception ex, string message)
		{
			logger.LogError(ex, "[{dataSource}] {message}", dataSource.Name, message);
		}
	}
}
