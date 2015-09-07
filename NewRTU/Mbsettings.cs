using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NewRTU
{
    [Serializable]
   public class Mbsettings:ISerializable
    {
        private static readonly string _FILE_PATH = "MbSettings.DAT";
        public bool Autostart { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBit { get; set; }
        public Mbsettings()
        {
            Autostart = false;
            Parity = Parity.Even;
            StopBit = StopBits.One;
        }
        public Mbsettings(SerializationInfo info, StreamingContext context)
        {
            Autostart = (bool)info.GetValue("AutoStart",Autostart.GetType());
            Parity = (Parity)info.GetValue("Parity",Parity.GetType());
            StopBit = (StopBits)info.GetValue("StopBit", StopBit.GetType());
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Parity", Parity);
            info.AddValue("StopBit", StopBit);
            info.AddValue("AutoStart",Autostart);
        }

        public static void SaveToFile(Mbsettings ms)
        {
            using (FileStream fs = new FileStream(_FILE_PATH, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, ms);
                string s=string.Format("Настройки сохранены");

                System.Windows.Forms.MessageBox.Show(s);
            }
        }
        public static Mbsettings LoadFromFile()
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(_FILE_PATH, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                return (Mbsettings)bf.Deserialize(fs);
            }
            catch
            {
                return new Mbsettings();
            }
            finally
            {
                if (fs != null) fs.Close();
            }
        }
    }
}
