namespace FEApp.Models
{
    public class FileItemClickedArgs
    {
        public FileItem ClickedItem { get; set; }

        public FileItemClickedArgs(FileItem clickedItem)
        {
            ClickedItem = clickedItem;
        }
    }
}
