﻿using AlotAddOnGUI.classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes.RandomizationAlgorithms;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace MassEffectRandomizer.Classes
{
    class Randomizer
    {
        private const string UPDATE_RANDOMIZING_TEXT = "UPDATE_RANDOMIZING_TEXT";
        private MainWindow mainWindow;
        private BackgroundWorker randomizationWorker;




        public Randomizer(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void randomize()
        {
            randomizationWorker = new BackgroundWorker();
            randomizationWorker.DoWork += PerformRandomization;
            randomizationWorker.ProgressChanged += Randomization_ProgressChanged;
            randomizationWorker.RunWorkerCompleted += Randomization_Completed;
            randomizationWorker.WorkerReportsProgress = true;
            randomizationWorker.RunWorkerAsync();
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate, mainWindow);
        }

        private void Randomization_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, mainWindow);
            mainWindow.Textblock_CurrentTask.Text = "Randomization complete";
            mainWindow.Progressbar_Bottom.Visibility = System.Windows.Visibility.Collapsed;
            mainWindow.Button_Randomize.Visibility = System.Windows.Visibility.Visible;
        }

        private void Randomization_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ThreadCommand tc = (ThreadCommand)e.UserState;
            switch (tc.Command)
            {
                case UPDATE_RANDOMIZING_TEXT:
                    mainWindow.Textblock_CurrentTask.Text = (string)tc.Data;
                    break;
            }
        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            ME1UnrealObjectInfo.loadfromJSON();
            Random random = new Random();

            //Randomize ENGINE
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());
            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {
                    case "Music_Music":
                        //if randomizemusic
                        RandomizeMusic(export, random);
                        break;
                    case "UISounds_GuiMusic":
                        RandomizeGUISounds(export, random, "Randomizing GUI Sounds - Music", "music");
                        break;
                    case "UISounds_GuiSounds":
                        RandomizeGUISounds(export, random, "Randomizing GUI Sounds - Sounds", "snd_gui");
                        break;
                    case "MovementTables_CreatureSpeeds":
                        RandomizeMovementSpeeds(export, random);
                        break;
                    case "GalaxyMap_Cluster":
                        //RandomizeClusters(export, random);
                        break;
                    case "GalaxyMap_System":
                        //RandomizeSystems(export, random);
                        break;
                    case "GalaxyMap_Planet":
                        //RandomizePlanets(export, random);
                        break;
                    case "Characters_StartingEquipment":
                        //RandomizeStartingWeapons(export, random);
                        break;
                    case "Classes_ClassTalents":
                        int shuffleattempts = 0;
                        bool reattemptTalentShuffle = false;
                        while (reattemptTalentShuffle)
                        {
                            if (shuffleattempts > 0)
                            {
                                randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Class Talents... Attempt #" + (shuffleattempts + 1)));
                            }
                            reattemptTalentShuffle = !RandomizeTalentLists(export, random); //true if shuffle is OK, false if it failed
                            shuffleattempts++;
                        }
                        break;
                    case "LevelUp_ChallengeScalingVars":
                        //RandomizeLevelUpChallenge(export, random);
                        break;
                    case "Items_ItemEffectLevels":
                        RandomizeWeaponStats(export, random);
                        break;
                    case "Characters_Character":
                        RandomizeCharacter(export, random);
                        break;
                }
            }
            engine.save();

            //RANDOMIZE ENTRYMENU
            ME1Package entrymenu = new ME1Package(Utilities.GetEntryMenuFile());
            foreach (IExportEntry export in entrymenu.Exports)
            {
                switch (export.ObjectName)
                {
                    case "FemalePregeneratedHeads":
                    case "MalePregeneratedHeads":
                    case "BaseMaleSliders":
                    case "BaseFemaleSliders":
                        if (mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
                        {
                            RandomizePregeneratedHead(export, random);
                        }
                        break;
                    default:
                        if ((export.ClassName == "Bio2DA" || export.ClassName == "Bio2DANumberedRows") && !export.ObjectName.Contains("Default") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
                        {
                            RandomizeCharacterCreator(random, export);
                        }
                        break;

                        //RandomizeGalaxyMap(random);
                        //RandomizeGUISounds(random);
                        //RandomizeMusic(random);
                        //RandomizeMovementSpeeds(random);
                        //RandomizeCharacterCreator(random);
                        //Dump2DAToExcel();
                }
            }
            entrymenu.save();
        }
        private void RandomizeGUISounds(Random random)
        {
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());
            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {

                }
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Finishing GUI Sound Randomizing"));

            engine.save();
        }

        private void RandomizeMusic(Random random)
        {
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());
            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {
                    case "Music_Music":
                        RandomizeMusic(export, random);
                        break;
                }
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Finishing Music Randomizing"));

            engine.save();
        }

        private void RandomizeMovementSpeeds(Random random)
        {
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());
            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {
                    case "MovementTables_CreatureSpeeds":
                        RandomizeMovementSpeeds(export, random);
                        break;
                }
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Finishing Movement Speeds Randomizing"));

            engine.save();
        }

        private void RandomizeMovementSpeeds(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Movement Speeds"));

            Bio2DA cluster2da = new Bio2DA(export);
            int[] colsToRandomize = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15, 16, 17, 18, 19 };
            for (int row = 0; row < cluster2da.rowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));
                    int randvalue = random.Next(10, 1200);
                    Console.WriteLine("Movement Speed Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    cluster2da[row, colsToRandomize[i]].Data = BitConverter.GetBytes(randvalue);
                    cluster2da[row, colsToRandomize[i]].Type = Bio2DACell.TYPE_INT;
                }
            }
            cluster2da.Write2DAToExport();
        }

        private void RandomizeGalaxyMap(Random random)
        {
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());

            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {
                    case "GalaxyMap_Cluster":
                        //RandomizeClusters(export, random);
                        break;
                    case "GalaxyMap_System":
                        //RandomizeSystems(export, random);
                        break;
                    case "GalaxyMap_Planet":
                        //RandomizePlanets(export, random);
                        break;
                    case "Characters_StartingEquipment":
                        //RandomizeStartingWeapons(export, random);
                        break;
                    case "Classes_ClassTalents":
                        int shuffleattempts = 0;
                        bool reattemptTalentShuffle = false;
                        while (reattemptTalentShuffle)
                        {
                            if (shuffleattempts > 0)
                            {
                                randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Class Talents... Attempt #" + (shuffleattempts + 1)));
                            }
                            reattemptTalentShuffle = !RandomizeTalentLists(export, random); //true if shuffle is OK, false if it failed
                            shuffleattempts++;
                        }
                        break;
                    case "LevelUp_ChallengeScalingVars":
                        //RandomizeLevelUpChallenge(export, random);
                        break;
                    case "Items_ItemEffectLevels":
                        RandomizeWeaponStats(export, random);
                        break;
                    case "Characters_Character":
                        RandomizeCharacter(export, random);
                        break;
                }
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Finishing Galaxy Map Randomizing"));

            engine.save();
        }

        private void RandomizeCharacter(IExportEntry export, Random random)
        {
            bool hasChanges = false;
            int[] humanLightArmorManufacturers = { 373, 374, 375, 379, 383, 451 };
            int[] bioampManufacturers = { 341, 342, 343, 345, 410, 496, 497, 498, 526 };
            int[] omnitoolManufacturers = { 362, 363, 364, 366, 411, 499, 500, 501, 527 };
            List<string> actorTypes = new List<string>();
            actorTypes.Add("BIOG_HumanFemale_Hench_C.hench_humanFemale");
            actorTypes.Add("BIOG_HumanMale_Hench_C.hench_humanmale");
            actorTypes.Add("BIOG_Asari_Hench_C.hench_asari");
            actorTypes.Add("BIOG_Krogan_Hench_C.hench_krogan");
            actorTypes.Add("BIOG_Turian_Hench_C.hench_turian");
            actorTypes.Add("BIOG_Quarian_Hench_C.hench_quarian");
            actorTypes.Add("BIOG_Jenkins_Hench_C.hench_jenkins");

            Bio2DA character2da = new Bio2DA(export);
            for (int row = 0; row < character2da.rowNames.Count(); row++)
            {
                //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));


                if (mainWindow.RANDSETTING_CHARACTER_HENCH_ARCHETYPES)
                {
                    if (character2da[row, 0].GetDisplayableValue().StartsWith("hench"))
                    {
                        //Henchman
                        int indexToChoose = random.Next(actorTypes.Count);
                        var actorNameVal = actorTypes[indexToChoose];
                        actorTypes.RemoveAt(indexToChoose);
                        Console.WriteLine("Character Randomizer HENCH ARCHETYPE [" + row + "][2] value is now " + actorNameVal);
                        character2da[row, 2].Data = BitConverter.GetBytes((ulong)export.FileRef.findName(actorNameVal));
                        hasChanges = true;
                    }
                }

                if (mainWindow.RANDSETTING_CHARACTER_INVENTORY)
                {
                    int randvalue = random.Next(humanLightArmorManufacturers.Length);
                    int manf = humanLightArmorManufacturers[randvalue];
                    Console.WriteLine("Character Randomizer ARMOR [" + row + "][21] value is now " + manf);
                    character2da[row, 21].Data = BitConverter.GetBytes(manf);

                    if (character2da[row, 24] != null)
                    {
                        randvalue = random.Next(bioampManufacturers.Length);
                        manf = bioampManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer BIOAMP [" + row + "][24] value is now " + manf);
                        character2da[row, 24].Data = BitConverter.GetBytes(manf);
                        hasChanges = true;
                    }

                    if (character2da[row, 29] != null)
                    {
                        randvalue = random.Next(omnitoolManufacturers.Length);
                        manf = omnitoolManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer OMNITOOL [" + row + "][29] value is now " + manf);
                        character2da[row, 29].Data = BitConverter.GetBytes(manf);
                        hasChanges = true;
                    }
                }
            }
            if (hasChanges)
            {
                character2da.Write2DAToExport();
            }
        }

        /// <summary>
        /// Randomizes the highest-level galaxy map view. Values are between 0 and 1 for columns 1 and 2 (X,Y).
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeClusters(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Galaxy Map - Clusters"));

            Bio2DA cluster2da = new Bio2DA(export);
            int[] colsToRandomize = { 1, 2 };
            for (int row = 0; row < cluster2da.rowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));
                    float randvalue = random.NextFloat(0, 1);
                    Console.WriteLine("Cluster Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    cluster2da[row, colsToRandomize[i]].Data = BitConverter.GetBytes(randvalue);
                }
            }
            cluster2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the mid-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeSystems(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Galaxy Map - Systems"));

            Console.WriteLine("Randomizing Galaxy Map - Systems");
            Bio2DA system2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 };//X,Y
            for (int row = 0; row < system2da.rowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(system2da[row, colsToRandomize[i]].Data, 0));
                    float randvalue = random.NextFloat(0, 1);
                    Console.WriteLine("System Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    system2da[row, colsToRandomize[i]].Data = BitConverter.GetBytes(randvalue);
                }
                //string value = system2da[row, 9].GetDisplayableValue();
                //Console.WriteLine("Scale: [" + row + "][9] value is " + value);
                float scalerandvalue = random.NextFloat(0.25, 2);
                Console.WriteLine("System Randomizer [" + row + "][9] value is now " + scalerandvalue);
                system2da[row, 9].Data = BitConverter.GetBytes(scalerandvalue);
                system2da[row, 9].Type = Bio2DACell.TYPE_FLOAT;
            }
            system2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the planet-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizePlanets(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Galaxy Map - Planets"));

            Console.WriteLine("Randomizing Galaxy Map - Planets");
            Bio2DA planet2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 };//X,Y
            for (int row = 0; row < planet2da.rowNames.Count(); row++)
            {
                for (int i = 0; i < planet2da.columnNames.Count(); i++)
                {
                    if (planet2da[row, i] != null && planet2da[row, i].Type == Bio2DACell.TYPE_FLOAT)
                    {
                        Console.WriteLine("[" + row + "][" + i + "]  (" + planet2da.columnNames[i] + ") value is " + BitConverter.ToSingle(planet2da[row, i].Data, 0));
                        float randvalue = random.NextFloat(0, 1);
                        if (i == 11)
                        {
                            randvalue = random.NextFloat(0.4, 2);
                        }
                        Console.WriteLine("Planets Randomizer [" + row + "][" + i + "] (" + planet2da.columnNames[i] + ") value is now " + randvalue);
                        planet2da[row, i].Data = BitConverter.GetBytes(randvalue);
                    }
                }
            }
            planet2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the planet-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeWeaponStats(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Items - Weapon Stats"));


            Console.WriteLine("Randomizing Items - Item Effect Levels");
            Bio2DA itemeffectlevels2da = new Bio2DA(export);
            for (int row = 0; row < itemeffectlevels2da.rowNames.Count(); row++)
            {
                Bio2DACell propertyCell = itemeffectlevels2da[row, 2];
                if (propertyCell != null)
                {
                    int gameEffect = propertyCell.GetIntValue();
                    switch (gameEffect)
                    {
                        case 15:
                            //GE_Weap_Damage
                            ItemEffectLevels.Randomize_GE_Weap_Damage(itemeffectlevels2da, row, random);
                            break;
                        case 17:
                            //GE_Weap_RPS
                            ItemEffectLevels.Randomize_GE_Weap_RPS(itemeffectlevels2da, row, random);
                            break;
                        case 447:
                            //GE_Weap_Projectiles
                            ItemEffectLevels.Randomize_GE_Weap_PhysicsForce(itemeffectlevels2da, row, random);
                            break;
                        case 1199:
                            //GE_Weap_HeatPerShot
                            ItemEffectLevels.Randomize_GE_Weap_HeatPerShot(itemeffectlevels2da, row, random);
                            break;
                        case 1201:
                            //GE_Weap_HeatLossRate
                            ItemEffectLevels.Randomize_GE_Weap_HeatLossRate(itemeffectlevels2da, row, random);
                            break;
                        case 1259:
                            //GE_Weap_HeatLossRateOH
                            ItemEffectLevels.Randomize_GE_Weap_HeatLossRateOH(itemeffectlevels2da, row, random);
                            break;
                    }
                }
            }
            itemeffectlevels2da.Write2DAToExport();
        }



        /// <summary>
        /// Randomizes the 4 guns you get at the start of the game.
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeStartingWeapons(IExportEntry export, Random random)
        {
            /* These are the valid values, invalid ones are removed. They might include some ones not normally accessible but are fully functional
            324	Manf_Armax_Weap
            325	Manf_Devlon_Weap
            326	Manf_Elkoss_Weap
            327	Manf_HK_Weap
            412	Manf_Elanus_Weap
            436	Manf_Geth_Weap
            502	Manf_Spectre01_Weap
            503	Manf_Spectre02_Weap
            504	Manf_Spectre03_Weap
            525	Manf_Haliat_Weap
            582	Manf_Ariake_Weap
            583	Manf_Rosen_Weap
            584	Manf_Kassa_Weap
            598	Manf_Batarian_Weap
            599	Manf_Cerberus_Weap
            600	Manf_Jorman_Weap
            601	Manf_HKShadow_Weap*/

            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Starting Weapons"));
            bool randomizeLevels = true; //will use better later
            Console.WriteLine("Randomizing Starting Weapons");
            Bio2DA startingitems2da = new Bio2DA(export);
            int[] rowsToRandomize = { 0, 1, 2, 3 };
            int[] manufacturers = { 324, 325, 326, 327, 412, 436, 502, 503, 504, 525, 582, 583, 584, 598, 599, 600, 601 };
            foreach (int row in rowsToRandomize)
            {
                //Columns:
                //0: Item Class - you must have 1 of each or game will crash when swapping to that slot and cutscenes will be super bugged
                //1: Item Sophistication (Level?)
                //2: Manufacturer
                if (randomizeLevels)
                {
                    startingitems2da[row, 2].Data = BitConverter.GetBytes(random.Next(1, 10));
                }
                startingitems2da[row, 2].Data = BitConverter.GetBytes(manufacturers[random.Next(manufacturers.Length)]);
            }
            startingitems2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the talent list
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private bool RandomizeTalentLists(IExportEntry export, Random random)
        {
            //List of talents... i think. Taken from talent_talenteffectlevels
            //int[] talentsarray = { 0, 7, 14, 15, 21, 28, 29, 30, 35, 42, 49, 50, 56, 57, 63, 64, 84, 86, 91, 93, 98, 99, 108, 109, 119, 122, 126, 128, 131, 132, 134, 137, 138, 141, 142, 145, 146, 149, 150, 153, 154, 157, 158, 163, 164, 165, 166, 167, 168, 169, 170, 171, 174, 175, 176, 177, 178, 180, 182, 184, 186, 188, 189, 190, 192, 193, 194, 195, 196, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 305, 306, 307, 310, 312, 313, 315, 317, 318, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332 };
            List<int> talentidstoassign = new List<int>();
            Bio2DA classtalents = new Bio2DA(export);

            for (int row = 0; row < classtalents.rowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].GetIntValue();
                if (baseclassid == 10)
                {
                    continue;
                }
                int isvisible = classtalents[row, 6].GetIntValue();
                if (isvisible == 0)
                {
                    continue;
                }
                talentidstoassign.Add(classtalents[row, 1].GetIntValue());
            }

            int i = 0;
            int spectretrainingid = 259;
            //while (i < 60)
            //{
            //    talentidstoassign.Add(spectretrainingid); //spectre training
            //    i++;
            //}

            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Class talents list"));
            //bool randomizeLevels = false; //will use better later
            Console.WriteLine("Randomizing Class talent list");

            int currentClassNum = -1;
            List<int> powersAssignedToThisClass = new List<int>();
            List<int> rowsNeedingPrereqReassignments = new List<int>(); //some powers require a prereq, this will ensure all powers are unlockable for this randomization
            List<int> talentidsNeedingReassignment = new List<int>(); //used only to filter out the list of bad choices, e.g. don't depend on self.
            List<int> powersAssignedAsPrereq = new List<int>(); //only assign 1 prereq to a power tree
            for (int row = 0; row < classtalents.rowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].GetIntValue();
                if (baseclassid == 10)
                {
                    continue;
                }
                if (currentClassNum != baseclassid)
                //this block only executes when we are changing classes in the list, so at this point
                //we have all of the info loaded about the class (e.g. all powers that have been assigned)
                {
                    if (powersAssignedToThisClass.Count() > 0)
                    {
                        List<int> possibleAllowedPrereqs = powersAssignedToThisClass.Except(talentidsNeedingReassignment).ToList();

                        //reassign prereqs now that we have a list of powers
                        foreach (int prereqrow in rowsNeedingPrereqReassignments)
                        {
                            int randomindex = -1;
                            int prereq = -1;
                            //while (true)
                            //{
                            randomindex = random.Next(possibleAllowedPrereqs.Count());
                            prereq = possibleAllowedPrereqs[randomindex];
                            //powersAssignedAsPrereq.Add(prereq);
                            classtalents[prereqrow, 8].Data = BitConverter.GetBytes(prereq);
                            classtalents[prereqrow, 9].Data = BitConverter.GetBytes(random.Next(5) + 4);
                            Console.WriteLine("Class " + baseclassid + "'s power on row " + row + " now depends on " + classtalents[prereqrow, 8].GetIntValue() + " at level " + classtalents[prereqrow, 9].GetIntValue());
                            //}
                        }
                    }
                    rowsNeedingPrereqReassignments.Clear();
                    powersAssignedToThisClass.Clear();
                    powersAssignedAsPrereq.Clear();
                    currentClassNum = baseclassid;

                }
                int isvisible = classtalents[row, 6].GetIntValue();
                if (isvisible == 0)
                {
                    continue;
                }

                if (classtalents[row, 8] != null)
                {
                    //prereq
                    rowsNeedingPrereqReassignments.Add(row);
                }

                if (classtalents[row, 1] != null) //talentid
                {
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value originally is " + classtalents[row, 1].GetDisplayableValue());

                    int randomindex = -1;
                    int talentindex = -1;
                    int reassignattemptsremaining = 250; //attempt 250 random attempts.
                    while (true)
                    {
                        reassignattemptsremaining--;
                        if (reassignattemptsremaining <= 0)
                        {
                            //this isn't going to work.
                            return false;
                        }
                        randomindex = random.Next(talentidstoassign.Count());
                        talentindex = talentidstoassign[randomindex];
                        if (baseclassid <= 5 && talentindex == spectretrainingid)
                        {
                            continue;
                        }
                        if (!powersAssignedToThisClass.Contains(talentindex))
                        {
                            break;
                        }
                    }

                    talentidstoassign.RemoveAt(randomindex);
                    classtalents[row, 1].Data = BitConverter.GetBytes(talentindex);
                    powersAssignedToThisClass.Add(talentindex);
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value is now " + classtalents[row, 1].GetDisplayableValue());
                }
                //if (randomizeLevels)
                //{
                //classtalents[row, 1].Data = BitConverter.GetBytes(random.Next(1, 12));
                //}
            }
            classtalents.Write2DAToExport();
            return true;
        }

        /// <summary>
        /// Randomizes the challenge scaling variables used by enemies
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeLevelUpChallenge(IExportEntry export, Random random)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Class talents list"));
            bool randomizeLevels = false; //will use better later
            Console.WriteLine("Randomizing Class talent list");
            Bio2DA challenge2da = new Bio2DA(export);



            for (int row = 0; row < challenge2da.rowNames.Count(); row++)
            {
                for (int col = 0; col < challenge2da.columnNames.Count(); col++)
                    if (challenge2da[row, col] != null)
                    {
                        Console.WriteLine("[" + row + "][" + col + "]  (" + challenge2da.columnNames[col] + ") value originally is " + challenge2da[row, 1].GetDisplayableValue());
                        //int randomindex = random.Next(talents.Count());
                        //int talentindex = talents[randomindex];
                        //talents.RemoveAt(randomindex);
                        float multiplier = random.NextFloat(0.7, 1.3);
                        if (col % 2 == 0)
                        {
                            //Fraction
                            Bio2DACell cell = challenge2da[row, col];
                            if (cell.Type == Bio2DACell.TYPE_FLOAT)
                            {
                                challenge2da[row, col].Data = BitConverter.GetBytes(challenge2da[row, col].GetFloatValue() * multiplier);
                            }
                            else
                            {
                                challenge2da[row, col].Data = BitConverter.GetBytes(challenge2da[row, col].GetIntValue() * multiplier);
                                challenge2da[row, col].Type = Bio2DACell.TYPE_FLOAT;
                            }
                        }
                        else
                        {
                            //Level Offset
                            challenge2da[row, col].Data = BitConverter.GetBytes((int)(challenge2da[row, col].GetIntValue() * multiplier));
                        }
                        Console.WriteLine("[" + row + "][" + col + "]  (" + challenge2da.columnNames[col] + ") value is now " + challenge2da[row, 1].GetDisplayableValue());
                    }
                if (randomizeLevels)
                {
                    challenge2da[row, 1].Data = BitConverter.GetBytes(random.Next(1, 12));
                }
            }
            challenge2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the character creator
        /// </summary>
        /// <param name="random">Random number generator</param>
        private void RandomizeCharacterCreator(Random random, IExportEntry export)
        {
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, "Randomizing Charactor Creator"));
            //if (headrandomizerclasses.Contains(export.ObjectName))
            //{
            //    RandomizePregeneratedHead(export, random);
            //    continue;
            //}
            Bio2DA export2da = new Bio2DA(export);
            bool hasChanges = false;
            for (int row = 0; row < export2da.rowNames.Count(); row++)
            {
                float numberedscalar = 0;
                for (int col = 0; col < export2da.columnNames.Count(); col++)
                {

                    //Extent
                    if (export2da.columnNames[col] == "Extent" || export2da.columnNames[col] == "Rand_Extent")
                    {
                        float multiplier = random.NextFloat(0.5, 6);

                        Bio2DACell cell = export2da[row, col];
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.columnNames[col] + ") value originally is " + export2da[row, col].GetDisplayableValue());

                        if (cell.Type == Bio2DACell.TYPE_FLOAT)
                        {
                            export2da[row, col].Data = BitConverter.GetBytes(export2da[row, col].GetFloatValue() * multiplier);
                            hasChanges = true;
                        }
                        else
                        {
                            export2da[row, col].Data = BitConverter.GetBytes(export2da[row, col].GetIntValue() * multiplier);
                            export2da[row, col].Type = Bio2DACell.TYPE_FLOAT;
                            hasChanges = true;
                        }
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.columnNames[col] + ") value now is " + export2da[row, col].GetDisplayableValue());
                        continue;
                    }

                    //Hair Scalars
                    if (export.ObjectName.Contains("MorphHair") && row > 0 && col >= 4 && col <= 8)
                    {

                        float scalarval = random.NextFloat(0, 1);
                        if (col == 5)
                        {
                            numberedscalar = scalarval;
                        }
                        else if (col > 5)
                        {
                            scalarval = numberedscalar;
                        }
                        Bio2DACell cell = export2da[row, col];
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.columnNames[col] + ") value originally is " + export2da[row, col].GetDisplayableValue());
                        export2da[row, col].Data = BitConverter.GetBytes(scalarval);
                        export2da[row, col].Type = Bio2DACell.TYPE_FLOAT;
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.columnNames[col] + ") value now is " + export2da[row, col].GetDisplayableValue());
                        hasChanges = true;
                        continue;
                    }

                    //Skin Tone
                    if (export.ObjectName.Contains("Skin_Tone") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE && row > 0 && col >= 1 && col <= 5)
                    {
                        if (export.ObjectName.Contains("Female"))
                        {
                            if (col < 5)
                            {
                                //Females have one less column
                                string rgbNewName = GetRandomColorRBGStr(random);
                                int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                                export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                                hasChanges = true;
                            }
                        } else
                        {
                            string rgbNewName = GetRandomColorRBGStr(random);
                            int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                            export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                            hasChanges = true;
                        }
                    }
                }
            }
            if (hasChanges)
            {
                export2da.Write2DAToExport();
            }
        }

        private void RandomizePregeneratedHead(IExportEntry export, Random random)
        {
            int[] floatSliderIndexesToRandomize = { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
            Dictionary<int, int> columnMaxDictionary = new Dictionary<int, int>();
            columnMaxDictionary[1] = 7; //basehead
            columnMaxDictionary[2] = 6; //skintone
            columnMaxDictionary[3] = 3; //archetype
            columnMaxDictionary[4] = 14; //scar
            columnMaxDictionary[12] = 8; //eyeshape
            columnMaxDictionary[18] = 13; //iriscolor +1
            columnMaxDictionary[23] = 10; //mouthshape
            columnMaxDictionary[28] = 13; //noseshape
            columnMaxDictionary[31] = 14; //beard
            columnMaxDictionary[32] = 7; //brows +1
            columnMaxDictionary[33] = 9; //hair
            columnMaxDictionary[34] = 8; //haircolor
            columnMaxDictionary[35] = 8; //facialhaircolor

            if (export.ObjectName.Contains("Female"))
            {
                floatSliderIndexesToRandomize = new int[] { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
                columnMaxDictionary.Clear();
                //there are female specific values that must be used
                columnMaxDictionary[1] = 10; //basehead
                columnMaxDictionary[2] = 6; //skintone
                columnMaxDictionary[3] = 3; //archetype
                columnMaxDictionary[4] = 11; //scar
                columnMaxDictionary[12] = 10; //eyeshape
                columnMaxDictionary[18] = 13; //iriscolor +1
                columnMaxDictionary[23] = 10; //mouthshape
                columnMaxDictionary[28] = 12; //noseshape
                columnMaxDictionary[31] = 8; //haircolor
                columnMaxDictionary[32] = 10; //hair
                columnMaxDictionary[33] = 17; //brows
                columnMaxDictionary[34] = 7; //browcolor
                columnMaxDictionary[35] = 7; //blush
                columnMaxDictionary[36] = 8; //lipcolor
                columnMaxDictionary[37] = 8; //eyemakeupcolor

            }
            Bio2DA export2da = new Bio2DA(export);
            for (int row = 0; row < export2da.rowNames.Count(); row++)
            {
                foreach (int col in floatSliderIndexesToRandomize)
                {
                    export2da[row, col].Data = BitConverter.GetBytes(random.NextFloat(0, 1));
                }
            }

            for (int row = 0; row < export2da.rowNames.Count(); row++)
            {
                foreach (KeyValuePair<int, int> entry in columnMaxDictionary)
                {
                    int col = entry.Key;
                    Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.columnNames[col] + ") value originally is " + export2da[row, col].GetDisplayableValue());

                    export2da[row, col].Data = BitConverter.GetBytes(random.Next(0, entry.Value) + 1);
                    export2da[row, col].Type = Bio2DACell.TYPE_INT;
                    Console.WriteLine("Character Creator Randomizer [" + row + "][" + col + "] (" + export2da.columnNames[col] + ") value is now " + export2da[row, col].GetDisplayableValue());

                }
            }
            Console.WriteLine("Writing export " + export.ObjectName);
            export2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the the music table
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeMusic(IExportEntry export, Random random, string randomizingtext = null)
        {
            if (randomizingtext == null)
            {
                randomizingtext = "Randomizing Music";
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, randomizingtext));
            Console.WriteLine(randomizingtext);
            Bio2DA music2da = new Bio2DA(export);
            List<byte[]> names = new List<byte[]>();
            int[] colsToRandomize = { 0, 5, 6, 7, 8, 9, 10, 11, 12 };
            for (int row = 0; row < music2da.rowNames.Count(); row++)
            {
                foreach (int col in colsToRandomize)
                {
                    if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.TYPE_NAME)
                    {
                        if (!music2da[row, col].GetDisplayableValue().StartsWith("music"))
                        {
                            continue;
                        }
                        names.Add(music2da[row, col].Data.TypedClone());
                    }
                }
            }

            for (int row = 0; row < music2da.rowNames.Count(); row++)
            {
                foreach (int col in colsToRandomize)
                {
                    if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.TYPE_NAME)
                    {
                        if (!music2da[row, col].GetDisplayableValue().StartsWith("music"))
                        {
                            continue;
                        }
                        Console.WriteLine("[" + row + "][" + col + "]  (" + music2da.columnNames[col] + ") value originally is " + music2da[row, col].GetDisplayableValue());
                        int r = random.Next(names.Count);
                        byte[] pnr = names[r];
                        names.RemoveAt(r);
                        music2da[row, col].Data = pnr;
                        Console.WriteLine("Music Randomizer [" + row + "][" + col + "] (" + music2da.columnNames[col] + ") value is now " + music2da[row, col].GetDisplayableValue());

                    }
                }
            }
            music2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the sounds and music in GUIs. This is shared between two tables as it contains the same indexing and table format
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeGUISounds(IExportEntry export, Random random, string randomizingtext = null, string requiredprefix = null)
        {
            if (randomizingtext == null)
            {
                randomizingtext = "Randomizing UI - Sounds";
            }
            randomizationWorker.ReportProgress(0, new ThreadCommand(UPDATE_RANDOMIZING_TEXT, randomizingtext));
            Console.WriteLine(randomizingtext);
            Bio2DA guisounds2da = new Bio2DA(export);
            int[] colsToRandomize = { 0 };//sound name
            List<byte[]> names = new List<byte[]>();

            if (requiredprefix != "music")
            {

                for (int row = 0; row < guisounds2da.rowNames.Count(); row++)
                {
                    if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.TYPE_NAME)
                    {
                        if (requiredprefix != null && !guisounds2da[row, 0].GetDisplayableValue().StartsWith(requiredprefix))
                        {
                            continue;
                        }
                        names.Add(guisounds2da[row, 0].Data.TypedClone());
                    }
                }
            }
            else
            {
                for (int n = 0; n < export.FileRef.Names.Count; n++)
                {
                    string name = export.FileRef.Names[n];
                    if (name.StartsWith("music.mus"))
                    {
                        Int64 nameval = n;
                        names.Add(BitConverter.GetBytes(nameval));
                    }
                }
            }

            for (int row = 0; row < guisounds2da.rowNames.Count(); row++)
            {
                if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.TYPE_NAME)
                {
                    if (requiredprefix != null && !guisounds2da[row, 0].GetDisplayableValue().StartsWith(requiredprefix))
                    {
                        continue;
                    }
                    Thread.Sleep(20);
                    Console.WriteLine("[" + row + "][" + 0 + "]  (" + guisounds2da.columnNames[0] + ") value originally is " + guisounds2da[row, 0].GetDisplayableValue());
                    int r = random.Next(names.Count);
                    byte[] pnr = names[r];
                    names.RemoveAt(r);
                    guisounds2da[row, 0].Data = pnr;
                    Console.WriteLine("Sounds - GUI Sounds Randomizer [" + row + "][" + 0 + "] (" + guisounds2da.columnNames[0] + ") value is now " + guisounds2da[row, 0].GetDisplayableValue());

                }
            }
            guisounds2da.Write2DAToExport();
        }

        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-3, 20));
            return (float)(mantissa * exponent);
        }

        static string GetRandomColorRBGStr(Random random)
        {
            return $"RGB({random.Next(255)},{random.Next(255)},{random.Next(255)})";
        }
    }
}
