using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace FEApp.Models
{
	public class FileItem
	{
		public enum FileType
		{
			File,
			Folder,
			Drive
		}

		public ImageSource Icon { get; private set; }
		public string Name { get; private set; }
		public string Extension { get; private set; }
		public FileType Type { get; private set; }
		public string Path { get; private set; }
		public float Opacity { get; private set; }

		public FileItem(ImageSource icon, float opacity, string extension, FileType type, string name, string path)
		{
			Icon = icon;
			Opacity = opacity;
			Extension = extension;
			Type = type;
			Name = name;
			Path = path;
		}
	}
}