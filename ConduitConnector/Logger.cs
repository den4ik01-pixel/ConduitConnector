using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConduitConnector
{
    public static class Logger
    {
        public static void WriteData(string pluginName, string projectName, string userName, 
            string revitVersion, string elementsCategory, int elementsChanged, ModificationTypes changeType)
        {
            using(FileStream fs = new FileStream("temp_modifications.mdb", FileMode.Append, FileAccess.Write))
            {
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(pluginName);
                bw.Write(projectName);
                bw.Write(userName);
                bw.Write(revitVersion);
                bw.Write(elementsCategory);
                bw.Write(elementsChanged);
                bw.Write((int)changeType);
            }
        }
    }

    public enum ModificationTypes
    {
        Add,
        Change,
        Delete
    }
}
