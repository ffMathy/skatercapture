using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SkaterCapture.Models
{
    public class ImageCollection
    {
        private List<string> _files;

        public ImageCollection()
        {
            _files = new List<string>();
        }

        public void AddFile(string path)
        {
            _files.Add(path);
        }

        public IEnumerable<string> Files
        {
            get { return _files.Where((e, i) => i % (int)Math.Max(_files.Count / 5.0, 1) == 0); }
        }
    }
}
