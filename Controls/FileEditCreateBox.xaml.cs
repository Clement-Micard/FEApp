using Windows.UI.Xaml.Controls;

namespace FEApp.Controls
{
	public sealed partial class FileEditCreateBox : ContentDialog
	{
		public string Result { get; set; }

		public FileEditCreateBox(string title, string primaryText, string secondaryText, string placeholder)
		{
			InitializeComponent();
			Title = title;
			PrimaryButtonText = primaryText;
			SecondaryButtonText = secondaryText;
			ElementNameBox.PlaceholderText = placeholder;
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			Result = ElementNameBox.Text.Sanitize();
		}
	}
}
