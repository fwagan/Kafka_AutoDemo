using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DemoMainWindow
{
	public class ConsumerGroup : INotifyPropertyChanged
	{
		private string _groupId;
		private string _topics = string.Empty;
		private int _consumerCount = 0;

		public ConsumerGroup(string groupId, string topics)
		{
			_groupId = groupId;
			_topics = topics;
		}

		public string GroupId
		{
			get => _groupId;
		}

		public string Topics
		{
			get => _topics;
			set
			{
				_topics = value;
				OnPropertyChanged();
			}
		}

		public string[] GetTopicArray()
		{
			return Topics.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
		}

		public int ConsumerCount
		{
			get => _consumerCount;
			set
			{
				_consumerCount = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
