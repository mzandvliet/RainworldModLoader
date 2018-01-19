using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RainWorldInject
{
    class ConfigManager : IDisposable
    {
        private bool isDirty = false;
        private string configFileName = null;
        private readonly Dictionary<string, string> configRawDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, object> configParsedDictionary = new Dictionary<string, object>();

        public ConfigManager(string fileName) {
            LoadConfig(fileName, true); // change to false so will not create new file if there was no configure file
        }

        ~ConfigManager() {
            Dispose();
        }

        #region Load and Save
        public void LoadConfig(string fileName, bool forceCreate) {
            configRawDictionary.Clear();
            configParsedDictionary.Clear();

            if (ParseConfigFile(fileName) || forceCreate) configFileName = fileName;
        }

        private bool ParseConfigFile(string fileName) {

            if (!File.Exists(fileName)) return false;

            try {
                using (StreamReader sr = File.OpenText(fileName))
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (line.Length < 3) continue;
                        if (line.StartsWith(@"#")) continue;

                        int equals = line.IndexOf('=');
                        string key = line.Remove(equals).Trim();
                        string value = line.Substring(equals + 1).Trim();
                        configRawDictionary[key] = value;
                    }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            return true;
        }

        public bool SaveConfig() {
            if (configFileName == null) {
                Console.WriteLine("No configuration filename specify, will not save to file...");
                return false;
            }

            if (!isDirty) return true; // not dirty = no need to save.
            bool retVal = DoWriteToFile(configFileName);
            if (retVal) isDirty = false;

            return retVal;
        }

        private bool DoWriteToFile(string filename) {
            try {
                StringBuilder sb = new StringBuilder();

                foreach (KeyValuePair<string, string> p in configRawDictionary)
                    sb.AppendLine(p.Key + @" = " + p.Value);

                File.WriteAllText(filename, sb.ToString());
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }
        #endregion

        #region GetValue and SetValue
        public T GetValue<T>(string key, T defaultValue) {

            // return if we already parsed this value.
            if (configParsedDictionary.TryGetValue(key, out var obj)) return (T)obj;
            // return if we really doesn't have that value from config file
            if (!configRawDictionary.TryGetValue(key, out var raw)) return defaultValue;
            // we have, but not parsed. parse and return value.
            string dateType = typeof(T).Name;
            switch (dateType) {
                case @"Boolean":
                    obj = raw[0] == '1';
                    break;
                case @"Int32":
                    obj = int.Parse(raw);
                    break;
                case @"Int64":
                    obj = Int64.Parse(raw);
                    break;
                case @"String":
                    obj = raw;
                    break;
            }

            configParsedDictionary[key] = obj;
            configRawDictionary[key] = raw; // not neccessary though.

            return (T)obj;
        }

        public void SetValue<T>(string key, T value) {

            switch (typeof(T).Name) {
                case @"Boolean":
                    configRawDictionary[key] = value.ToString() == @"True" ? @"1" : @"0";
                    break;
                default:
                    configRawDictionary[key] = value?.ToString();
                    break;
            }

            // T is already a known type so fill parsed.
            configParsedDictionary[key] = value;

            isDirty = true;
            SaveConfig();
        }
        #endregion

        #region IDisposable Support
        public void Dispose() {
            if (isDirty) {
                SaveConfig();
            }
        }
        #endregion
    }
}
