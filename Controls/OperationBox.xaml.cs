using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace FEApp.Controls
{
	public sealed partial class OperationBox : UserControl
	{
		private Popup ParentPopup = null;

		public OperationBox(string operationName = "Placeholder", string operationInfo = null)
		{
			InitializeComponent();
			OperationName.Text = operationName;
			OperationText = operationName;

			if (operationInfo == null)
			{
				OperationInfo.Text = "ThisOperationMayTakeTime".GetLocalized();
			}
			else
			{
				OperationInfo.Text = operationInfo;
			}
		}

		public string OperationText
		{
			get { return (string)GetValue(SetOperationName); }
			set { SetValue(SetOperationName, value); }
		}

		public static DependencyProperty SetOperationName = DependencyProperty.Register("OperationText", typeof(string), typeof(OperationBox),
			new PropertyMetadata("Placeholder", new PropertyChangedCallback(OnMessageTextChanged)));

		private static void OnMessageTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			OperationBox box = d as OperationBox;
			box.OnMessageTextChanged(e);
		}

		private void OnMessageTextChanged(DependencyPropertyChangedEventArgs e)
		{
			OperationName.Text = e.NewValue.ToString();
		}

		public string OperationInfoText
		{
			get { return (string)GetValue(SetOperationInfo); }
			set { SetValue(SetOperationInfo, value); }
		}
		public static DependencyProperty SetOperationInfo = DependencyProperty.Register("OperationInfo", typeof(string), typeof(OperationBox),
			new PropertyMetadata("This operation may take time.", new PropertyChangedCallback(OnOperationInfoChanged)));

		private static void OnOperationInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			OperationBox box = d as OperationBox;
			box.OnOperationInfoChanged(e);
		}

		private void OnOperationInfoChanged(DependencyPropertyChangedEventArgs e)
		{
			OperationInfo.Text = e.NewValue.ToString();
		}

		/// <summary>
		/// Show the progress box dialog.
		/// </summary>
		public void Show()
		{
			Popup popup = new Popup()
			{
				Width = Window.Current.Bounds.Width,
				Height = Window.Current.Bounds.Height,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				IsLightDismissEnabled = false
			};

			OperationBox box = new OperationBox()
			{
				Width = popup.Width,
				Height = popup.Height,
				OperationText = OperationText
			};
			popup.Child = box;
			ParentPopup = popup;

			popup.IsOpen = true;
		}

		/// <summary>
		/// Close the progress box dialog.
		/// </summary>
		public bool Close()
		{
			if (ParentPopup != null && ParentPopup.IsOpen == true)
			{
				ParentPopup.IsOpen = false;
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
