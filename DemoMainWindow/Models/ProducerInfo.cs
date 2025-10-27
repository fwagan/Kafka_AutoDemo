using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace DemoMainWindow
{
	public class ProducerInfo : INotifyPropertyChanged, ILoggerDataSource
	{
		private readonly ILogger _logger;
		private readonly IProducer<Null, string>? _producer;
		private readonly System.Timers.Timer? _timer;
		private readonly Random _random = new Random();

		private string _id;
		private string _topic = string.Empty;
		private int _intervalMinSeconds = 1;
		private int _intervalMaxSeconds = 5;
		private string _messageTemplate = string.Empty;

		public ProducerInfo(ILogger logger, string id)
		{
			_logger = logger;
			_id = id;

			var config = new ProducerConfig
			{
				BootstrapServers = "localhost:9092",
				ClientId = _id,
				MessageTimeoutMs = 5000,
				RequestTimeoutMs = 5000,
				SocketTimeoutMs = 5000,
				MetadataMaxAgeMs = 5000,
				Acks = Acks.All
			};

			try
			{
				_producer = new ProducerBuilder<Null, string>(config).Build();
				_timer = new System.Timers.Timer();
				_timer.Elapsed += OnTimerElapsed;
			}
			catch (Exception ex)
			{
				_logger.LogError(this, ex, "Failed to create Kafka Producer");
			}
		}

		public string Id
		{
			get => _id;
		}

		public string Topic
		{
			get => _topic;
			set
			{
				_topic = value;
				OnPropertyChanged();
			}
		}

		public int IntervalMinSeconds
		{
			get => _intervalMinSeconds;
			set
			{
				_intervalMinSeconds = value;
				OnPropertyChanged();
			}
		}

		public int IntervalMaxSeconds
		{
			get => _intervalMaxSeconds;
			set
			{
				_intervalMaxSeconds = value;
				OnPropertyChanged();
			}
		}

		public string MessageTemplate
		{
			get => _messageTemplate;
			set
			{
				_messageTemplate = value;
				OnPropertyChanged();
			}
		}

		string ILoggerDataSource.Name => $"Producer-{Topic}:{Id}";

		public void Start()
		{
			if (_timer == null || _producer == null)
			{
				_logger.LogError(this, "Fail to initialize.");
				return;
			}

			_logger.LogInformation("Producer started: Topic={topic}, Interval={intervalMinSeconds}s-{intervalMaxSeconds}s", Topic, IntervalMinSeconds, IntervalMaxSeconds);

			SetRandomInterval();
			_timer.Start();
		}

		public async Task StopAsync()
		{
			_timer?.Stop();
			_logger.LogWarning(this, "Stopping producer.");

			if (_producer != null)
			{
				try
				{
					await Task.Run(() => _producer.Flush(TimeSpan.FromSeconds(5)));
					_producer.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError(this, ex, "Error when stopping Producer.");
				}
			}

			_logger.LogInformation(this, $"Producer stopped: Topic={Topic}");
		}

		private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
		{
			if (_producer == null) return;

			try
			{
				var message = string.IsNullOrWhiteSpace(MessageTemplate)
					? $"Test message at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}"
					: MessageTemplate.Replace("{timestamp}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

				var deliveryResult = await _producer.ProduceAsync(Topic, new Message<Null, string> { Value = message });

				if (deliveryResult.Status == PersistenceStatus.Persisted)
				{
					_logger.LogInformation(this, $"Message sent: Partition={deliveryResult.Partition.Value}, Offset={deliveryResult.Offset.Value}, Message={message}");
				}
				else
				{
					_logger.LogWarning(this, $"Message status: {deliveryResult.Status}, Message={message}");
				}

				SetRandomInterval();
			}
			catch (ProduceException<Null, string> ex)
			{
				_logger.LogError(this, $"Message sending failure: {ex.Error.Reason}");
			}
			catch (Exception ex)
			{
				_logger.LogError(this, ex, "Error when sending message");
			}
		}

		private void SetRandomInterval()
		{
			if (_timer == null) return;

			var nextInterval = _random.Next(IntervalMinSeconds * 1000, IntervalMaxSeconds * 1000);
			_timer.Interval = nextInterval;
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}