using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Fintec.Iso8583;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Iso8583.Test
{
	[TestClass]
	public class ConfigParserTests
	{
		private static string _configXmlContent;
		private static string _pathToConfigXmlIncludingFilename;

		[ClassInitialize]
		public static void Init(TestContext testContext)
		{
			_configXmlContent = GetTextFromEmbededResource("Iso8583.Test.Iso8583Config.xml");
			_pathToConfigXmlIncludingFilename = Path.Combine(GetExecutingDirectoryNameForTests(), "Iso8583Config.xml");
			using (StreamWriter streamWriter = File.CreateText(_pathToConfigXmlIncludingFilename))
			{
				streamWriter.Write(_configXmlContent);
			}
		}

		[TestMethod]
		public void ConfigParser_CreateDefault_CreatesMessageFactoryFromFile()
		{
			var sut = ConfigParser.CreateDefault(_pathToConfigXmlIncludingFilename);
			var msgType0100 = int.Parse("100", NumberStyles.HexNumber);
			var header100 = sut.GetIsoHeader(msgType0100);

			header100.Should().Be("ISO015000050", "because we expect the config file to be read and parsed");
		}

		/// <summary>
		/// Reads text that is embedded into an assembly
		/// </summary>
		/// <remarks> 
		/// fullyQualifiedFileName is fully qualified, e.g. if the file abc.config was in assembly with namespace MyCompany.MyLib inside a 
		/// folder Config then fullyQualifiedFileName should read MyCompany.MyLib.abc.config
		/// </remarks>
		private static string GetTextFromEmbededResource(string fullyQualifiedFileName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var result = string.Empty;

			using (var stream = assembly.GetManifestResourceStream(fullyQualifiedFileName))
			{
				if (stream == null) return result;
				var reader = new StreamReader(stream);
				result = reader.ReadToEnd();
			}

			return result;
		}

		/// <summary>
		/// Gets the executing directory name for tests.
		/// </summary>
		/// <returns></returns>
		public static string GetExecutingDirectoryNameForTests()
		{
			var location = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);
			var dirName = new FileInfo(location.AbsolutePath).Directory.FullName;
			return Uri.UnescapeDataString(dirName);
		}
	}
}
