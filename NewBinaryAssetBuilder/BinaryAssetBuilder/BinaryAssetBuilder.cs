using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BinaryAssetBuilder.Core;
using BinaryAssetBuilder.Remote;
using EALA.Metrics;

namespace BinaryAssetBuilder
{
	internal class BinaryAssetBuilder
	{
		private static Tracer _Tracer = Tracer.GetTracer("BinaryAssetBuilder", "BinaryAssetBuilder");

		private Mutex _Mutex = new Mutex();

		private Mutex _SessionCacheSaveMutex = new Mutex();

		private PluginRegistry _PluginRegistry;

		private VerifierPluginRegistry _VerifierPluginRegistry;

		private ISessionCache _cache;

		private PathMonitor _monitor;

		public static MetricDescriptor[] _Descriptors = new MetricDescriptor[22]
		{
			MetricManager.GetDescriptor("BAB.MapName", MetricType.Name, "Map name"),
			MetricManager.GetDescriptor("BAB.ProcessingTime", MetricType.Duration, "Total processing time"),
			MetricManager.GetDescriptor("BAB.FilesProcessedCount", MetricType.Count, "Number of files processed"),
			MetricManager.GetDescriptor("BAB.InstancesProcessedCount", MetricType.Count, "Number of instances processed"),
			MetricManager.GetDescriptor("BAB.InstancesCopiedFromCacheCount", MetricType.Count, "Number of instances copied from cache"),
			MetricManager.GetDescriptor("BAB.InstancesCompiledCount", MetricType.Count, "Number of instances compiled"),
			MetricManager.GetDescriptor("BAB.FilesParsedCount", MetricType.Count, "Number of files parsed"),
			MetricManager.GetDescriptor("BAB.SessionCacheSize", MetricType.Size, "Size of session cache"),
			MetricManager.GetDescriptor("BAB.MaxMemorySize", MetricType.Size, "Maximum allocated memory"),
			MetricManager.GetDescriptor("BAB.StartupTime", MetricType.Duration, "Startup time"),
			MetricManager.GetDescriptor("BAB.SessionSerialization", MetricType.Duration, "Session Serialization time"),
			MetricManager.GetDescriptor("BAB.ShutdownTime", MetricType.Duration, "Shutdown time"),
			MetricManager.GetDescriptor("BAB.DocumentProcessingTime", MetricType.Duration, "Document processing time"),
			MetricManager.GetDescriptor("BAB.NetworkCacheEnabled", MetricType.Enabled, "Network cache enabled"),
			MetricManager.GetDescriptor("BAB.SessionCacheEnabled", MetricType.Enabled, "Session cache enabled"),
			MetricManager.GetDescriptor("BAB.LinkedStreamsEnabled", MetricType.Enabled, "Linked streams enabled"),
			MetricManager.GetDescriptor("BAB.InstanceProcessingTime", MetricType.Duration, "Instance Processing Time"),
			MetricManager.GetDescriptor("BAB.OutputPrepTime", MetricType.Duration, "Output Preparation Time"),
			MetricManager.GetDescriptor("BAB.PrepSourceTime", MetricType.Duration, "Prepare Sources Time"),
			MetricManager.GetDescriptor("BAB.ProcIncludesTime", MetricType.Duration, "Include Processing Time"),
			MetricManager.GetDescriptor("BAB.ValidateTime", MetricType.Duration, "Validation Time"),
			MetricManager.GetDescriptor("BAB.BuildSuccessful", MetricType.Success, "Build completed successfully")
		};

		private DateTime _StartTime = DateTime.Now;

		public static GUIBuildOutput _BuildWindow = null;

		private int _RunResult;

		private TimeSpan _CacheSerializationTime;

		public ISessionCache Cache
		{
			set
			{
				_cache = value;
			}
		}

		public PathMonitor Monitor
		{
			set
			{
				_monitor = value;
			}
		}

		public int RunResult => _RunResult;

