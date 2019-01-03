﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MassEffectRandomizer.Classes;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MassEffectRandomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public static bool DEBUG_LOGGING { get; internal set; }
        public enum RandomizationMode
        {
            ERandomizationMode_SelectAny = 0,
            ERandomizationMode_Common = 1,
            ERAndomizationMode_Screed = 2
        }
        private RandomizationMode _selectedRandomizationMode;
        public RandomizationMode SelectedRandomizeMode
        {
            get { return _selectedRandomizationMode; }
            set { SetProperty(ref _selectedRandomizationMode, value); UpdateCheckboxSettings(); }
        }

        private void UpdateCheckboxSettings()
        {
            if (SelectedRandomizeMode == RandomizationMode.ERandomizationMode_Common)
            {
                RANDSETTING_GALAXYMAP_CLUSTERS = true;
                RANDSETTING_GALAXYMAP_SYSTEMS = true;
                RANDSETTING_GALAXYMAP_PLANETCOLOR = true;
                RANDSETTING_WEAPONS_STARTINGEQUIPMENT = true;
                RANDSETTING_CHARACTER_INVENTORY = true;

                //testing only
                RANDSETTING_CHARACTER_HENCH_ARCHETYPES = true;
                RANDSETTING_CHARACTER_CHARCREATOR = true;
            }
        }

        //RANDOMIZATION OPTION BINDINGS
        //Galaxy Map
        private bool _randsetting_galaxymap_planetcolor;
        public bool RANDSETTING_GALAXYMAP_PLANETCOLOR { get { return _randsetting_galaxymap_planetcolor; } set { SetProperty(ref _randsetting_galaxymap_planetcolor, value); } }

        private bool _randsetting_galaxymap_systems;
        public bool RANDSETTING_GALAXYMAP_SYSTEMS { get { return _randsetting_galaxymap_systems; } set { SetProperty(ref _randsetting_galaxymap_systems, value); } }

        private bool _randsetting_galaxymap_clusters;
        public bool RANDSETTING_GALAXYMAP_CLUSTERS { get { return _randsetting_galaxymap_clusters; } set { SetProperty(ref _randsetting_galaxymap_clusters, value); } }

        //Weapons
        private bool _randsetting_weapons_startingequipment;
        public bool RANDSETTING_WEAPONS_STARTINGEQUIPMENT { get { return _randsetting_weapons_startingequipment; } set { SetProperty(ref _randsetting_weapons_startingequipment, value); } }

        //Character
        private bool _randsetting_character_hench_archetypes;
        public bool RANDSETTING_CHARACTER_HENCH_ARCHETYPES { get { return _randsetting_character_hench_archetypes; } set { SetProperty(ref _randsetting_character_hench_archetypes, value); } }

        private bool _randsetting_character_inventory;
        public bool RANDSETTING_CHARACTER_INVENTORY { get { return _randsetting_character_inventory; } set { SetProperty(ref _randsetting_character_inventory, value); } }

        private bool _randsetting_character_charactercreator;
        public bool RANDSETTING_CHARACTER_CHARCREATOR { get { return _randsetting_character_charactercreator; } set { SetProperty(ref _randsetting_character_charactercreator, value); } }

        private bool _randsetting_character_charactercreator_skintone;
        public bool RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE { get { return _randsetting_character_charactercreator_skintone; } set { SetProperty(ref _randsetting_character_charactercreator_skintone, value); } }

        
        //END RANDOMIZE OPTION BINDINGS

        public MainWindow()
        {
            EmbeddedDllClass.ExtractEmbeddedDlls("lzo2.dll", Properties.Resources.lzo2);
            EmbeddedDllClass.ExtractEmbeddedDlls("lzo2.dll", Properties.Resources.lzo2helper);
            EmbeddedDllClass.LoadDll("lzo2.dll");
            EmbeddedDllClass.LoadDll("lzo2helper.dll");
            InitializeComponent();
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            TextBlock_AssemblyVersion.Text = "Version " + version;
            DataContext = this;
            SelectedRandomizeMode = RandomizationMode.ERandomizationMode_Common;
        }

        #region Property Changed Notification
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies listeners when given property is updated.
        /// </summary>
        /// <param name="propertyname">Name of property to give notification for. If called in property, argument can be ignored as it will be default.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        /// <summary>
        /// Sets given property and notifies listeners of its change. IGNORES setting the property to same value.
        /// Should be called in property setters.
        /// </summary>
        /// <typeparam name="T">Type of given property.</typeparam>
        /// <param name="field">Backing field to update.</param>
        /// <param name="value">New value of property.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>True if success, false if backing field and new value aren't compatible.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        public async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBackupButtonStatus();
            string me1Path = Utilities.GetGamePath();

            //int installedGames = 5;
            bool me1Installed = (me1Path != null);

            if (!me1Installed)
            {
                Log.Error("Mass Effect couldn't be found. Application will now exit.");
                await this.ShowMessageAsync("Mass Effect is not installed", "Mass Effect couldn't be found on this system. Mass Effect Randomizer only works with legitimate, official copies of Mass Effect. If you need assistance, please come to the ME3Tweaks Discord.");
                Log.Error("Exiting due to game not being found");
                Environment.Exit(1);
            }
            Log.Information("Game is installed at " + me1Path);
        }

        private void RandomizeButton_Click(object sender, RoutedEventArgs e)
        {
            Button_Randomize.Visibility = Visibility.Collapsed;
            Textblock_CurrentTask.Visibility = Visibility.Visible;
            Progressbar_Bottom.Visibility = Visibility.Visible;
            Randomizer randomizer = new Randomizer(this);
            randomizer.randomize();
        }

        private void Image_ME3Tweaks_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://me3tweaks.com");
            }
            catch (Exception ex)
            {

            }
        }

        private void Button_BackupRestore_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(System.IO.Path.Combine(Utilities.GetAppDataFolder(), "BACKED_UP")))
            {
                Utilities.Restore2DAFiles();
            }
            else
            {
                Utilities.Backup2daFiles();
            }
            UpdateBackupButtonStatus();
        }

        private void UpdateBackupButtonStatus()
        {
            if (File.Exists(System.IO.Path.Combine(Utilities.GetAppDataFolder(), "BACKED_UP")))
            {
                Button_BackupRestore.Content = "Restore 2DA files";
            }
            else
            {
                Button_BackupRestore.Content = "Backup 2DA files";
            }

        }
    }
}
