using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace FEApp.Services
{
	public class DialogBoxService
	{
		/// <summary>
		/// Show a content box.
		/// </summary>
		/// <param name="title">The title of the box.</param>
		/// <param name="content">The content of the box (Can be anything).</param>
		/// <param name="primaryButton">The primary button text.</param>
		/// <param name="secondaryButton">The secondary button text.</param>
		/// <returns>Returns a ContentDialogResult.</returns>
		public static async Task<ContentDialogResult> ShowContentBox(string title, object content, string primaryButton, string secondaryButton)
		{
			ContentDialog contentDialog = new ContentDialog
			{
				Title = title,
				Content = content,
				PrimaryButtonText = primaryButton,
				CloseButtonText = secondaryButton
			};
			return await contentDialog.ShowAsync();
		}

		/// <summary>
		/// Close all active dialogs.
		/// </summary>
		public static void CloseActiveDialogs()
		{
			IReadOnlyList<Popup> popups = VisualTreeHelper.GetOpenPopups(Window.Current);
			foreach (Popup popup in popups)
			{
				if (popup.Child is ContentDialog)
				{
					(popup.Child as ContentDialog).Hide();
				}
			}
		}

		/// <summary>
		/// Check if any content dialog is open.
		/// </summary>
		/// <returns>Returns true or false according to the state of the Content Dialog.</returns>
		public static bool IsAnyContentDialogOpen()
		{
			IReadOnlyList<Popup> popups = VisualTreeHelper.GetOpenPopups(Window.Current);
			bool isOpen = false;
			foreach (Popup popup in popups)
			{
				if (popup.Child is ContentDialog)
				{
					isOpen = true;
				}
			}
			return !isOpen;
		}
	}
}