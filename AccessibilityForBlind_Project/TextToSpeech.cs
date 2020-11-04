﻿// Copyright (c) 2020 Jahangmar
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccessibilityForBlind
{
    public static class TextToSpeech
    {
        private static string executable;
        private static bool initialized = false;

        private const string linux_mimic_std = "/usr/bin/mimic";
        private const string linux_mimic_manual_install = "mimic/mimic";
        public static void Init()
        {
            initialized = false;
            switch (StardewModdingAPI.Constants.TargetPlatform)
            {
                case StardewModdingAPI.GamePlatform.Android:
                    ModEntry.Log("Android detected. Speech2Text is not supported.", StardewModdingAPI.LogLevel.Error);
                    break;
                case StardewModdingAPI.GamePlatform.Linux:
                    if (ModEntry.GetConfig().tts_path.Length > 0)
                    {
                        if (System.IO.File.Exists(ModEntry.GetConfig().tts_path))
                        {

                            executable = ModEntry.GetConfig().tts_path;
                            initialized = true;
                            ModEntry.Log("Using custom tts installation specified in config.json", StardewModdingAPI.LogLevel.Debug);
                        }
                        else
                        {
                            ModEntry.Log("Couldn't find specified tts installation '" + ModEntry.GetConfig().tts_path + "' from config.json", StardewModdingAPI.LogLevel.Error);
                        }
                    }
                    else if (System.IO.File.Exists(linux_mimic_std))
                    {
                        executable = linux_mimic_std;
                        initialized = true;
                        ModEntry.Log("Using system mimic installation", StardewModdingAPI.LogLevel.Debug);
                    }
                    else
                    {
                        string path = ModEntry.GetHelper().DirectoryPath + "/" + linux_mimic_manual_install;
                        if (System.IO.File.Exists(path))
                        {
                            executable = path;
                            initialized = true;
                            ModEntry.Log("Using mimic installation in mod folder", StardewModdingAPI.LogLevel.Debug);
                        }
                        else
                        {
                            ModEntry.Log("Couldn't find mimic installation at " + linux_mimic_std + " or in mod folder", StardewModdingAPI.LogLevel.Error);
                        }
                    }
                    break;
                case StardewModdingAPI.GamePlatform.Mac:
                    ModEntry.Log("OSX detected. Speech2Text is not supported.", StardewModdingAPI.LogLevel.Error);
                    break;
                case StardewModdingAPI.GamePlatform.Windows:
                    ModEntry.Log("Windows detected. Speech2Text is not supported.", StardewModdingAPI.LogLevel.Error);
                    break;
            }
            //if (initialized)
             //   queueTask.Start();
        }

        private static Process proc;
        private static Speech last_speech;
        private static Queue<Speech> queue = new Queue<Speech>();

        public struct Speech
        {
            public string text;
            public bool male;
            public float speed;
            public bool overwrite;
        }

        public static bool Speaking() =>
            proc != null && !proc.HasExited;

        public static void Stop()
        {
            if (Speaking())
                proc.Kill();
        }

        public static void Repeat()
        {
            Speak(last_speech);
        }

        public static void SpeakQueued(Speech speech)
        {
            queue.Enqueue(speech);
            speak(speech);
        }

        static Task queueTask = new Task(() =>
        {
            while (true)
            {
                while (queue.Count > 0 && !Speaking())
                {
                    Speech q = queue.Dequeue();
                    speak(q);
                };
            }
        });

        public static void Speak(Speech speech, bool overwrite = true) =>
            Speak(speech.text, speech.male, speech.speed);

        public static void Speak(string text, bool male=true, float speed = 1.0f, bool overwrite = true) 
        {
            if (!initialized)
                return;

            last_speech.text = text;
            last_speech.male = male;
            last_speech.speed = speed;

            Speech speech = new Speech()
            {
                text = text,
                male = male,
                speed = speed
            };

            if (overwrite)
            {
                queue.Clear();
                Stop();
            }

            SpeakQueued(speech);
        }

        private static void speak(Speech speech)
        {
            try
            {
                proc = new Process();
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.FileName = executable;
                proc.StartInfo.Arguments = "-t " + "\"" + speech.text.Replace('"', '\'') + "\"";
                proc.Start();
                ModEntry.Log("speaking '" + speech.text + "'");

            }
            catch (Exception e)
            {
                ModEntry.Log("Failed Text2Speech: " + e.Message, StardewModdingAPI.LogLevel.Error);
                proc = null;
            }

        }

        public static string ItemToSpeech(StardewValley.Item item)
        {
            if (item != null)
            {
                if (item.Stack > 1)
                    return item.Stack + " " + item.DisplayName;
                else
                    return item.DisplayName;
            }
            else
            {
                return "none";
            }
        }

    }

}
