namespace DemoMainWindow
{
	public class MessageProcessedEventArgs : EventArgs
	{
		public string GroupId { get; }
		public string Topic { get; }

		public MessageProcessedEventArgs(string groupId, string topic)
		{
			GroupId = groupId;
			Topic = topic;
		}
	}
}
