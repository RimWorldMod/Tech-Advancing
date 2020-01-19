using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using TechAdvancing;

namespace TechAdvancingTests
{
    //[TestClass]
    //public class ConfigTabTests
    //{
    //    private List<ConfigTabValueSavedAttribute[]> GetSavedPropertiesAttributes =>
    //        typeof(TechAdvancing_Config_Tab).GetFields().Select(x => x.GetCustomAttributes(typeof(ConfigTabValueSavedAttribute), false).Cast<ConfigTabValueSavedAttribute>().ToArray()).ToList();

    //    /// <summary>
    //    /// Ensures that the <see cref="ConfigTabValueSavedAttribute"/> is only used once per field.
    //    /// </summary>
    //    [TestMethod]
    //    public void SingleUseSaveAttributeTest()
    //    {
    //        foreach (var attrCollection in this.GetSavedPropertiesAttributes)
    //        {
    //            if (attrCollection.Length > 1)
    //            {
    //                Assert.Fail($"A field is marked with more than one {nameof(ConfigTabValueSavedAttribute)} attribute. The attributes got the following savenames: "
    //                    + string.Join("; ", attrCollection.Select(x => x.SaveName)));
    //            }
    //        }
    //    }

    //    [TestMethod]
    //    public void DistinctSaveAttributeTest()
    //    {
    //        var alreadySeen = new List<string>();
    //        foreach (var attrCollection in this.GetSavedPropertiesAttributes)
    //        {
    //            foreach (var attr in attrCollection)
    //            {
    //                if (alreadySeen.Contains(attr.SaveName))
    //                    Assert.Fail($"Two or more {nameof(ConfigTabValueSavedAttribute)} attributes use the same savename. The conflicting name is {attr.SaveName}");

    //                alreadySeen.Add(attr.SaveName);
    //            }
    //        }
    //    }
    //}
}
