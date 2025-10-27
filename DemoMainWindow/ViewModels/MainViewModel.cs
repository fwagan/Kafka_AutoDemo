using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DemoMainWindow
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private readonly ObservableLoggerProvider _producerLoggerProvider;
		private readonly ObservableLoggerProvider _consumerLoggerProvider;
		private int _nextProducerId = 0;
		private int _nextConsumerId = 0;

		public MainViewModel()
		{
			_producerLoggerProvider = new ObservableLoggerProvider();
			_consumerLoggerProvider = new ObservableLoggerProvider();

			Producers = new ObservableCollection<ProducerInfo>();
			Consumers = new ObservableCollection<ConsumerInfo>();
			ConsumerGroups = new ObservableCollection<ConsumerGroup>();
			TopicWatches = new ObservableCollection<TopicWatch>();

			AddProducerCommand = new RelayCommand(AddProducer);
			DeleteProducerCommand = new RelayCommand<ProducerInfo>(DeleteProducer);

			AddConsumerGroupCommand = new RelayCommand(AddConsumerGroup);
			DeleteConsumerGroupCommand = new RelayCommand<ConsumerGroup>(DeleteConsumerGroup);

			AddConsumerCommand = new RelayCommand(AddConsumer);
			DeleteConsumerCommand = new RelayCommand<ConsumerInfo>(DeleteConsumer);
		}

		#region Producer input

		private string _newProducerTopic = string.Empty;
		private int _newProducerIntervalMin = 1;
		private int _newProducerIntervalMax = 5;
		private string _newProducerMessageTemplate = string.Empty;

		public string NewProducerTopic
		{
			get => _newProducerTopic;
			set
			{
				_newProducerTopic = value;
				OnPropertyChanged();
			}
		}

		public int NewProducerIntervalMin
		{
			get => _newProducerIntervalMin;
			set
			{
				_newProducerIntervalMin = value;
				OnPropertyChanged();
			}
		}

		public int NewProducerIntervalMax
		{
			get => _newProducerIntervalMax;
			set
			{
				_newProducerIntervalMax = value;
				OnPropertyChanged();
			}
		}

		public string NewProducerMessageTemplate
		{
			get => _newProducerMessageTemplate;
			set
			{
				_newProducerMessageTemplate = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Consumer Group input

		private string _newConsumerGroupId = string.Empty;
		private string _newConsumerGroupTopics = string.Empty;

		public string NewConsumerGroupId
		{
			get => _newConsumerGroupId;
			set
			{
				_newConsumerGroupId = value;
				OnPropertyChanged();
			}
		}

		public string NewConsumerGroupTopics
		{
			get => _newConsumerGroupTopics;
			set
			{
				_newConsumerGroupTopics = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Consumer input fields

		private ConsumerGroup? _selectedConsumerGroup;
		private string _newConsumerTopics = string.Empty;
		private bool _isIndependentConsumer = false;

		public string NewConsumerTopics
		{
			get => _newConsumerTopics;
			set
			{
				_newConsumerTopics = value;
				OnPropertyChanged();
			}
		}

		public bool IsIndependentConsumer
		{
			get => _isIndependentConsumer;
			set
			{
				_isIndependentConsumer = value;
				OnPropertyChanged();
			}
		}

		public ConsumerGroup? SelectedConsumerGroup
		{
			get => _selectedConsumerGroup;
			set
			{
				_selectedConsumerGroup = value;
				OnPropertyChanged();
			}
		}

		#endregion

		public string GetNextProducerId()
		{
			var id = _nextProducerId.ToString();
			_nextProducerId = (_nextProducerId + 1) % 10000;
			return id;
		}

		public string GetNextConsumerId()
		{
			var id = _nextConsumerId.ToString();
			_nextConsumerId = (_nextConsumerId + 1) % 10000;
			return id;
		}

		public ObservableCollection<LogEntry> ProducerLogs => _producerLoggerProvider.Logs;
		public ObservableCollection<LogEntry> ConsumerLogs => _consumerLoggerProvider.Logs;


		public ObservableCollection<ProducerInfo> Producers { get; }
		public ObservableCollection<ConsumerInfo> Consumers { get; }
		public ObservableCollection<ConsumerGroup> ConsumerGroups { get; }
		public ObservableCollection<TopicWatch> TopicWatches { get; }

		public ICommand AddProducerCommand { get; }
		public ICommand DeleteProducerCommand { get; }

		public ICommand AddConsumerGroupCommand { get; }
		public ICommand DeleteConsumerGroupCommand { get; }

		public ICommand AddConsumerCommand { get; }
		public ICommand DeleteConsumerCommand { get; }

		private void AddProducer()
		{
			if (!string.IsNullOrWhiteSpace(NewProducerTopic))
			{
				var id = GetNextProducerId();
				var logger = _producerLoggerProvider.CreateLogger("Producer", NewProducerTopic);

				var producer = new ProducerInfo(logger, id)
				{
					Topic = NewProducerTopic,
					IntervalMinSeconds = NewProducerIntervalMin,
					IntervalMaxSeconds = NewProducerIntervalMax,
					MessageTemplate = NewProducerMessageTemplate,
				};
				Producers.Add(producer);

				producer.Start();

				NewProducerTopic = string.Empty;
				NewProducerIntervalMin = 1;
				NewProducerIntervalMax = 5;
				NewProducerMessageTemplate = string.Empty;
			}
		}

		private async void DeleteProducer(ProducerInfo? producer)
		{
			if (producer != null)
			{
				await producer.StopAsync();
				Producers.Remove(producer);
			}
		}

		private void AddConsumerGroup()
		{
			if (!string.IsNullOrWhiteSpace(NewConsumerGroupId) && !string.IsNullOrWhiteSpace(NewConsumerGroupTopics))
			{
				if (ConsumerGroups.Any(g => g.GroupId == NewConsumerGroupId))
				{
					MessageBox.Show("Consumer group ID existed", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				ConsumerGroups.Add(new ConsumerGroup(NewConsumerGroupId, NewConsumerGroupTopics));

				NewConsumerGroupId = string.Empty;
				NewConsumerGroupTopics = string.Empty;
			}
		}

		private void DeleteConsumerGroup(ConsumerGroup? group)
		{
			if (group != null)
			{
				var consumersInGroup = Consumers.Count(c => c.GroupId == group.GroupId);
				if (consumersInGroup > 0)
				{
					MessageBox.Show($"Cannot delete consumer group: {consumersInGroup} comsumer(s) alive", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				ConsumerGroups.Remove(group);
			}
		}

		private void AddConsumer()
		{
			string groupId;
			string topics;
			ConsumerGroup? consumerGroup = null;

			var id = GetNextConsumerId();

			if (IsIndependentConsumer)
			{
				if (string.IsNullOrWhiteSpace(NewConsumerTopics))
				{
					MessageBox.Show("Must choose at least one topic", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				groupId = $"Auto-{id}";
				topics = NewConsumerTopics;

				consumerGroup = new ConsumerGroup(groupId, topics);
				ConsumerGroups.Add(consumerGroup);
			}
			else
			{
				if (SelectedConsumerGroup == null)
				{
					MessageBox.Show("Must choose a consumer group", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				groupId = SelectedConsumerGroup.GroupId;
				topics = SelectedConsumerGroup.Topics;
				consumerGroup = SelectedConsumerGroup;
				consumerGroup.ConsumerCount++;
			}

			var logger = _consumerLoggerProvider.CreateLogger("Consumer", $"{groupId}:{topics}");

			var consumer = new ConsumerInfo(logger, id)
			{
				Topics = topics,
				GroupId = groupId,
				ConsumerGroup = consumerGroup
			};
			consumer.MessageProcessed += OnConsumerMessageProcessed;
			Consumers.Add(consumer);

			var topicArray = topics.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
			foreach (var topic in topicArray)
			{
				var key = $"{groupId}|{topic}";
				if (!TopicWatches.Any(tw => tw.Key == key))
				{
					TopicWatches.Add(new TopicWatch(groupId, topic));
				}
			}

			consumer.Start();

			if (IsIndependentConsumer)
			{
				NewConsumerTopics = string.Empty;
			}
		}

		private void OnConsumerMessageProcessed(object? sender, MessageProcessedEventArgs e)
		{
			var key = $"{e.GroupId}|{e.Topic}";
			var watch = TopicWatches.FirstOrDefault(tw => tw.Key == key);
			if (watch != null)
			{
				watch.IncrementMessagesProcessed();
			}
		}

		private async void DeleteConsumer(ConsumerInfo? consumer)
		{
			if (consumer != null)
			{
				consumer.MessageProcessed -= OnConsumerMessageProcessed;
				await consumer.StopAsync();

				var group = ConsumerGroups.FirstOrDefault(g => g.GroupId == consumer.GroupId);
				if (group != null)
				{
					group.ConsumerCount--;
				}

				Consumers.Remove(consumer);

				var topicArray = consumer.Topics.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
				foreach (var topic in topicArray)
				{
					var key = $"{consumer.GroupId}|{topic}";
					var hasOtherConsumers = Consumers.Any(c => c.GroupId == consumer.GroupId && c.Topics.Split(',').Select(t => t.Trim()).Contains(topic));
					if (!hasOtherConsumers)
					{
						var watch = TopicWatches.FirstOrDefault(tw => tw.Key == key);
						if (watch != null)
						{
							watch.Dispose();
							TopicWatches.Remove(watch);
						}
					}
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
