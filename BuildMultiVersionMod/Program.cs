using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace BuildMultiVersionMod
{
    class Program
    {
        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:dd.MMM.yyyy HH:mm:ss.fff}] {message}");
        }

        private static void Log(params string[] categoriesAndMessage)
        {
            Log($"[{string.Join(" ", categoriesAndMessage.Take(categoriesAndMessage.Length - 1).Select(x => $"[{x}]"))}] {categoriesAndMessage.Last()}");
        }

        private static void CopyDirectoryRecursively(string source, string dest)
        {
            if (Directory.Exists(dest))
            {
                throw new InvalidOperationException("Destination directory exists. Aborting!");
            }

            // create directory structure
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(dest, dir.Substring(source.Length + 1)));
            }

            // copy files
            foreach (string file_name in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                File.Copy(file_name, Path.Combine(dest, file_name.Substring(source.Length + 1)));
            }
        }

        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            Log("Discovering current directory...");
            var currentDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            Log("Current directory: " + currentDir);

            var oldVersionFolder = Path.Combine(currentDir, "..", "..", "..", "TechAdvancingMulti", "OldVersions");
            Log("OldVersion input directory: " + oldVersionFolder);

            var latestFolder = Path.Combine(currentDir, "..", "..", "..", "TechAdvancing");
            Log("LatestTechadvancing input directory: " + latestFolder);

            var outputFolder = Path.Combine(currentDir, "..", "..", "..", "TechAdvancingMulti", "TechAdvancing");
            Log("Output directory: " + outputFolder);

            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            Directory.CreateDirectory(outputFolder);

            var aboutXmlPath = Path.Combine(latestFolder, "About", "About.xml");
            var aboutXmlFile = XDocument.Load(aboutXmlPath);

            var supportedVersionsNode = aboutXmlFile.Root.Element("supportedVersions");

            var supportedVersions = supportedVersionsNode.Elements().Select(x => x.Value).ToList();
            if (supportedVersions.Count > 1)
            {
                Log("Warning: More than one supported version specified! Picking latest.");
            }
            else if (supportedVersions.Count == 0)
            {
                throw new InvalidOperationException("No supported versions listed in the About.xml file. Aborting!");
            }

            supportedVersions.Sort();
            Log("Found version(s): " + string.Join(", ", supportedVersions));

            var latestVersion = supportedVersions.Last();
            Log("Chosen latest version: " + latestVersion);

            var oldVersionsLatestVersionPath = Path.Combine(oldVersionFolder, latestVersion);
            if (Directory.Exists(oldVersionsLatestVersionPath))
            {
                throw new InvalidOperationException($"Directory {oldVersionsLatestVersionPath} should not exist, but it does! Aborting.");
            }

            Log("Copying the current version...");
            var multiversion_latestPath = Path.Combine(outputFolder, latestVersion);
            CopyDirectoryRecursively(latestFolder, multiversion_latestPath);

            var outputAboutFolder = Path.Combine(outputFolder, "About");
            Directory.Move(Path.Combine(multiversion_latestPath, "About"), outputAboutFolder);
            CopyDirectoryRecursively(Path.Combine(multiversion_latestPath, "Languages"), Path.Combine(outputFolder, "Languages"));


            Log("Updating About.xml...");

            var oldVersions = Directory.GetDirectories(oldVersionFolder, "*", SearchOption.TopDirectoryOnly).Select(x => x.Split(Path.DirectorySeparatorChar).Last()).Reverse().ToList();

            foreach (var version in oldVersions)
            {
                supportedVersionsNode.Add(new XElement("li", version));
            }

            aboutXmlFile.Save(Path.Combine(outputAboutFolder, "About.xml"));


            Log("Copying old versions...");
            foreach (var oldVersion in oldVersions)
            {
                Log($"Copying {oldVersion}...");
                CopyDirectoryRecursively(Path.Combine(oldVersionFolder, oldVersion), Path.Combine(outputFolder, oldVersion));
                var delPath = Path.Combine(outputFolder, oldVersion, "About");
                if (Directory.Exists(delPath))
                    Directory.Delete(delPath, true);
            }



            Log("Done!");
        }
    }
}
