using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace jvk.util
{
    public class ProcessRun
    {
        public delegate void ProcessControlFlow(Process p);

        public string fileName;
        public string args;
        public bool redirectStandardError;
        public bool redirectStandardInput;
        public bool redirectStandardOutput;
        public ProcessControlFlow controlFlow;
        public bool useShellExecute;
        public bool createNoWindow;

        Process _procRef;
        private int _nExitCode = -1;
        private bool _isStopped = false;
        public int ExitCode {
            get {
                if (_isStopped)
                {
                    return _nExitCode;
                }
                return 0;
            }
        }


        public ProcessRun()
        {
            _isStopped = false;
            controlFlow = drain;
            createNoWindow = true;
        }


        public void drain(Process p)
        {
            var stdout = p.StandardOutput;
            Console.WriteLine();
            Console.WriteLine("{0} {1}", fileName, args);
            Console.WriteLine("<code>");
            while (true)
            {
                string line = stdout.ReadLine();
                if (null == line) break;
                Console.WriteLine(line);
            }
            Console.WriteLine("</code>");
        }

        public static void waitFinish(Process p)
        {
            while (!p.HasExited)
            {
                System.Threading.Thread.Sleep(5);
            }
        }


        public void run()
        {
            if (_procRef != null)
            {
                throw new ArgumentException("Invalid use of a executed ProcessRun");
            }

            ProcessStartInfo pi = new ProcessStartInfo();

            pi.FileName = fileName;
            pi.Arguments = args;
            pi.RedirectStandardError = redirectStandardError;
            pi.RedirectStandardInput = redirectStandardInput;
            pi.RedirectStandardOutput = redirectStandardOutput;
            pi.UseShellExecute = useShellExecute;
            pi.CreateNoWindow = createNoWindow;

            try
            {
                var p = Process.Start(pi);
                _procRef = p;
                p.Exited += P_Exited;
                controlFlow(p);
            }
            catch (Exception ex)
            {
                string nm = pi.FileName;
                if (File.Exists(nm))
                {
                    throw ex;
                }
                throw new FileNotFoundException(nm + "\r\n\r\n" + ex.Message, ex);
            }
        }

        private void P_Exited(object sender, EventArgs e)
        {
            _isStopped = true;
            _nExitCode = _procRef.ExitCode;
        }

        public static string quoted(string v)
        {
            return String.Format("{1}{0}{1}", v, '\"');
        }

        public struct ArgBuilder
        {
            StringBuilder sb;

            public static ArgBuilder makeOne()
            {
                ArgBuilder b;
                b.sb = new StringBuilder();
                return b;
            }

            public ArgBuilder append(string arg)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(arg);
                return this;
            }

            public ArgBuilder appendQuoted(string arg)
            {
                return append(quoted(arg));
            }

            public string export()
            {
                return sb.ToString();
            }
        }

    } // end - class ProcessRun
}
