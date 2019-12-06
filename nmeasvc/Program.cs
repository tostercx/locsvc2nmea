using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO;
using System.Configuration.Install;
using System.Threading;
using System.IO.Ports;

namespace nmeasvc
{
    //*
    class Program : ServiceBase
    {
        Thread mainThread;
        bool serviceStopping = false;

        // make sure the service reboots on crash
        static void SetRecoveryOptions(string serviceName)
        {
            int exitCode;
            using (var process = new Process())
            {
                var startInfo = process.StartInfo;
                startInfo.FileName = "sc";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // tell Windows that the service should restart if it fails
                startInfo.Arguments = string.Format("failure \"{0}\" reset= 0 actions= restart/5000", serviceName);

                process.Start();
                process.WaitForExit();

                exitCode = process.ExitCode;
            }

            if (exitCode != 0)
                throw new InvalidOperationException();
        }

        // service start
        protected override void OnStart(string[] args)
        {
            mainThread = new Thread(MainLoop);
            mainThread.Start();
        }

        // service stop
        protected override void OnStop()
        {
            serviceStopping = true;
        }

        public void MainLoop()
        {
            var gpsPort = new SerialPort("COM49", 4800);
            gpsPort.ReadTimeout = 1500;
            gpsPort.WriteTimeout = 1500;

            var gps = new Gps();

            while (!serviceStopping)
            {
                try
                {
                    if (gpsPort.IsOpen)
                        gpsPort.Close();

                    gpsPort.Open();
                    var updateTime = DateTime.Now;

                    while (!serviceStopping)
                    {
                        try
                        {
                            var lastTime = gps.GetLocation().localTime;

                            if (lastTime != updateTime)
                                updateTime = lastTime;
                            else if ((DateTime.Now - updateTime).TotalSeconds > 5 * 60) // reboot if no updates
                                gps.Reboot();

                            gpsPort.Write(gps.GetNmea());
                        }
                        catch { break; }
                        Thread.Sleep(500); // give a location update every .5s
                    }
                }
                catch { Thread.Sleep(10000); } // on port open failure retry after 10s
            }
        }

        static void Main(string[] args)
        {
            // cd to service exe dir
            var cd = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Directory.SetCurrentDirectory(cd);

            // if running as a service start
            if (!Environment.UserInteractive)
            {
                Run(new Program());
            }
            // else assume user wants to install
            else
            {
                var serviceName = WindowsServiceInstaller.serviceName;
                Console.WriteLine("Performing setup...");

                // stop
                try
                {
                    var srv = new ServiceController(serviceName);
                    srv.Stop();
                }
                catch { }

                // uninstall
                try
                {
                    Console.WriteLine(" ------------------- Uninstalling any previos services ------------------- ");
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", Process.GetCurrentProcess().MainModule.FileName });
                }
                catch { }

                // reinstall
                try
                {
                    Console.WriteLine(" ------------------- Installing " + serviceName + " service ------------------- ");
                    ManagedInstallerClass.InstallHelper(new string[] { Process.GetCurrentProcess().MainModule.FileName });
                }
                catch
                {
                    Console.WriteLine("Something went wrong while installing the service...");
                }

                // run
                Console.WriteLine(" ------------------- Starting " + serviceName + " service ------------------- ");
                try
                {
                    using (var srv = new ServiceController(serviceName))
                    {
                        int tries = 5;
                        srv.Start();

                        // echo service status 5x to see if it's running properly
                        while (tries-- > 0)
                        {
                            Thread.Sleep(1000);
                            Console.WriteLine("Status: " + srv.Status);
                        }
                    }

                    SetRecoveryOptions(serviceName);
                }
                catch
                {
                    Console.WriteLine("Status: couldn't send start command (not installed?)");
                }
            }
        }
    }
    //*/

    /*
    class Program
    {
        public static void Main()
        {
            var serviceStopping = false;
            var gpsPort = new SerialPort("COM49", 4800);
            gpsPort.ReadTimeout = 1500;
            gpsPort.WriteTimeout = 1500;

            var gps = new Gps();

            while (!serviceStopping)
            {
                try
                {
                    if (gpsPort.IsOpen)
                        gpsPort.Close();

                    gpsPort.Open();
                    var updateTime = DateTime.Now;

                    while (!serviceStopping)
                    {
                        try
                        {
                            var lastTime = gps.GetLocation().localTime;

                            if (lastTime != updateTime)
                                updateTime = lastTime;
                            else if ((DateTime.Now - updateTime).TotalSeconds > 30) // reboot if no updates for 30s
                                gps.Reboot();

                            gpsPort.Write(gps.GetNmea());
                        }
                        catch (Exception e) { Console.WriteLine(e.Message);  break; }
                        Thread.Sleep(500); // give a location update every .5s
                    }
                }
                catch (Exception e) { Console.WriteLine(e.Message); Thread.Sleep(10000); } // on port open failure retry after 10s
            }
        }

    }
    //*/
}
