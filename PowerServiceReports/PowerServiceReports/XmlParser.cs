using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog.Core;

namespace PowerServiceReports
{
    public class XmlParser
    {
        public static PowerServiceReportsConfig LoadConfigFile(Logger Logger, string path)
        {
            try
            {
                PowerServiceReportsConfig config = new PowerServiceReportsConfig();
                if (!File.Exists(path))
                {
                    Console.WriteLine("The configuration file is missing. Please input the folder path for storing the CSV file and " +
                                      "scheduled time interval in munites. Configuration file will be created from your input.");
                    Logger.Warning("[PowerServiceReports] The configuration file is missing.");
                    return null;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlElement xmlConf = doc.DocumentElement;
                foreach (XmlNode n in xmlConf.ChildNodes)
                {
                    switch (n.LocalName.ToUpper())
                    {
                        case "INTERVAL":
                            config.interval = int.Parse(n.InnerText);
                            break;
                        case "PATH":
                            config.folderPath = n.InnerText;
                            break;
                    }
                }

                return config;
            }
            catch (Exception e)
            {
                Logger.Error($"An error ocurred while loading the configuration. {e}");
                return null;
            }
        }

        public static void CreateConfigFile(Logger Logger, string path, PowerServiceReportsConfig config)
        {
            try
            {
                using (var writer = XmlWriter.Create(path, new XmlWriterSettings() { Indent = true, IndentChars = "\t" }))
                {
                    writer.WriteStartDocument();

                    writer.WriteStartElement("configuration");
                    writer.WriteElementString("path", config.folderPath);
                    writer.WriteElementString("interval", config.interval.ToString());
                    writer.WriteEndElement();

                    writer.WriteEndDocument();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"An error ocurred while storing the configuration file. {e}");
            }
        }

        public static void UpdateConfigFile(Logger Logger, PowerServiceReportsConfig config, string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlElement xmlConf = doc.DocumentElement;
                foreach (XmlNode n in xmlConf.ChildNodes)
                {
                    switch (n.LocalName.ToUpper())
                    {
                        case "INTERVAL":
                            if (config.interval.HasValue)
                                n.InnerText = config.interval.Value.ToString();
                            break;
                        case "PATH":
                            if (!string.IsNullOrEmpty(config.folderPath) || !string.IsNullOrWhiteSpace(config.folderPath))
                                n.InnerText = config.folderPath;
                            break;
                    }
                }

                doc.Save(path);
            }
            catch (Exception e)
            {
                Logger.Error($"An error ocurred while changing the settings. {e}");
            }
        }
    }
}
