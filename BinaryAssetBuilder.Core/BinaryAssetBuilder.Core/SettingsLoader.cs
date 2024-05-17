using System;
using System.IO;

namespace BinaryAssetBuilder.Core
{
	public class SettingsLoader
	{
		public static Settings GetSettingsForConfiguration(string configName)
		{
			Settings settings = (Settings)Settings.Current.Clone();
			SetConfiguration(settings, configName);
			return settings;
		}

		private static void SetConfiguration(Settings settings, string configName)
		{
			string text = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(configName))
			{
				BuildConfiguration buildConfiguration = null;
				BuildConfiguration[] buildConfigurations = settings.BuildConfigurations;
				foreach (BuildConfiguration buildConfiguration2 in buildConfigurations)
				{
					if (buildConfiguration2.Name.Equals(configName, StringComparison.InvariantCultureIgnoreCase))
					{
						buildConfiguration = buildConfiguration2;
						break;
					}
				}
				if (buildConfiguration == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Invalid build configuration '{0}' specified.", configName);
				}
				text = buildConfiguration.ArtPaths;
				text3 = buildConfiguration.AudioPaths;
				text2 = buildConfiguration.DataPaths;
				settings.Postfix = buildConfiguration.Postfix;
				if (buildConfiguration.AppendPostfixToStream && buildConfiguration.StreamPostfix == null && !string.IsNullOrEmpty(buildConfiguration.Postfix))
				{
					settings.StreamPostfix = "_" + buildConfiguration.Postfix;
				}
				else if (buildConfiguration.StreamPostfix == null)
				{
					settings.StreamPostfix = "";
				}
				else
				{
					settings.StreamPostfix = buildConfiguration.StreamPostfix;
				}
			}
			else
			{
				settings.Postfix = null;
			}
			if (text == null)
			{
				text = settings.DefaultArtPaths;
			}
			if (text3 == null)
			{
				text3 = settings.DefaultAudioPaths;
			}
			if (text2 == null)
			{
				text2 = settings.DefaultDataPaths;
			}
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(text3))
			{
				throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "No search paths for selected configuration specified.");
			}
			settings.ArtPaths = ProcessPaths(text);
			settings.DataPaths = ProcessPaths(text2);
			settings.AudioPaths = ProcessPaths(text3);
			if (settings.MonitorPaths != null)
			{
				settings.ProcessedMonitorPaths = ProcessPaths(settings.MonitorPaths);
			}
		}

		public static void PostProcessSettings(string anchorPath)
		{
			Settings current = Settings.Current;
			if (Path.GetFileName(anchorPath) == "running")
			{
				anchorPath = Path.GetDirectoryName(anchorPath);
			}
			if (!Path.IsPathRooted(current.DataRoot))
			{
				current.DataRoot = Path.GetFullPath(Path.Combine(anchorPath, current.DataRoot));
			}
			if (!Path.IsPathRooted(current.SchemaPath))
			{
				current.SchemaPath = Path.GetFullPath(Path.Combine(anchorPath, current.SchemaPath));
			}
			if (string.IsNullOrEmpty(current.OutputDirectory))
			{
				current.OutputDirectory = Path.GetDirectoryName(current.InputPath);
			}
			if (string.IsNullOrEmpty(current.IntermediateOutputDirectory) || !current.LinkedStreams)
			{
				current.IntermediateOutputDirectory = current.OutputDirectory;
			}
			if (current.BuildCache && string.IsNullOrEmpty(current.BuildCacheDirectory))
			{
				current.BuildCacheDirectory = Path.Combine(current.OutputDirectory, "cache");
			}
			if (current.SessionCache && string.IsNullOrEmpty(current.SessionCacheDirectory))
			{
				current.SessionCacheDirectory = current.OutputDirectory;
			}
			current.BigEndian = current.TargetPlatform != TargetPlatform.Win32;
			SetConfiguration(current, current.BuildConfigurationName);
		}

		private static string[] ProcessPaths(string combinedPaths)
		{
			string[] array = combinedPaths.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != "*")
				{
					array[i] = ShPath.Canonicalize(Path.Combine(Settings.Current.DataRoot, array[i]));
				}
			}
			return array;
		}
	}
}
