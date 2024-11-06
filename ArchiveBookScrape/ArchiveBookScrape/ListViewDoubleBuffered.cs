using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ArchiveBookScrape
{
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(ListView))]
    public class ListViewDoubleBuffered : ListView
    {
        public ListViewDoubleBuffered()
        {
            this.DoubleBuffered = true;
        }
    }
}
