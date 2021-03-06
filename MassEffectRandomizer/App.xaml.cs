﻿using CommandLine;
using MassEffectRandomizer.Classes;
using Serilog;
using Serilog.Sinks.RollingFile.Extension;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MassEffectRandomizer
{


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal const string REGISTRY_KEY = @"Software\MassEffectRandomizer";
        internal const string BACKUP_REGISTRY_KEY = @"Software\ALOTAddon"; //Shared. Do not change
        public static string LogDir;
        internal static string MainThemeColor = "Violet";
        private static bool POST_STARTUP = false;
        public const string DISCORD_INVITE_LINK = "https://discord.gg/s8HA6dc";
        public const string ME2R_LINK = "https://www.nexusmods.com/masseffect2/mods/364";
        [STAThread]
        public static void Main()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            catch (Exception e)
            {
                OnFatalCrash(e);
                throw e;
            }
        }

        public App() : base()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string exePath = assembly.Location;
            string exeFolder = Directory.GetParent(exePath).ToString();
            LogDir = Path.Combine(Utilities.GetAppDataFolder(), "logs");


            string[] args = Environment.GetCommandLineArgs();
            Parsed<Options> parsedCommandLineArgs = null;
            string updateDestinationPath = null;

            #region Update boot
            if (args.Length > 1)
            {
                var result = Parser.Default.ParseArguments<Options>(args);
                if (result.GetType() == typeof(Parsed<Options>))
                {
                    //Parsing succeeded - have to do update check to keep logs in order...
                    parsedCommandLineArgs = (Parsed<Options>)result;
                    if (parsedCommandLineArgs.Value.UpdateDest != null)
                    {
                        if (File.Exists(parsedCommandLineArgs.Value.UpdateDest))
                        {
                            updateDestinationPath = parsedCommandLineArgs.Value.UpdateDest;
                        }
                        if (parsedCommandLineArgs.Value.BootingNewUpdate)
                        {
                            Thread.Sleep(1000); //Delay boot to ensure update executable finishes
                            try
                            {
                                string updateFile = Path.Combine(exeFolder, "MassEffectRandomizer-Update.exe");
                                if (File.Exists(updateFile))
                                {
                                    File.Delete(updateFile);
                                    Log.Information("Deleted staged update");
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Warning("Unable to delete staged update: " + e.ToString());
                            }
                        }
                    }
                }
            }
            #endregion

            Log.Logger = new LoggerConfiguration()
      .WriteTo.SizeRollingFile(Path.Combine(App.LogDir, "applog.txt"),
              retainedFileDurationLimit: TimeSpan.FromDays(7),
              fileSizeLimitBytes: 1024 * 1024 * 10) // 10MB
#if DEBUG
      .WriteTo.Debug()
#endif
      .CreateLogger();
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            POST_STARTUP = true;
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            Log.Information("===========================================================================");
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Log.Information("Mass Effect Randomizer " + version);
            Log.Information("Application boot: " + DateTime.UtcNow.ToString());

            #region Update mode boot
            if (updateDestinationPath != null)
            {
                Log.Information(" >> In update mode. Update destination: " + updateDestinationPath);
                int i = 0;
                while (i < 8)
                {

                    i++;
                    try
                    {
                        Log.Information("Applying update");
                        File.Copy(assembly.Location, updateDestinationPath, true);
                        Log.Information("Update applied, restarting...");
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Error("Error applying update: " + e.Message);
                        if (i < 8)
                        {
                            Thread.Sleep(1000);
                            Log.Warning("Attempt #" + (i + 1));
                        }
                        else
                        {
                            Log.Fatal("Unable to apply update after 8 attempts. We are giving up.");
                            MessageBox.Show("Update was unable to apply. See the application log for more information. If this continues to happen please come to the ME3Tweaks discord, or download a new copy from GitHub.");
                            Environment.Exit(1);
                        }
                    }
                }
                Log.Information("Rebooting into normal mode to complete update");
                ProcessStartInfo psi = new ProcessStartInfo(updateDestinationPath);
                psi.WorkingDirectory = updateDestinationPath;
                psi.Arguments = "--completing-update";
                Process.Start(psi);
                Environment.Exit(0);
                Current.Shutdown();
            }
            #endregion
            System.Windows.Controls.ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control),
           new FrameworkPropertyMetadata(true));

            Log.Information("Loading property lookup DB");
            ME1UnrealObjectInfo.loadfromJSON();

            //try
            //{
            //    var application = new App();
            //    application.InitializeComponent();
            //    application.Run();
            //}
            //catch (Exception e)
            //{
            //    OnFatalCrash(e);
            //    throw e;
            //}
        }

        /// <summary>
        /// Called when an unhandled exception occurs. This method can only be invoked after startup has completed. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Exception to process</param>
        static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("Mass Effect Randomizer has crashed! This is the exception that caused the crash:");
            string st = FlattenException(e.Exception);
            Log.Fatal(errorMessage);
            Log.Fatal(st);
            //Log.Information("Forcing beta mode off before exiting...");
            //Utilities.WriteRegistryKey(Registry.CurrentUser, AlotAddOnGUI.MainWindow.REGISTRY_KEY, AlotAddOnGUI.MainWindow.SETTINGSTR_BETAMODE, 0);
            File.Create(Utilities.GetAppCrashFile());
        }

        /// <summary>
        /// Called when a fatal crash occurs. Only does something if startup has not completed.
        /// </summary>
        /// <param name="e">The fatal exception.</param>
        public static void OnFatalCrash(Exception e)
        {
            if (!POST_STARTUP)
            {
                string errorMessage = string.Format("Mass Effect Randomizer has encountered a fatal startup crash:\n" + FlattenException(e));
                File.WriteAllText(Path.Combine(Utilities.GetAppDataFolder(), "FATAL_STARTUP_CRASH.txt"), errorMessage);
            }
        }

        /// <summary>
        /// Flattens an exception into a printable string
        /// </summary>
        /// <param name="exception">Exception to flatten</param>
        /// <returns>Printable string</returns>
        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.GetType().Name + ": " + exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }


    class Options
    {
        [Option('u', "update-dest-path",
          HelpText = "Indicates where this booting instance of Mass Effect Randomizer should attempt to copy itself and reboot to")]
        public string UpdateDest { get; set; }

        [Option('c', "completing-update",
            HelpText = "Indicates that we are booting a new copy of Mass Effect Randomizer that has just been upgraded")]
        public bool BootingNewUpdate { get; set; }
    }
}

