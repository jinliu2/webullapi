using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WeBullAPI {

    public class functions {

		public static void cc_logstatus(string message, ConsoleColor bgclr, ConsoleColor fgclr, double elapsed = 0) {
            webull.curtime = DateTime.Now.ToString("hh:mm:ss.fff tt");
            Console.BackgroundColor = bgclr;
            Console.ForegroundColor = fgclr;
            Console.Write((Constants.DEBUG ? "DEBUG| " + Math.Round(elapsed,2).ToString().PadLeft(10) + " ms | " : "") + webull.curtime + " | " + message);
            Console.ResetColor();
            Console.WriteLine();
            logstatus((Constants.DEBUG ? "DEBUG| " + Math.Round(elapsed, 2).ToString().PadLeft(9 - Math.Round(elapsed, 2).ToString().Length) + " ms | " : "") + DateTime.Now + " | " + message, webull.logfile);
		}

        public static void outlog(string msg) {
            webull.curtime = DateTime.Now.ToString("hh:mm:ss.fff tt");
            Console.WriteLine(webull.curtime + " | " + msg + "");
            logstatus(DateTime.Now + " | " + msg + "", webull.logfile);
        }

		public static void logstatus(string strLogText, string dlog) {
			StreamWriter log;
            string folderpath = Path.GetDirectoryName(dlog);
			if ( !Directory.Exists( folderpath ) ) {
				try {
					Directory.CreateDirectory( folderpath );
				} catch (Exception ex) {
                    Console.WriteLine(ex.Message);
					return;
				}
			}
			if (!File.Exists(dlog)) {
                log = new StreamWriter(dlog);
			} else {
                log = File.AppendText(dlog);
			}
			log.WriteLine(strLogText);
			log.Close();
		}
    }
}
