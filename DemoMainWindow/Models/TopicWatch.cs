using Confluent.Kafka;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DemoMainWindow
{
	public class TopicWatch : INotifyPropertyChanged
	{
		private string _groupId;
		private string _topic;
		private long _totalMessagesInTopic = 0;
		private int _messagesProcessed = 0;
		private int _pendingMessagesProcessed = 0;
		private System.Timers.Timer? _refreshTimer;
		private readonly object _lock = new object();

		public TopicWatch(string groupId, string topic)
		{
			_groupId = groupId;
			_topic = topic;

			Task.Run(() => RefreshTopicMessageCount());

			_refreshTimer = new System.Timers.Timer(1000);
			_refreshTimer.Elapsed += (s, e) => RefreshTopicMessageCount();
			_refreshTimer.Start();
		}

		public string GroupId
		{
			get => _groupId;
		}

		public string Topic
		{
			get => _topic;
		}

		public string Key => $"{GroupId}|{Topic}";

		public long TotalMessagesInTopic
		{
			get => _totalMessagesInTopic;
			private set
			{
				_totalMessagesInTopic = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Progress));
				OnPropertyChanged(nameof(ProgressPercentage));
			}
		}

		public int MessagesProcessed
		{
			get => _messagesProcessed;
			private set
			{
				_messagesProcessed = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Progress));
				OnPropertyChanged(nameof(ProgressPercentage));
			}
		}

		public void IncrementMessagesProcessed()
		{
			lock (_lock)
			{
				_pendingMessagesProcessed++;
			}
		}

		public string Progress => $"{MessagesProcessed}/{TotalMessagesInTopic}";

		public double ProgressPercentage
		{
			get
			{
				if (TotalMessagesInTopic == 0) return 0;
				return (double)MessagesProcessed / TotalMessagesInTopic * 100;
			}
		}

		private void RefreshTopicMessageCount()
		{
			try
			{
				var adminConfig = new AdminClientConfig
				{
					BootstrapServers = "kafka-broker-1:9092,kafka-broker-2:9094,kafka-broker-3:9095"
				};

				var consumerConfig = new ConsumerConfig
				{
					BootstrapServers = "kafka-broker-1:9092,kafka-broker-2:9094,kafka-broker-3:9095",
					GroupId = $"topic-watch-temp-{Guid.NewGuid()}",
					AutoOffsetReset = AutoOffsetReset.Latest
				};

				using var adminClient = new AdminClientBuilder(adminConfig).Build();
				using var consumer = new ConsumerBuilder<Ignore, Ignore>(consumerConfig).Build();

				// 使用AdminClient获取Topic的metadata
				var metadata = adminClient.GetMetadata(Topic, TimeSpan.FromSeconds(5));
				var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == Topic);

				if (topicMetadata == null || topicMetadata.Partitions == null || !topicMetadata.Partitions.Any())
				{
					TotalMessagesInTopic = 0;
					return;
				}

				// 创建TopicPartition列表
				var topicPartitions = topicMetadata.Partitions
					.Select(p => new TopicPartition(Topic, new Partition(p.PartitionId)))
					.ToList();

				// 使用Consumer获取每个分区的最高水位（high watermark）
				var watermarkOffsets = topicPartitions
					.Select(tp => consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5)))
					.ToList();

				// 总消息数 = 所有分区的High水位之和
				long totalMessages = watermarkOffsets.Sum(wm => wm.High.Value);

				// 一次性更新所有数据并触发UI刷新
				int processedDelta;
				lock (_lock)
				{
					processedDelta = _pendingMessagesProcessed;
					_pendingMessagesProcessed = 0;
				}

				// 更新Topic总数
				TotalMessagesInTopic = totalMessages;

				// 更新已处理数（触发UI更新）
				if (processedDelta > 0)
				{
					MessagesProcessed += processedDelta;
				}
			}
			catch
			{
				// 忽略错误，保持原有值
			}
		}

		public void Dispose()
		{
			_refreshTimer?.Stop();
			_refreshTimer?.Dispose();
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
