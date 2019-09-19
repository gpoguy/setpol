using System;
using System.Collections.Generic;
using System.IO;

namespace SetPol {
    //much of the code in here is geared towards saving these settings into domain-based GPOs but the core get/set/save are used by setpol to save against a known pol file.
    public enum PolType
    {
        Computer,
        User
    }
    public enum  KeyType {
        REG_BINARY = 3,
        REG_DWORD = 4,
        REG_DWORD_LITTLE_ENDIAN = 4,
        REG_DWORD_BIG_ENDIAN = 5,
        REG_EXPAND_SZ = 2,
        REG_LINK = 6,
        REG_MULTI_SZ = 7,
        REG_NONE = 0,
        REG_QWORD = 11,
        REG_QWORD_LITTLE_ENDIAN = 11,
        REG_SZ = 1
    }

    public class PolValue {
        public string m_Key;
        public string m_Value;
        public KeyType m_KeyType;
        public int m_DataSize;
        public byte[] m_Data;
        public bool m_bDeleteValue = false;

        public System.Collections.ArrayList m_Parsed;

        public string GetDataAsString() {
            string ret = "";
            for ( long l = 0; l < m_Data.Length; l=l+2 ) {
                char c = System.Convert.ToChar((m_Data[l+1] << 8) + m_Data[l]);
                    if ( c != 0 && c != '\n' )
                        ret += c;
            }
            return ret;
        }
        public ulong GetDataAsDWORD() {
            ulong ret = 0;
            if ( m_Data.Length >= 4 )
                ret = (ulong)((m_Data[3]<<24) + (m_Data[2]<<16) + (m_Data[1]<<8) + (m_Data[0]));
            return ret;
        }
        public void SetDataAsEmpty()
        {
            m_DataSize = 2;
            m_Data = new byte[m_DataSize];
        }
        public void SetDataAsString( string val ) {
            m_DataSize = val.Length*2;
            m_Data = new byte[m_DataSize];
            System.CharEnumerator ce = val.GetEnumerator();
            long l = 0;
            while ( ce.MoveNext() ) {
                m_Data[l] = (byte)(ce.Current&0xFF);
                l++;
                m_Data[l] = (byte)(ce.Current>>8);
                l++;
            }
        }
        public void SetDataAsMString(string[] arr, int Length)        
        {           
            m_DataSize = (Length+arr.Length) * 2;
            m_Data = new byte[m_DataSize];
            long l = 0;
            foreach (string elem in arr)
            {
                if (string.IsNullOrEmpty(elem))
                    continue;
                System.CharEnumerator ce = elem.GetEnumerator();                
                while (ce.MoveNext())
                {
                    m_Data[l] = (byte)(ce.Current & 0xFF);
                    l++;
                    m_Data[l] = (byte)(ce.Current >> 8);
                    l++;
                }
                m_Data[l] = 0;
                l++;
                m_Data[l] = 0;
                l++;
            }
        }
        public void SetDataAsDWORD(ulong val) {
            m_DataSize = 4;
            m_Data = new byte[m_DataSize];
            m_Data[0] = (byte)(val&0xFF);
            m_Data[1] = (byte)((val&0xFF00)>>8);
            m_Data[2] = (byte)((val&0xFF0000)>>16);
            m_Data[3] = (byte)((val&0xFF000000)>>24);
        }
        public void SetDataAsQWORD(UInt64 val)
        {
            m_DataSize = 8;
            m_Data = new byte[m_DataSize];
            m_Data[0] = (byte)(val & 0xFF);
            m_Data[1] = (byte)((val & 0xFF00) >> 8);
            m_Data[2] = (byte)((val & 0xFF0000) >> 16);
            m_Data[3] = (byte)((val & 0xFF000000) >> 24);
            m_Data[4] = (byte)((val & 0xFF00000000) >> 32);
            m_Data[5] = (byte)((val & 0xFF0000000000) >> 40);
            m_Data[6] = (byte)((val & 0xFF000000000000) >> 48);
            m_Data[7] = (byte)((val & 0xFF00000000000000) >> 56);
        }
    }
    public class PolFileManager {
        public void OpenPolFile(string path, PolType type) {
            if ( type == PolType.Computer  ) {
                if ( m_path_c != null && m_path_c.Length > 0 )
                    return;
                m_vals_c = new System.Collections.Generic.Dictionary<string, PolValue>();
                m_path_c = path;
            } else {
                if ( m_path_u != null && m_path_u.Length > 0 )
                    return;
                m_vals_u = new System.Collections.Generic.Dictionary<string, PolValue>();
                m_path_u = path;
            }
            if (File.Exists(path) ) {
                byte[] file = File.ReadAllBytes(path);
                if ( !( file[0] == 0x50 && 
                       file[1] == 0x52 && 
                    file[2] == 0x65 && 
                    file[3] == 0x67 && 
                    file[4] == 0x01 && 
                    file[5] == 0x00 && 
                    file[6] == 0x00 && 
                    file[7] == 0x00 ) ) {
                    System.String str = Properties.Resources.IDS_INVALID_POL_FORMAT;
                    throw new Exception(String.Format(str,path));
                }                            
                FillVals(file,type);
            }
        }
        public PolValue Get(string key, PolType type) {
            System.Collections.Generic.Dictionary<string,PolValue> dict = GetVals(type);
                        string [] splitKey = key.Split(new char[] {'\\'});
            int splitLength = splitKey.Length;
            //create a variable that includes the reg path minus the valuename so that we can compare that as well down below
            string keyName = key.TrimEnd(splitKey[splitLength - 1].ToCharArray());
            foreach (string dictKey in dict.Keys)
            {
                string[] splitDict = dictKey.Split(new char[] { '\\' });
                if (splitDict[splitDict.Length-1].Equals(splitKey[splitLength - 1].ToLower()) && dictKey.Contains(keyName.ToLower()))
                    return (PolValue)GetVals(type)[dictKey.ToLower()];
                
            }
            return null; 
        }
        public System.Collections.Generic.Dictionary<string, PolValue> GetVals(string key, PolType type)
        {
            System.Collections.Generic.Dictionary<string, PolValue> dict = GetVals(type);
            return dict;
        }
        public System.Collections.ArrayList GetLike(string Key, PolType type) {            
            System.Collections.ArrayList arr = new System.Collections.ArrayList();
            foreach ( KeyValuePair<string,PolValue> val in GetVals(type) ) {
                PolValue vl = (PolValue)(val.Value);
                string k  = val.Key.ToString();
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match( k, Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    PolValue newval = new PolValue();
                    newval.m_Key = vl.m_Key;
                    newval.m_Value = vl.m_Value;
                    newval.m_KeyType = vl.m_KeyType;
                    newval.m_DataSize = vl.m_DataSize;
                    newval.m_Data = vl.m_Data;
                    newval.m_Parsed = new System.Collections.ArrayList();
                    newval.m_Parsed.Clear();
                    newval.m_Parsed.AddRange(m.Groups);
                    arr.Add(newval);
                }
            }
            return arr;
        }
        public void Set(PolValue val, PolType type) {
            // generate key
            string key = val.m_Key + "\\" + val.m_Value;
            key = key.ToLower();
            // generate disabled/enabled (inverse) key based on provided key
            string delPrefix = "**del.";
            string inverseKey = val.m_Key + "\\" + delPrefix + val.m_Value;
            if (val.m_Value.Length >= 6 && val.m_Value.Contains(delPrefix))
            {
                inverseKey = val.m_Key + "\\" + val.m_Value.Substring(6, val.m_Value.Length - 6);
            }
            inverseKey = inverseKey.ToLower();
            // check if key is being disabled
            if (val.m_bDeleteValue)
            {
                // remove key
               Remove(key, type);
                // add disabled key
               GetVals(type).Add(inverseKey, val);
            }
            else
            {
                // check if key already exists
                if (GetVals(type).ContainsKey(key))
                {
                    // update key value
                    GetVals(type)[key] = val;
                }
                else
                {
                    // check if inverse key already exists
                    if (GetVals(type).ContainsKey(inverseKey))
                    {
                        // remove inverse key
                        Remove(inverseKey, type);
                    }
                    // add new key
                    GetVals(type).Add(key, val);
                }
            }
        }
        public void Remove(string key, PolType type) {
            GetVals(type).Remove(key.ToLower());
        }
        public void SavePolFile(PolType type) {
            byte[] file = FillFile(type);
            File.WriteAllBytes(GetPath(type),file);
        }
        private enum CurrentToken {
                None = 0,
                Key,
                Value,
                Type,
                Size,
                Data }
        private void FillVals(byte[] file, PolType type) {
            long start = 8;
            PolValue pv = null;
            string token = "";
            byte[] buf = new byte [file.Length];
            int buf_size = 0;
            CurrentToken curtoken = CurrentToken.None;
            for ( long l = start; l < file.Length; l=l+2 ) {
                if ( file[l] == '[' && pv == null ) {
                    pv = new PolValue();
                    curtoken = CurrentToken.Key;
                    continue;
                }
                if ( file[l] == ';' && curtoken != CurrentToken.Data ) {
                    switch (curtoken) {
                        case CurrentToken.Key:
                        {
                            pv.m_Key = token;
                            curtoken = CurrentToken.Value;
                            break;
                        }
                        case CurrentToken.Value:
                        {
                            pv.m_Value = token;
                            curtoken = CurrentToken.Type;
                            break;
                        }
                        case CurrentToken.Type:
                        {
                            pv.m_KeyType = (KeyType)((file[l-1]<<24) + (file[l-2]<<16) + (file[l-3]<<8) + (file[l-4]));
                            curtoken = CurrentToken.Size;
                            break;
                        }
                        case CurrentToken.Size:
                        {
                            pv.m_DataSize = ((file[l-1]<<24) + (file[l-2]<<16) + (file[l-3]<<8) + (file[l-4]));
                            curtoken = CurrentToken.Data;
                            break;
                        }
                        case CurrentToken.Data:
                        case CurrentToken.None:
                        default:
                        {
                            System.String str = Properties.Resources.IDS_INVALID_POL_FORMAT;
                            throw new Exception(String.Format(str,GetPath(type)));
                        }
                    }
                    token = "";
                    continue;
                }
                if ( file[l] == ']' && buf_size >= pv.m_DataSize ) {
                    pv.m_Data = new byte[buf_size];
                    Array.Copy(buf,pv.m_Data, buf_size);
                    buf_size = 0;                    
                    GetVals(type)[pv.m_Key.ToLower() + "\\" + pv.m_Value.ToLower()]=pv;
                    pv = null;
                    token = "";
                    continue;
                }
                if ( curtoken == CurrentToken.Data ) {
                    buf[buf_size] = file[l];
                    buf_size++; 
                    if ( buf_size < pv.m_DataSize ) {
                        buf[buf_size] = file[l+1];
                        buf_size++;
                    } else {
                        l--;
                    }
                } else {
                    char c = System.Convert.ToChar((file[l+1] << 8) + file[l]);
                    if ( c != 0 )
                        token += c;
                }
            }
        }
        private byte[] FillFile(PolType type) {
            System.Collections.ArrayList arr = new System.Collections.ArrayList();
            arr.Add (0x50 );
            arr.Add (0x52 );
            arr.Add (0x65 );
            arr.Add (0x67 ); 
            arr.Add (0x01 ); 
            arr.Add (0x00 ); 
            arr.Add (0x00 ); 
            arr.Add (0x00 );
            foreach ( KeyValuePair<string,PolValue> val in GetVals(type) ) {
            //foreach ( System.Collections.DictionaryEntry val in GetVals(type) ) {
                PolValue vl = (PolValue)(val.Value);
                arr.Add('[');
                arr.Add(0);

                // key
                System.CharEnumerator ce = vl.m_Key.GetEnumerator();
                while ( ce.MoveNext() ) {
                    arr.Add ( (ce.Current&0xFF) );
                    arr.Add ( (ce.Current>>8) );
                }
                arr.Add(0);
                arr.Add(0);
                arr.Add(';');
                arr.Add(0);

                // value
                ce = vl.m_Value.GetEnumerator();
                while ( ce.MoveNext() ) {
                    arr.Add ( (ce.Current&0xFF) );
                    arr.Add ( (ce.Current>>8) );
                }
                arr.Add(0);
                arr.Add(0);
                arr.Add(';');
                arr.Add(0);

                // type
                int l = (int)vl.m_KeyType;
                arr.Add( (l&0x000000FF) );
                arr.Add( ((l>>8)&0x0000FFFF) );
                arr.Add( ((l>>16)&0x00FFFFFF) );
                arr.Add( (l>>24) );
                arr.Add(';');
                arr.Add(0);

                //size

                int data_size = vl.m_DataSize;
                if ( vl.m_KeyType == KeyType.REG_SZ ||
                     vl.m_KeyType == KeyType.REG_EXPAND_SZ ||
                     vl.m_KeyType == KeyType.REG_MULTI_SZ )
                {
                    data_size += 2;
                }

                l = data_size;
                arr.Add( (l&0x000000FF) );
                arr.Add( ((l>>8)&0x0000FFFF) );
                arr.Add( ((l>>16)&0x00FFFFFF) );
                arr.Add( (l>>24) );

                arr.Add(';');
                arr.Add(0);


                for ( int i = 0; i < vl.m_Data.Length; i++ ) {
                    arr.Add(vl.m_Data[i]);
                }

                if ( vl.m_KeyType == KeyType.REG_SZ ||
                     vl.m_KeyType == KeyType.REG_EXPAND_SZ ||
                     vl.m_KeyType == KeyType.REG_MULTI_SZ )
                {
                    arr.Add(0);
                    arr.Add(0);
                }
                arr.Add(']');
                arr.Add(0);
            }
            byte[] res = new byte[arr.Count];
            for ( int i = 0; i < arr.Count; i++ ) {
                res[i] = System.Convert.ToByte(arr[i]);
            }
            return res;
        }
        private System.Collections.Generic.Dictionary<string,PolValue> GetVals(PolType type)
        {
                System.Collections.Generic.Dictionary<string,PolValue> vals = null;
                if ( type == PolType.Computer )
                    vals = m_vals_c;
                else
                    vals = m_vals_u;
                return vals;
        }        
        private string GetPath(PolType type) {
                string path = null;
                if ( type == PolType.Computer )
                    path = m_path_c;
                else
                    path = m_path_u;
                return path;
        }
     
        private System.Collections.Generic.Dictionary<string, PolValue> m_vals_c;
        private string m_path_c;
        private System.Collections.Generic.Dictionary<string, PolValue> m_vals_u;
        private string m_path_u;
    }
}
