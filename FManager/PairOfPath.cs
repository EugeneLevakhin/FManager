using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FManager
{
    class PairOfPath                                    // class for representation pair of folders path for copying async
    {
        private string sourcePath;
        public string SourcePath
        {
            get { return sourcePath; }
            set { sourcePath = value; }
        }

        private string destinationPath;
        public string DestinationPath
        {
            get { return destinationPath; }
            set { destinationPath = value; }
        }

        public PairOfPath(string sourcePath, string destinationPath)
        {
            this.sourcePath = sourcePath;
            this.destinationPath = destinationPath;
        }
    }
}
