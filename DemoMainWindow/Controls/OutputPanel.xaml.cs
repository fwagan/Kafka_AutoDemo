using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DemoMainWindow
{
	public partial class OutputPanel : UserControl
	{
		public static readonly DependencyProperty LogsProperty =
			DependencyProperty.Register(
				nameof(Logs),
				typeof(ObservableCollection<LogEntry>),
				typeof(OutputPanel),
				new PropertyMetadata(null));

		public ObservableCollection<LogEntry>? Logs
		{
			get => (ObservableCollection<LogEntry>?)GetValue(LogsProperty);
			set => SetValue(LogsProperty, value);
		}

		public OutputPanel()
		{
			InitializeComponent();
		}
	}
}
