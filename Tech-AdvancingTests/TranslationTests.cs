using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Xml;

namespace TechAdvancingTests
{
    /// <summary>
    /// THIS ENTIRE PROJECT IS LITERALLY JUST FOR CHECKING TRANSLATIONS AND OTHER STUFF THATS NOT RELATED TO THE MAIN PROJECT. DO NOT REFERENCE IT BECAUSE IT WILL BREAK CI!
    /// </summary>
    [TestClass]
    public class TranslationTests
    {
        [TestMethod]
        public void XmlSyntaxTest()
        {
            string languageFolder = Path.Combine(
                new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName,
                @"..\..\..\..\TechAdvancing\Languages\");

            var xmlFiles = Directory.GetFiles(languageFolder, "*.xml", SearchOption.AllDirectories);
            foreach (var translation in xmlFiles)
            {
                string contents = File.ReadAllText(translation);
                try
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(contents);
                }
                catch (XmlException ex)
                {
                    Assert.Fail("Invalid XML syntax: " + ex.ToString());
                }
            }
        }
    }
}
