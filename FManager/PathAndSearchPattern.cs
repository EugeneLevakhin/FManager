using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FManager
{
    class PathAndSearchPattern
    {
        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        private string searchPattern;
        public string SearchPattern
        {
            get { return searchPattern; }
            set { searchPattern = value; }
        }

        public PathAndSearchPattern(string path, string searchPattern)
        {
            this.path = path;
            this.searchPattern = searchPattern;
        }
    }
}
