using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LezyFileBrowser
{
    public class ListItemTag
    {
        public FileData FileInfo { get; set; }

        public WorkOnFileOrDir WorkOnFileOrDir { get; set; }

    }
}
