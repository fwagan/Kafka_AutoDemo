using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DemoMainWindow
{
	public class ConsumerInfo : INotifyPropertyChanged, ILoggerDataSource
	{
		private readonly ILogger _logger;
		private IConsumer<Ignore, string>? _consumer;
		private CancellationTokenSource? _cancellationTokenSource;
		private Task? _consumeTask;

		private string _id;
		private string _topics = string.Empty;
		private string _groupId = string.Empty;
		private string[]? _topicArrayCache;

		public ConsumerInfo(ILogger logger, string id)
		{
			_logger = logger;
			_id = id;
		}

		public string Id
		{
			get => _id;
		}

		public string Topics
		{
			get => _topics;
			set
			{
				_topics = value;
				_topicArrayCache = null;
				OnPropertyChanged();
			}
		}

		public string[] TopicArray
		{
			get
			{
				if (_topicArrayCache == null)
				{
					_topicArrayCache = Topics.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
				}
				return _topicArrayCache;
			}
		}

		public string GroupId
		{
			get => _groupId;
			set
			{
				_groupId = value;
				OnPropertyChanged();
			}
		}

		string ILoggerDataSource.Name => $"Consumer-{Topics}:{Id}";

		public void Start()
		{
			var config = new ConsumerConfig
			{
				BootstrapServers = "kafka-broker-1:9092,kafka-broker-2:9094,kafka-broker-3:9095",
				GroupId = GroupId,
				ClientId = _id,
				AutoOffsetReset = AutoOffsetReset.Earliest,
				EnableAutoCommit = false,
				EnableAutoOffsetStore = false
			};

			try
			{
				_consumer = new ConsumerBuilder<Ignore, string>(config).Build();
				_consumer.Subscribe(TopicArray);

				_cancellationTokenSource = new CancellationTokenSource();
				_consumeTask = Task.Run(() => ConsumeMessagesAsync(_cancellationTokenSource.Token));

				_logger.LogInformation("Consumer started: Topics={topics}, GroupId={groupId}", Topics, GroupId);
			}
			catch (Exception ex)
			{
				_logger.LogError(this, ex, "Failed to initialize Kafka Consumer");
			}
		}

		public async Task StopAsync()
		{
			_cancellationTokenSource?.Cancel();
			_logger.LogWarning(this, "Stopping consumer.");

			if (_consumeTask != null)
			{
				try
				{
					await Task.WhenAny(_consumeTask, Task.Delay(TimeSpan.FromSeconds(5)));
				}
				catch (Exception ex)
				{
					_logger.LogError(this, ex, "Error occurs when waiting Consumer to complete task");
				}
			}

			if (_consumer != null)
			{
				try
				{
					await Task.Run(() => _consumer.Close());
					_consumer.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError(this, ex, "Error occurs when stopping Consumer");
				}
			}

			_logger.LogInformation(this, $"Consumer stopped: Topics={Topics}, GroupId={GroupId}");
		}

		private async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
		{
			if (_consumer == null)
			{
				return;
			}

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));

						if (consumeResult != null)
						{
							_logger.LogInformation(this, $"Comsuming message: Topic={consumeResult.Topic}, Partition={consumeResult.Partition.Value}, Offset={consumeResult.Offset.Value}, Message={consumeResult.Message.Value}");

							await ProcessMessageAsync(cancellationToken);

							_consumer.Commit(consumeResult);
						}
					}
					catch (ConsumeException ex)
					{
						_logger.LogError(this, $" Error occurs when consuming message: {ex.Error.Reason}");
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Cancellation requested, exit gracefully
			}
			catch (Exception ex)
			{
				_logger.LogError(this, ex, "Unexpected error occurs when consuming message");
			}
		}

		private async Task ProcessMessageAsync(CancellationToken cancellationToken)
		{
			await Task.Delay(Random.Shared.Next(500, 1000), cancellationToken);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
