using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace EqConverter
{
    class Program
    {
        public class FilterSet
        {
            public double f = 0.9282573; // Default to 16KHz for disabled filter
            public float gain = (float)0.5; // Default to 0dB for disabled filter
            public bool on = false;
            public double q = 0.39434525; // Default to 0.71 for disabled filter
            public const double type = 0.21428572; // Analog bell
        }

        public class Options
        {
            [Value(0)]
            public IEnumerable<string> Files
            {
                get;
                set;
            }

            [Option('f', "folders", Required = false, HelpText = "Use containing folders as preset names")]
            public bool Folders { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Enable verbose message output")]
            public bool Verbose { get; set; }
        }

        static void Main(string[] args)
        {
            const float q_min = (float)0.1;
            const float q_max = 10;
            const int f_min = 16;
            const int f_max = 20000;
            float preamp = (float)0.5;
            string out_folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\TBEQPresets";
            string preset = "";
            Console.WriteLine("AutoEq to Toneboosters PEQ Converter");
            var result = Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.Verbose)
                       {
                           Console.WriteLine("(i) Running in verbose mode");
                           Console.WriteLine("(v) Input files: {0}", String.Join(", ", o.Files));
                       }
                       string[] files = o.Files.ToArray();
                       if (files.Length == 0)
                       {
                           Console.WriteLine("(E) No files specified");
                           Environment.Exit(0);
                       }
                       if (o.Folders)
                       {
                           Console.WriteLine("(i) Presets will be named after the containing folder");
                       }
                       foreach (string filepath in files)
                       {
                           if (File.Exists(filepath))
                           {
                               Console.WriteLine("(i) Processing input file: '{0}'", filepath);
                               string[] lines = File.ReadAllLines(filepath);
                               if (lines[0].StartsWith("Preamp"))
                               {
                                   if (o.Folders)
                                   {
                                       preset = Path.GetDirectoryName(filepath).Split("\\").LastOrDefault();
                                   }
                                   else
                                   {
                                       preset = Path.GetFileName(filepath).Split(" ParametricEQ").FirstOrDefault();
                                   }
                                   if (o.Verbose)
                                   {
                                       Console.WriteLine("================================================================");
                                       Console.WriteLine("(v) Preset name will be {0}", preset);
                                   }
                                   preamp = Convert.ToSingle(lines[0].Substring(8, 5));
                                   if (o.Verbose)
                                   {
                                       Console.WriteLine("(v) Preamp gain is {0} dB", (preamp));
                                   }
                                   preamp = (float)Math.Round((preamp + 20) / 40, 8);
                                   if (o.Verbose)
                                   {
                                       Console.WriteLine("(v) Normalised preamp gain is {0}", preamp);
                                       Console.WriteLine("----------------------------------------------------------------");
                                   }
                                   lines = lines.Where((source, index) => index != 0).ToArray();
                                   FilterSet[] filters = new FilterSet[10];
                                   int line = 0;
                                   int disabled = 0;
                                   foreach (string filter in lines)
                                   {
                                       string temp = filter.Substring(filter.IndexOf(":") + 2);
                                       bool on = temp.StartsWith("ON");
                                       if (o.Verbose)
                                       {
                                           if (on) { Console.WriteLine("(v) Filter {0} is enabled", line + 1); }
                                           else { Console.WriteLine("(v) Filter {0} is disabled, skipping", line + 1); }
                                       }
                                       if (!on)
                                       {
                                           disabled++;
                                           line++;
                                           continue;
                                       }
                                       var result = Regex.Matches(temp, @"(?<=Fc )(.*)(?= Hz)");
                                       int f = Math.Min(Math.Max((int)Convert.ToInt16(result[0].Value), 16), 20000);
                                       if (o.Verbose)
                                       {
                                           Console.WriteLine("(v) Filter {0} frequency is {1} Hz", line + 1, f);
                                       }
                                       result = Regex.Matches(temp, @"(?<=Gain )(.*)(?= dB)");
                                       float gain = Convert.ToSingle(result[0].Value);
                                       if (o.Verbose)
                                       {
                                           Console.WriteLine("(v) Filter {0} gain is {1} dB", line + 1, gain);
                                       }
                                       result = Regex.Matches(temp, @"(?<=Q )(.*)");
                                       float q = Math.Min(Math.Max(Convert.ToSingle(result[0].Value), (float)0.1), 10);
                                       if (o.Verbose)
                                       {
                                           Console.WriteLine("(v) Filter {0} quality is {1}", line + 1, q);
                                       }
                                       filters[line] = new FilterSet();
                                       filters[line].f = Math.Round(Math.Pow((double)(f - f_min) / (f_max - f_min), 1.0 / 3.0), 8);
                                       filters[line].gain = (float)Math.Round((gain + 20) / 40, 8);
                                       filters[line].on = on;
                                       filters[line].q = Math.Round(Math.Pow((q - q_min) / (q_max - q_min), 1.0 / 3.0), 8);
                                       if (o.Verbose)
                                       {
                                           Console.WriteLine("(v) Filter {0} normalised frequency is {1}", line + 1, filters[line].f);
                                           Console.WriteLine("(v) Filter {0} normalised gain is {1}", line + 1, filters[line].gain);
                                           Console.WriteLine("(v) Filter {0} normalised quality is {1}", line + 1, filters[line].q);
                                           Console.WriteLine("----------------------------------------------------------------");
                                       }
                                       line++;
                                   }
                                   if (o.Verbose)
                                   {
                                       if (disabled == 1)
                                       {
                                           Console.WriteLine("(v) {0} filters found, {1} is disabled", line, disabled);
                                       }
                                       else
                                       {
                                           Console.WriteLine("(v) {0} filters found, {1} are disabled", line, disabled);
                                       }
                                       Console.WriteLine("================================================================");
                                   };
                                   Directory.CreateDirectory(out_folder);
                                   XmlWriterSettings settings = new XmlWriterSettings();
                                   settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
                                   settings.Indent = true;
                                   settings.IndentChars = "";
                                   XmlWriter output = XmlWriter.Create(out_folder + "\\" + preset + ".xml", settings);
                                   output.WriteStartDocument();
                                   output.WriteStartElement("Preset");
                                   output.WriteStartElement("PresetInfo");
                                   output.WriteAttributeString("Name", preset);
                                   output.WriteAttributeString("TenBand", "1");
                                   for (int i = 0; i < 10; i++)
                                   {
                                       if (o.Verbose)
                                       {
                                           Console.Write("(v) Writing filter {0} to XML file...", i + 1);
                                       };
                                       if (filters[i] == null)
                                       {
                                           filters[i] = new FilterSet();
                                       }
                                       output.WriteStartElement("Value");
                                       output.WriteString(Convert.ToString(filters[i].f));
                                       output.WriteEndElement();
                                       output.WriteStartElement("Value");
                                       output.WriteString(Convert.ToString(filters[i].gain));
                                       output.WriteEndElement();
                                       output.WriteStartElement("Value");
                                       output.WriteString(Convert.ToString(Convert.ToInt16(filters[i].on)));
                                       output.WriteEndElement();
                                       output.WriteStartElement("Value");
                                       output.WriteString(Convert.ToString(filters[i].q));
                                       output.WriteEndElement();
                                       output.WriteStartElement("Value");
                                       output.WriteString(Convert.ToString(FilterSet.type));
                                       output.WriteEndElement();
                                       output.WriteStartElement("Value");
                                       output.WriteString("0");
                                       output.WriteEndElement();
                                       if (o.Verbose)
                                       {
                                           Console.WriteLine(" OK");
                                       };
                                   }
                                   if (o.Verbose)
                                   {
                                       Console.Write("(v) Writing preamp gain to XML file...");
                                   };
                                   output.WriteStartElement("Value");
                                   output.WriteString("0");
                                   output.WriteEndElement();
                                   output.WriteStartElement("Value");
                                   output.WriteString(Convert.ToString(preamp));
                                   output.WriteEndElement();
                                   output.WriteStartElement("Value");
                                   output.WriteString("1");
                                   output.WriteEndElement();
                                   output.WriteStartElement("Value");
                                   output.WriteString("0.33333334");
                                   output.WriteEndElement();
                                   output.WriteStartElement("Value");
                                   output.WriteString("0.05");
                                   output.WriteEndElement();
                                   output.WriteStartElement("Value");
                                   output.WriteString("0");
                                   output.WriteEndDocument();
                                   output.Close();
                                   if (o.Verbose)
                                   {
                                       Console.WriteLine(" OK");
                                       Console.WriteLine("================================================================");
                                   };
                                   Console.WriteLine("(i) Input file: '{0}' processed!", filepath);
                               }
                               else
                               {
                                   Console.WriteLine("(E) File {0} is not a ParametricEQ.txt file", filepath);
                               }
                           }
                           else
                           {
                               Console.WriteLine("(E) File '{0}' does not exist", filepath);
                           }
                       };
                   });
        }
    }
}