		public static string GetApplicationVersionString()
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			object[] array = null;
			string arg = "";
			array = entryAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), inherit: true);
			if (array.Length != 0)
			{
				arg = (array[0] as AssemblyTitleAttribute).Title;
			}
			string arg2 = entryAssembly.GetName().Version.ToString(3);
			string arg3 = "";
			array = entryAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), inherit: true);
			if (array.Length != 0)
			{
				arg3 = (array[0] as AssemblyCopyrightAttribute).Copyright;
			}
			return $"{arg} {arg2}\n{arg3}\n";
		}

		public BinaryAssetBuilder()
		{
			_PluginRegistry = new PluginRegistry(Settings.Current.Plugins, Settings.Current.TargetPlatform);
			_VerifierPluginRegistry = new VerifierPluginRegistry(Settings.Current.VerifierPlugins, Settings.Current.TargetPlatform);
		}

		public void GuiTraceWriter(string source, TraceEventType eventType, string message)
		{
			BaseTraceWriter(source, eventType, message);
			_BuildWindow.Write(source, eventType, message);
		}

		public void BaseTraceWriter(string source, TraceEventType eventType, string message)
		{
			if (eventType == TraceEventType.Information || eventType == TraceEventType.Verbose)
			{
				Console.WriteLine("[{0}] {1}", DateTime.Now - _StartTime, message);
			}
			else
			{
				Console.WriteLine("[{0}] {1}: {2}", DateTime.Now - _StartTime, eventType, message);
			}
		}

		public void DoBuildData()
		{
			Logger.info("BinaryAssetBuilder DoBuildData");
			try
			{
				MetricManager.OpenSession();
				_StartTime = DateTime.Now;
				_Tracer.Message("BinaryAssetBuilder started");
				MetricManager.Enabled = false;
				SchemaSet schemaSet = new SchemaSet(Settings.Current.StableSort);
				HashProvider.InitializeStringHashes(Settings.Current.SessionCacheDirectory);
				MetricManager.Submit("BAB.SessionCacheEnabled", Settings.Current.SessionCache);
				InitializeSessionCache();
				bool success = false;
				DocumentProcessor documentProcessor = new DocumentProcessor(Settings.Current, _PluginRegistry, _VerifierPluginRegistry);
				documentProcessor.Cache = _cache;
				documentProcessor.SchemaSet = schemaSet;
				documentProcessor.ProcessDocument(Settings.Current.InputPath, generateOutput: true, __outputStringHashes: true, out success);
				MetricManager.Enabled = false;
				Settings.Current.SingleFile = true;
				Settings.Current.BuildCache = false;
				if (Settings.Current.OutputAssetReport)
				{
					AssetReport.Close();
				}
				HashProvider.FinalizeStringHashes();
				if (Settings.Current.OutputStringHashes)
				{
					BuildStringHashes(schemaSet);
				}
				if (Settings.Current.SessionCache && (success || Settings.Current.PartialSessionCache) && !Settings.Current.FreezeSessionCache)
				{
					DateTime now = DateTime.Now;
					_cache.SaveCache(Settings.Current.CompressedSessionCache);
					_CacheSerializationTime += DateTime.Now - now;
					MetricManager.Submit("BAB.SessionSerialization", _CacheSerializationTime);
				}
				_Tracer.Message("BinaryAssetBuilder complete");
				documentProcessor.OutputTypeCompileTimes();
			}
			catch (Exception ex)
			{
				_RunResult = -1;
				if (ex.GetType() == typeof(BinaryAssetBuilderException))
				{
					((BinaryAssetBuilderException)ex).Trace(_Tracer);
				}
				else
				{
					_Tracer.TraceError(ex.ToString());
				}
				if (Settings.Current.PauseOnError && _BuildWindow == null)
				{
					Console.WriteLine("\nPress ENTER to exit\n");
					Console.ReadLine();
				}
				if (_BuildWindow != null)
				{
					_BuildWindow.SaveAndOpenText();
				}
				if (Settings.Current.Resident)
				{
					Console.WriteLine("Resident BAB must now exit due to previous errors.");
				}
			}
			finally
			{
				MetricManager.CloseSession();
				if (_BuildWindow != null)
				{
					_BuildWindow.DiffThreadClose();
					_BuildWindow = null;
				}
			}
			try
			{
				if (Settings.Current.Resident)
				{
					IClientCommand clientCommand = (IClientCommand)Activator.GetObject(typeof(IClientCommand), "ipc://BinaryAssetBuilderClientChannel/ClientCommand");
					clientCommand.NotifyBuildFinished(RunResult);
				}
			}
			catch
			{
			}
			finally
			{
				if (RunResult != 0)
				{
					if (Settings.Current.Resident)
					{
						Program._systemTrayForm.systemTrayIcon.Visible = false;
					}
					Environment.Exit(RunResult);
				}
			}
		}

		private void InitializeSessionCache()
		{
			if (Settings.Current.SessionCache)
			{
				string text = Path.Combine(Settings.Current.SessionCacheDirectory, "BinaryAssetBuilder.SessionCache.xml");
				if (text != _cache.CacheFileName)
				{
					DateTime now = DateTime.Now;
					_cache.LoadCache(text);
					_CacheSerializationTime = DateTime.Now - now;
				}
				if (Settings.Current.Resident && _monitor.IsResultTrustable())
				{
					_cache.InitializeCache(_monitor.GetChangedFiles());
				}
				else
				{
					_cache.InitializeCache(new List<string>());
				}
				_monitor.Reset();
				if (_PluginRegistry.AssetBuilderPluginsVersion != _cache.AssetCompilersVersion)
				{
					Settings.Current.StreamHints = false;
					_cache.AssetCompilersVersion = _PluginRegistry.AssetBuilderPluginsVersion;
				}
				_Tracer.TraceInfo("Session caching enabled ('{0}').", text);
			}
			else
			{
				_cache.InitializeCache(new List<string>());
			}
		}

		private bool BuildStringHashes(SchemaSet theSchemas)
		{
			string fileName = Path.Combine(HashProvider.GetOutputDirectory(), HashProvider.StringHashesFile);
			DocumentProcessor documentProcessor = new DocumentProcessor(Settings.Current, _PluginRegistry, _VerifierPluginRegistry);
			documentProcessor.Cache = _cache;
			documentProcessor.SchemaSet = theSchemas;
			documentProcessor.ProcessDocument(fileName, generateOutput: true, __outputStringHashes: false, out var success);
			return success;
		}

		public void Run()
		{
			Logger.info("BinaryAssetBuilder Run");
			_PluginRegistry.ReInitialize(Settings.Current.Plugins, Settings.Current.TargetPlatform);
			_VerifierPluginRegistry.ReInitialize(Settings.Current.VerifierPlugins, Settings.Current.TargetPlatform);
			Tracer.TraceWrite = BaseTraceWriter;
			if (Settings.Current.GuiMode)
			{
				_BuildWindow = new GUIBuildOutput();
				_BuildWindow.Show();
				Tracer.TraceWrite = GuiTraceWriter;
			}
			Tracer.SetTraceLevel(Settings.Current.TraceLevel);
			if (string.IsNullOrEmpty(Settings.Current.DataRoot))
			{
				throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Data root not set in application configuration file.");
			}
			if (Settings.Current.GuiMode)
			{
				Thread thread = new Thread(DoBuildData);
				thread.Start();
				Application.Run(_BuildWindow);
			}
			else
			{
				DoBuildData();
			}
		}
	}
}
