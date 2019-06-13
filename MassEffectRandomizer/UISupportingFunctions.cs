﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using MahApps.Metro.Controls.Dialogs;
using MassEffectRandomizer.Classes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using Serilog;
using SlavaGu.ConsoleAppLauncher;

namespace MassEffectRandomizer
{
    public partial class MainWindow
    {
        private BackgroundWorker BackupWorker;

        private async void BackupGame()
        {
            Log.Information("Start of UI thread BackupGame() for Mass Effect");
            ALOTVersionInfo info = Utilities.GetInstalledALOTInfo();
            if (info != null)
            {
                //Game is modified via ALOT flag
                if (info.ALOTVER > 0)
                {
                    Log.Warning("ALOT is installed. Backup of ALOT installed game is not allowed.");
                    await this.ShowMessageAsync("ALOT is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                else if (info.MEUITMVER > 0)
                {
                    Log.Warning("MEUITM is installed. Backup of MEUITM installed game is not allowed.");
                    await this.ShowMessageAsync("MEUITM is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                else
                {
                    Log.Warning("ALOT or MEUITM is installed. Backup of ALOT or MEUITM installed game is not allowed.");
                    await this.ShowMessageAsync("ALOT is installed", "You cannot backup an installation that has ALOT already installed. If you have a backup, you can restore it by clicking the game backup button in the Settings menu. Otherwise, delete your game folder and redownload it.");
                }
                return;
            }

            string gamedir = Utilities.GetGamePath();
            if (gamedir == null)
            {
                //exe is missing? not sure how this could be null at this point.
                Log.Error("Game directory is null - has the filesystem changed since the app was booted?");
                await this.ShowMessageAsync("Cannot determine game path", "The game path cannot be determined - this is most likely a bug. Please report this issue to the developers on Discord (Settings -> Report an issue).");
                return;
            }

            var openFolder = new CommonOpenFileDialog();
            openFolder.IsFolderPicker = true;
            openFolder.Title = "Select backup destination";
            openFolder.AllowNonFileSystemItems = false;
            openFolder.EnsurePathExists = true;
            if (openFolder.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            Log.Information("User has chosen directory for backup destination: " + openFolder.FileName);

            var dir = openFolder.FileName;
            if (!Directory.Exists(dir))
            {
                Log.Error("User attempting to backup to directory that doesn't exist. Explorer can cause this issue sometimes by allow selection of previous directory.");
                await this.ShowMessageAsync("Directory does not exist", "The backup destination directory does not exist: " + dir);
                return;
            }
            if (!Utilities.IsDirectoryEmpty(dir))
            {
                Log.Warning("User attempting to backup to directory that is not empty");
                await this.ShowMessageAsync("Directory is not empty", "The backup destination directory must be empty.");
                return;
            }

            if (Utilities.IsSubfolder(gamedir, dir))
            {
                Log.Warning("User attempting to backup to subdirectory of backup source - not allowed because this will cause infinite recursion and will be deleted when restores are attempted");
                await this.ShowMessageAsync("Directory is subdirectory of game", "Backup directories cannot be subfolders of the game directory. Choose a different directory.");
                return;
            }
            currentProgressDialogController = backupRestoreController = await this.ShowProgressAsync("Backing up Mass Effect", "Mass Effect is being backed up, please wait...", true);
            currentProgressDialogController.SetIndeterminate();

            BackupWorker = new BackgroundWorker();
            BackupWorker.DoWork += VerifyAndBackupGame;
            BackupWorker.WorkerReportsProgress = true;
            BackupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            BackupWorker.RunWorkerCompleted += BackupCompleted;
            BackupWorker.RunWorkerAsync(dir);
            //ShowStatus("Verifying game data before backup", 4000);
            // get all the directories in selected dirctory
        }

        private async void RestoreGame()
        {
            currentProgressDialogController = backupRestoreController = await this.ShowProgressAsync("Preparing to restore Mass Effect", "Preparing to restore Mass Effect, please wait...", true);
            currentProgressDialogController.SetIndeterminate();

            BackupWorker = new BackgroundWorker();
            BackupWorker.DoWork += RestoreGame;
            BackupWorker.WorkerReportsProgress = true;
            BackupWorker.ProgressChanged += BackupWorker_ProgressChanged;
            BackupWorker.RunWorkerCompleted += RestoreCompleted;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this);
            TaskbarManager.Instance.SetProgressValue(0, 0);
            BackupWorker.RunWorkerAsync();
        }

        private async void RestoreCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            await currentProgressDialogController?.CloseAsync();
            if (e.Result != null)
            {
                bool result = (bool)e.Result;
                if (result)
                {
                    await this.ShowMessageAsync("Restore completed", "Mass Effect has been restored from backup.");
                }
                else
                {
                    await this.ShowMessageAsync("Restore failed", "Mass Effect was not restored from backup. Check the logs for more information.");
                }
            }
        }

        public const string REGISTRY_KEY = @"SOFTWARE\ALOTAddon"; //Backup is shared with ALOT Installer

        private async void BackupCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, this);
            string destPath = (string)e.Result;
            if (destPath != null)
            {
                Utilities.WriteRegistryKey(Registry.CurrentUser, REGISTRY_KEY, "ME1VanillaBackupLocation", destPath);
            }

            //Backup completed, update UI
            await currentProgressDialogController?.CloseAsync();
            if (e.Result != null)
            {
                string result = (string)e.Result;
                if (result != null)
                {
                    BackupRestoreText = "Restore";
                    BackupRestore_Button.ToolTip = "Click to restore game from\n" + result;
                    await this.ShowMessageAsync("Backup completed", "Mass Effect has been backed up. You can restore this backup by clicking the Restore button.");
                }
                else
                {
                    await this.ShowMessageAsync("Backup failed", "Mass Effect was unable to be fully backed up. Check the logs for more information.");
                }
            }
        }

        private async void RestoreGame(object sender, DoWorkEventArgs e)
        {
            string gamePath = Utilities.GetGamePath();
            string backupPath = Utilities.GetGameBackupPath();
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_PROGRESSBAR_INDETERMINATE, true));
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_CURRENTPROGRESSDIALOG_TITLE, "Deleting existing game installation"));
            if (Directory.Exists(gamePath))
            {
                Log.Information("Deleting existing game directory: " + gamePath);
                try
                {
                    bool deletedDirectory = Utilities.DeleteFilesAndFoldersRecursively(gamePath);
                    if (deletedDirectory != true)
                    {
                        BackupWorker.ReportProgress(0, new ThreadCommand(RESTORE_FAILED_COULD_NOT_DELETE_FOLDER));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception deleting game directory: " + gamePath + ": " + ex.Message);
                }
            }
            else
            {
                Log.Error("Game directory not found! Was it removed while the app was running?");
            }

            Log.Information("Reverting lod settings");
            string exe = Utilities.ExtractInternalStaticExecutable("MassEffectModderNoGui.exe", true);
            string args = "--remove-lods --gameid 1";
            Utilities.runProcess(exe, args);

            string iniPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\BioWare\Mass Effect\Config\BIOEngine.ini";
            if (File.Exists(iniPath))
            {
                Log.Information("Reverting Indirect Sound ini fix for ME1");
                IniFile engineConf = new IniFile(iniPath);
                engineConf.DeleteKey("DeviceName", "ISACTAudio.ISACTAudioDevice");
            }

            if (Utilities.IsDirectoryWritable(Directory.GetParent(gamePath).FullName))
            {
                Directory.CreateDirectory(gamePath);
            }
            else
            {
                //Must have admin rights.
                Log.Information("We need admin rights to create this directory");
                exe = Utilities.ExtractInternalStaticExecutable("PermissionsGranter.exe", true); //Need to extract permissions granter
                args = "\"" + System.Security.Principal.WindowsIdentity.GetCurrent().Name + "\" -create-directory \"" + gamePath.TrimEnd('\\') + "\"";
                int result = Utilities.runProcessAsAdmin(exe, args);
                if (result == 0)
                {
                    Log.Information("Elevated process returned code 0, restore directory is hopefully writable now.");
                }
                else if (result == Utilities.WIN32_EXCEPTION_ELEVATED_CODE)
                {
                    Log.Information("Elevated process returned exception code, user probably declined prompt");

                    e.Result = false;
                    return;
                }
                else
                {
                    Log.Error("Elevated process returned code " + result + ", directory likely is not writable");
                    e.Result = false;
                    return;
                }
            }

            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_CURRENTPROGRESSDIALOG_TITLE, "Restoring game from backup"));
            if (gamePath != null)
            {
                Log.Information("Copying backup to game directory: " + backupPath + " -> " + gamePath);
                CopyDir.CopyAll_ProgressBar(new DirectoryInfo(backupPath), new DirectoryInfo(gamePath), BackupWorker, "Restoring Mass Effect...", -1, 0);
                Log.Information("Restore of game data has completed");
            }
            e.Result = true;
        }

        public ConsoleApp BACKGROUND_MEM_PROCESS = null;
        private List<string> BACKGROUND_MEM_PROCESS_ERRORS;
        private List<string> BACKGROUND_MEM_PROCESS_PARSED_ERRORS;

        private void VerifyAndBackupGame(object sender, DoWorkEventArgs e)
        {
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_ADDONUI_CURRENTTASK, "Calculating space requirements..."));
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_PROGRESSBAR_INDETERMINATE, true));


            //Get size
            string backupPath = (string)e.Argument;

            long dirsize = Utilities.DirSize(new DirectoryInfo(Utilities.GetGamePath()));
            dirsize = Convert.ToInt64(dirsize * 1.1);


            ulong freeBytes;
            ulong diskSize;
            ulong totalFreeBytes;
            bool gotFreeSpace = Utilities.GetDiskFreeSpaceEx(backupPath, out freeBytes, out diskSize, out totalFreeBytes);

            if ((long)freeBytes < dirsize)
            {
                Log.Error("Not enough free space on drive for backup. We need " + ByteSize.FromBytes(dirsize) + " but we only have " + ByteSize.FromBytes(freeBytes));
                BackupWorker.ReportProgress(0, new ThreadCommand(SHOW_DIALOG, new KeyValuePair<string, string>("Not enough free space on drive", "There is not enough space on " + Path.GetPathRoot(backupPath) + " to store a backup.\nFree space: " + ByteSize.FromBytes(freeBytes) + "\nRequired space: " + ByteSize.FromBytes(dirsize))));
                e.Result = null;
                return;
            }



            //verify vanilla
            Log.Information("Verifying game: Mass Effect");
            string exe = Utilities.ExtractInternalStaticExecutable("MassEffectModderNoGui.exe", true);
            string args = "--check-game-data-vanilla --gameid 1 --ipc";
            List<string> acceptedIPC = new List<string>();
            acceptedIPC.Add("TASK_PROGRESS");
            acceptedIPC.Add("ERROR");
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_CURRENTPROGRESSDIALOG_DESCRIPTION, "Verifying game data before backup..."));
            BackupWorker.ReportProgress(0, new ThreadCommand(UPDATE_CURRENTPROGRESSDIALOG_SETMAXIMUM, 100));

            runMEM_BackupAndBuild(exe, args, BackupWorker, acceptedIPC);
            while (BACKGROUND_MEM_PROCESS.State == AppState.Running)
            {
                Thread.Sleep(250);
            }
            int backupVerifyResult = BACKGROUND_MEM_PROCESS.ExitCode ?? 1;
            if (backupVerifyResult != 0)
            {
                string modified = "";
                string gameDir = Utilities.GetGamePath();
                foreach (String error in BACKGROUND_MEM_PROCESS_ERRORS)
                {
                    modified += "\n - " + error;
                    //.Remove(0, gameDir.Length + 1);
                }
                Log.Warning("Backup verification failed. Allowing user to choose to continue or not");
                ThreadCommandDialogOptions tcdo = new ThreadCommandDialogOptions();
                tcdo.signalHandler = new EventWaitHandle(false, EventResetMode.AutoReset);
                tcdo.title = "Game is modified";
                tcdo.message = "Mass Effect has files that do not match what is in the MEM vanilla database.\nYou can continue to back this installation up, but it may not be truly unmodified, and the restored copy may not work." + modified;
                tcdo.NegativeButtonText = "Abort";
                tcdo.AffirmativeButtonText = "Continue";
                BackupWorker.ReportProgress(0, new ThreadCommand(SHOW_DIALOG_YES_NO, tcdo));
                tcdo.signalHandler.WaitOne();
                //Thread resumes
                if (!CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS)
                {
                    e.Result = null;
                    return;
                }
                else
                {
                    Log.Warning("User continuing even with non-vanilla backup.");
                    CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS = false; //reset
                }
            }
            else
            {
                Log.Information("Backup verification passed - no issues.");
            }
            string gamePath = Utilities.GetGamePath();
            string[] ignoredExtensions = { ".wav", ".pdf", ".bak" };
            if (gamePath != null)
            {
                Log.Information("Creating backup... Only errors will be reported.");
                try
                {
                    CopyDir.CopyAll_ProgressBar(new DirectoryInfo(gamePath), new DirectoryInfo(backupPath), BackupWorker, "Backing up Mass Effect...", -1, 0, ignoredExtensions);
                }
                catch (Exception ex)
                {
                    Log.Error("Error creating backup:");
                    Log.Error(App.FlattenException(ex));
                    BackupWorker.ReportProgress(0, new ThreadCommand(SHOW_DIALOG, new KeyValuePair<string, string>("Backup failed", "Backup of Mass Effect failed. An error occured during the copy process. The error message was: " + ex.Message + ".\nSome files may have been copied, but this backup is not usable. You can delete the folder you were backing up files into.\nReview the log for more information.")));
                    e.Result = null;
                    return;
                }
                Log.Information("Backup copy created");
            }
            e.Result = backupPath;
        }

        private async void BackupWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is null)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, this);
                TaskbarManager.Instance.SetProgressValue(e.ProgressPercentage, 100);
            }
            else
            {
                ThreadCommand tc = (ThreadCommand)e.UserState;
                switch (tc.Command)
                {
                    case RESTORE_FAILED_COULD_NOT_DELETE_FOLDER:
                        Log.Error("Restore has failed - could not delete existing installation. Some may be missing - consider this game installation ruined and requires a restore now.");
                        await this.ShowMessageAsync("Restore failed", "Could not delete the existing game directory. This is usually due to something still open (such as the game), or running something from within the game folder. Close other programs and try again.");
                        return;
                    case UPDATE_CURRENTPROGRESSDIALOG_DESCRIPTION:
                        currentProgressDialogController?.SetMessage((string)tc.Data);
                        break;
                    case UPDATE_CURRENTPROGRESSDIALOG_SETINDETERMINATE:
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                        currentProgressDialogController?.SetIndeterminate();
                        break;
                    case UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE:
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);

                        currentProgressDialogController?.SetProgress((int)tc.Data);
                        var max = currentProgressDialogController?.Maximum;
                        if (max.HasValue)
                        {
                            TaskbarManager.Instance.SetProgressValue((int)tc.Data, (int)max.Value);

                        }

                        break;
                    case UPDATE_CURRENTPROGRESSDIALOG_TITLE:
                        currentProgressDialogController?.SetTitle((string)tc.Data);
                        break;
                    case CLOSE_CURRENT_DIALOG:
                        await currentProgressDialogController?.CloseAsync();
                        break;
                    case ERROR_OCCURED:
                        //Build_ProgressBar.IsIndeterminate = false;
                        //ProgressBarValue = 0;
                        //await this.ShowMessageAsync("Error building Addon MEM Package", "An error occured building the addon. The logs will provide more information. The error message given is:\n" + (string)tc.Data);
                        break;
                    case SHOW_DIALOG:
                        KeyValuePair<string, string> messageStr = (KeyValuePair<string, string>)tc.Data;
                        await this.ShowMessageAsync(messageStr.Key, messageStr.Value);
                        break;
                    case "UPDATE_CURRENTPROGRESSDIALOG_SETMAXIMUM":
                        if (currentProgressDialogController != null) currentProgressDialogController.Maximum = (int)tc.Data;
                        break;
                    case SHOW_DIALOG_YES_NO:
                        ThreadCommandDialogOptions tcdo = (ThreadCommandDialogOptions)tc.Data;
                        MetroDialogSettings settings = new MetroDialogSettings();
                        settings.NegativeButtonText = tcdo.NegativeButtonText;
                        settings.AffirmativeButtonText = tcdo.AffirmativeButtonText;
                        MessageDialogResult result = await this.ShowMessageAsync(tcdo.title, tcdo.message, MessageDialogStyle.AffirmativeAndNegative, settings);
                        if (result == MessageDialogResult.Negative)
                        {
                            CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS = false;
                        }
                        else
                        {
                            CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS = true;
                        }

                        tcdo.signalHandler.Set();
                        break;

                }
            }
        }

        private void runMEM_BackupAndBuild(string exe, string args, BackgroundWorker worker, List<string> acceptedIPC = null)
        {
            Log.Information("Running process: " + exe + " " + args);
            BACKGROUND_MEM_PROCESS = new ConsoleApp(exe, args);
            BACKGROUND_MEM_PROCESS_ERRORS = new List<string>();
            BACKGROUND_MEM_PROCESS_PARSED_ERRORS = new List<string>();
            BACKGROUND_MEM_PROCESS.ConsoleOutput += (o, args2) =>
            {
                string str = args2.Line;
                if (str.StartsWith("[IPC]", StringComparison.Ordinal))
                {
                    string command = str.Substring(5);
                    int endOfCommand = command.IndexOf(' ');
                    if (endOfCommand >= 0)
                    {
                        command = command.Substring(0, endOfCommand);
                    }
                    if (acceptedIPC == null || acceptedIPC.Contains(command))
                    {
                        string param = str.Substring(endOfCommand + 5).Trim();
                        switch (command)
                        {
                            case "TASK_PROGRESS":
                                int percentInt = Convert.ToInt32(param);
                                worker.ReportProgress(0, new ThreadCommand(UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE, percentInt));
                                break;
                            case "PROCESSING_FILE":
                                //unsure if I want to use this in MER
                                //worker.ReportProgress(completed, new ThreadCommand(UPDATE_ADDONUI_CURRENTTASK, param));
                                break;
                            case "ERROR":
                                Log.Error("Error IPC from MEM: " + param);
                                BACKGROUND_MEM_PROCESS_ERRORS.Add(param);
                                break;

                            default:
                                Log.Information("Unknown IPC command: " + command);
                                break;
                        }
                    }
                }
                else
                {
                    if (str.Trim() != "")
                    {
                        Log.Information("Realtime Process Output: " + str);
                    }
                }
            };
            BACKGROUND_MEM_PROCESS.Run();
        }

        public const string UPDATE_ADDONUI_CURRENTTASK = "UPDATE_OPERATION_LABEL";
        public const string HIDE_TIPS = "HIDE_TIPS";
        public const string UPDATE_PROGRESSBAR_INDETERMINATE = "SET_PROGRESSBAR_DETERMINACY";
        public const string INCREMENT_COMPLETION_EXTRACTION = "INCREMENT_COMPLETION_EXTRACTION";
        public const string SHOW_DIALOG = "SHOW_DIALOG";
        public const string ERROR_OCCURED = "ERROR_OCCURED";
        private const string SHOW_DIALOG_YES_NO = "SHOW_DIALOG_YES_NO";
        public const string RESTORE_FAILED_COULD_NOT_DELETE_FOLDER = "RESTORE_FAILED_COULD_NOT_DELETE_FOLDER";
        public const string UPDATE_CURRENTPROGRESSDIALOG_DESCRIPTION = "UPDATE_CURRENTPROGRESSDIALOG_DESCRIPTION";
        public const string UPDATE_CURRENTPROGRESSDIALOG_SETINDETERMINATE = "UPDATE_CURRENTPROGRESSDIALOG_SETINDETERMINATE";
        public const string UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE = "UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE";
        public const string CLOSE_CURRENT_DIALOG = "CLOSE_CURRENT_DIALOG";
        public const string UPDATE_CURRENTPROGRESSDIALOG_TITLE = "UPDATE_CURRENTPROGRESSDIALOG_TITLE";
        public const string UPDATE_CURRENTPROGRESSDIALOG_SETMAXIMUM = "UPDATE_CURRENTPROGRESSDIALOG_SETMAXIMUM";
        private bool CONTINUE_BACKUP_EVEN_IF_VERIFY_FAILS = false;
        private ProgressDialogController backupRestoreController;
        private ProgressDialogController currentProgressDialogController;
    }

    /// <summary>
    /// Class for passing data between threads
    /// </summary>
    public class ThreadCommand
    {
        /// <summary>
        /// Creates a new thread command object with the specified command and data object. This constructori s used for passing data to another thread. The receiver will need to read the command then cast the data.
        /// </summary>
        /// <param name="command">command for this thread communication.</param>
        /// <param name="data">data to pass to another thread</param>
        public ThreadCommand(string command, object data)
        {
            this.Command = command;
            this.Data = data;
        }

        /// <summary>
        /// Creates a new thread command object with the specified command. This constructor is used for notifying other threads something has happened.
        /// </summary>
        /// <param name="command">command for this thread communication.</param>
        /// <param name="data">data to pass to another thread</param>
        public ThreadCommand(string command)
        {
            this.Command = command;
        }

        public string Command;
        public object Data;
    }

    internal class ThreadCommandDialogOptions
    {
        public EventWaitHandle signalHandler;
        public string title;
        public string message;
        public string AffirmativeButtonText;
        public string NegativeButtonText;
    }
    class CopyDir
    {
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target, BackgroundWorker worker = null)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (fi.FullName.EndsWith(".txt"))
                {
                    continue; //don't copy logs
                }
                //Log.Information(@"Copying {0}\{1}", target.FullName, fi.Name);
                try
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }
                catch (Exception e)
                {
                    Log.Error("Error copying file: " + fi + " -> " + Path.Combine(target.FullName, fi.Name));
                    throw e;
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static int CopyAll_ProgressBar(DirectoryInfo source, DirectoryInfo target, BackgroundWorker worker, string messagePrefix, int total, int done, string[] ignoredExtensions = null)
        {
            if (total == -1)
            {
                //calculate number of files
                total = Directory.GetFiles(source.FullName, "*.*", SearchOption.AllDirectories).Length;
                worker.ReportProgress(0, new ThreadCommand(MainWindow.UPDATE_CURRENTPROGRESSDIALOG_SETMAXIMUM, total));
            }

            int numdone = done;
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                if (ignoredExtensions != null)
                {
                    bool skip = false;
                    foreach (string str in ignoredExtensions)
                    {
                        if (fi.Name.ToLower().EndsWith(str))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                    {
                        numdone++;
                        worker.ReportProgress(0, new ThreadCommand(MainWindow.UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE, numdone));
                        //worker.ReportProgress((int)((numdone * 1.0 / total) * 100.0));
                        continue;
                    }
                }
                string displayName = fi.Name;
                string path = Path.Combine(target.FullName, fi.Name);

                //Todo: Update description of box
                //worker.ReportProgress(done, new ThreadCommand(UPDATE_ADDONUI_CURRENTTASK, displayName));
                try
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }
                catch (Exception e)
                {
                    Log.Error("Error copying file: " + fi + " -> " + Path.Combine(target.FullName, fi.Name));
                    throw e;
                }
                // Log.Information(@"Copying {0}\{1}", target.FullName, fi.Name);
                numdone++;
                worker.ReportProgress(0, new ThreadCommand(MainWindow.UPDATE_CURRENTPROGRESSDIALOG_SETPROGRESSVALUE, numdone));
                if (messagePrefix != null)
                {
                    string message = $"{messagePrefix}\n\n{numdone} of {total} files copied";
                    worker.ReportProgress(0, new ThreadCommand(MainWindow.UPDATE_CURRENTPROGRESSDIALOG_DESCRIPTION, message));
                }
                //worker.ReportProgress((int)((numdone * 1.0 / total) * 100.0));
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                numdone = CopyAll_ProgressBar(diSourceSubDir, nextTargetSubDir, worker, messagePrefix, total, numdone);
            }
            return numdone;
        }



        // Output will vary based on the contents of the source directory.
    }
}