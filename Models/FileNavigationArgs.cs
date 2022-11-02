namespace FEApp.Models
{
    public class FileNavigationArgs
    {
        public string Path { get; set; }
        public string PathDisplayName { get; set; }
        public FileNavigationArgs(string path)
        {
            Path = path;
            PathDisplayName = System.IO.Path.GetFileName(path);
        }
    }
}
