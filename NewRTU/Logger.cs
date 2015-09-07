using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace NewRTU
{
   public class Logger
    {
       string _logName;
       public Logger()
       {
           _logName = "Log_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute+".txt";
           using (StreamWriter writer = new StreamWriter(_logName))
           {
               writer.WriteLine("Начало лога " + DateTime.Now);
           }
       }
       public static string BytePrinter(byte[] data)
       {
           string res = "";
           for (int k = 6; k < data.Length; k++)
           {
               res += " " + data[k];
           }
           return res;
       }
       public void LogTofile(string msg)
       {
           using (StreamWriter writer = new StreamWriter(_logName,true))
           {
               writer.WriteLine(DateTime.Now + ": "+ msg);
           }
       }
    }
}
