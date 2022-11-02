using FEApp.Enums;

namespace FEApp.Models
{
    public class DirectoryTreeModificationArgs
    {
        public string Path { get; set; }
        public string NewPath { get; set; }
        public DirectoryChangeOperation PerformedOperation { get; set; }

        public DirectoryTreeModificationArgs(string path, string newPath, DirectoryChangeOperation performedOperation)
        {
            Path = path;
            NewPath = newPath;
            PerformedOperation = performedOperation;
        }
    }
}
