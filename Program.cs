using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SetPol
{
    class Program
    {
        static void Main(string[] args)
        {
            //arg0: path to .pol file
            //arg1: registry key path in the form of "Software\Microsoft\Windows\CurrentVersion\Policies
            //arg2: registry value type (currently supporting REG_SZ and REG_DWORD)
            //arg3: registry value (name of registry value)
            //arg4: registry data (data for value)

            var polFilePath = args[0];
            var keyPath = args[1];
            var keyType = args[2];
            var value = args[3];
            var data = args[4];

            PolFileManager polFile = new PolFileManager();
            if (File.Exists(polFilePath))
            {
                polFile.OpenPolFile(polFilePath, PolType.Computer);
                PolValue val = new PolValue();
                val.m_Key = keyPath;
                Enum.TryParse(keyType, out val.m_KeyType);
                val.m_Value = value;
                if (val.m_KeyType == KeyType.REG_DWORD)
                {
                    ulong v = Convert.ToUInt64(data);
                    val.SetDataAsDWORD(v);
                }
                if (val.m_KeyType == KeyType.REG_SZ)
                {
                    val.SetDataAsString(data.ToString());
                }
                if (val.m_KeyType == KeyType.REG_NONE)
                {
                    val.SetDataAsString(data.ToString());
                }

                polFile.Set(val, PolType.Computer);
                polFile.SavePolFile(PolType.Computer);

            }

        }
    }
}
