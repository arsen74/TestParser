using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using TestParser.Parsing;

namespace TestParser.Tests
{
    [TestClass]
    public class SearchEngineTest
    {
        [TestMethod]
        public void Search_Links_First_Valid()
        {
            //Assign
            var html = @"<html><head><title>First</title></head><body><a href=""#first"">First</a><a href=""#second"">Second</a></body></html>";

            //Act
            var result = SearchEngine.FindLinks(html);

            //Assert
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("First", result[0].Html);
            Assert.AreEqual("#first", result[0].Href);
            Assert.AreEqual("Second", result[1].Html);
            Assert.AreEqual("#second", result[1].Href);
        }

        [TestMethod]
        public void Search_Links_Second_Html_Text()
        {
            //Assign
            var html = @"<html><head><title>Second</title></head><body><a href=""/"" itemprop=""url""><img itemprop=""logo"" src=""/img/3.0/logo_7_ru.png"" class=""logo"" alt="""" /></a></html>";

            //Act
            var result = SearchEngine.FindLinks(html);

            //Assert
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(@"<img itemprop=""logo"" src=""/img/3.0/logo_7_ru.png"" class=""logo"" alt="""" />", result[0].Html);
            Assert.AreEqual("/", result[0].Href);
        }

        [TestMethod]
        public void Search_Links_Valid_Real_Data_1()
        {
            //Assign
            string html = string.Empty;
            using (var stream = File.OpenRead(Path.Combine(string.Concat(Environment.CurrentDirectory, @"\_html\", "test_1.htm"))))
            {
                html = LoadHtml(stream, Encoding.UTF8);
            }

            //Act
            var result = SearchEngine.FindLinks(html);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(457, result.Length);
        }

        private static string LoadHtml(Stream stream, Encoding encoding, int bufferSize = 1024)
        {
            string htmlSource = null;
            using (var reader = new StreamReader(stream, encoding, true, bufferSize))
            {
                htmlSource = reader.ReadToEnd();
            }

            return htmlSource;
        }
    }
}
