using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BinaryAssetBuilder.Project;
using BinaryAssetBuilder.Utility;
using EALA.Metrics;

namespace BinaryAssetBuilder.Core
{
	public class DocumentProcessor
	{
		private class TypeCompileData
		{
			public TimeSpan TotalProcessTime = new TimeSpan(0L);

			public TimeSpan LongestProcessTime = new TimeSpan(0L);

			public uint InstancesProcessed;

			public readonly string TypeName;

			public string LongestProcessInstance = "";

			public TypeCompileData(string typeName)
			{
				TypeName = typeName;
			}
		}

		public class ProcessOptions
		{
			public string BasePatchStream;

			public string[] BaseStreamSearchPaths = new string[0];

			public StreamReference[] StreamReferences = new StreamReference[0];

			public string RelativeBasePath = "";

			public bool UsePrecompiled;

			public bool GenerateOutput;

			public string Configuration;
		}

		[NonSerialized]
		public const uint Version = 11u;

		private static Tracer _Tracer = Tracer.GetTracer("DocumentProcessor", "Provides XML processing functionality");

		private static InstanceHandleSet _MissingReferences = new InstanceHandleSet();

		private List2<string> _DocumentStack = new List2<string>();

		private ISessionCache _Cache;

		private SchemaSet _SchemaSet;

		private InstanceHandleSet _RequiredInheritFromSources = new InstanceHandleSet();

		private PluginRegistry _PluginRegistry;

		private VerifierPluginRegistry _VerifierPluginRegistry;

		private int _InstancesProcessedCount;

		private int _FilesProcessedCount;

		private int _FilesParsedCount;

		private int _InstancesCopiedFromCacheCount;

		private int _InstancesCompiledCount;

		private static TimeSpan _TotalProcInstancesTime = new TimeSpan(0L);

		private static TimeSpan _TotalPrepareOutputTime = new TimeSpan(0L);

		private static TimeSpan _TotalPrepareSourceTime = new TimeSpan(0L);

		private static TimeSpan _TotalPostProcTime = new TimeSpan(0L);

		private static TimeSpan _TotalValidateTime = new TimeSpan(0L);

		private static IDictionary<uint, TypeCompileData> _TypeProcessingTime = new SortedDictionary<uint, TypeCompileData>();

		private long _MaxTotalMemory;

		private static Stack<string> _CurrentDocumentStack = new Stack<string>();

		private static Stack<string> _CurrentStreamStack = new Stack<string>();

		private bool _CompilingProject;

		private Dictionary<string, string> _ProjectDefaultConfigurations;

		[NonSerialized]
		private IDictionary<string, AssetLocationInfo> _LastWrittenAssets = new SortedDictionary<string, AssetLocationInfo>();

		public ISessionCache Cache
		{
			get
			{
				return _Cache;
			}
			set
			{
				_Cache = value;
			}
		}

		public InstanceHandleSet MissingReferences => _MissingReferences;

		public InstanceHandleSet ChangedInheritFromReferences => _RequiredInheritFromSources;

		public SchemaSet SchemaSet
		{
			get
			{
				return _SchemaSet;
			}
			set
			{
				_SchemaSet = value;
			}
		}

		public PluginRegistry Plugins => _PluginRegistry;

		public VerifierPluginRegistry VerifierPlugins => _VerifierPluginRegistry;

		public static string CurrentDocument => _CurrentDocumentStack.Peek();

		public static void ResetTimers()
		{
			_TotalProcInstancesTime = new TimeSpan(0L);
			_TotalPrepareOutputTime = new TimeSpan(0L);
			_TotalPrepareSourceTime = new TimeSpan(0L);
			_TotalPostProcTime = new TimeSpan(0L);
			_TotalValidateTime = new TimeSpan(0L);
			_TypeProcessingTime = new Dictionary<uint, TypeCompileData>();
		}

		public DocumentProcessor(Settings settings, PluginRegistry pluginRegistry, VerifierPluginRegistry verifierPluginRegistry)
		{
			_PluginRegistry = pluginRegistry;
			_VerifierPluginRegistry = verifierPluginRegistry;
			if (Settings.Current.SingleFile)
			{
				_Tracer.TraceInfo("Single file mode enabled.");
				Settings.Current.BuildCache = false;
			}
			_Tracer.TraceData("Build Cache Status: {0}", Settings.Current.BuildCache ? "Disabled" : "Active");
			MetricManager.Submit("BAB.NetworkCacheEnabled", Settings.Current.BuildCache);
			if (Settings.Current.BuildCache)
			{
				_Tracer.TraceInfo("Network caching enabled ('{0}').", Settings.Current.BuildCacheDirectory);
			}
		}

		public void ProcessDocument(string fileName, bool generateOutput, bool __outputStringHashes, out bool success)
		{
			Logger.info($"DocumentProcessor ProcessDocument {fileName}");
			XIncludingReaderWrapper.LoadAssembly();
			ExpressionEvaluatorWrapper.LoadAssembly();
			DateTime now = DateTime.Now;
			using (new MetricTimer("BAB.ProcessingTime"))
			{
				if (!File.Exists(fileName))
				{
					throw new BinaryAssetBuilderException(ErrorCode.InputXmlFileNotFound, "File {0} not found", fileName);
				}
				MetricManager.Submit("BAB.MapName", Path.GetFileName(Path.GetDirectoryName(fileName)));
				MetricManager.Submit("BAB.StartupTime", DateTime.Now - now);
				now = DateTime.Now;
				success = false;
				try
				{
					if (Path.GetExtension(fileName).ToLower().Equals(".babproj", StringComparison.CurrentCultureIgnoreCase))
					{
						ProcessProjectDocument(fileName, generateOutput);
					}
					else
					{
						ProcessOptions processOptions = new ProcessOptions();
						processOptions.GenerateOutput = generateOutput;
						processOptions.BasePatchStream = Settings.Current.BasePatchStream;
						processOptions.UsePrecompiled = Settings.Current.UsePrecompiled;
						Logger.info("DocumentProcessor ProcessDocument begin ProcessDocumentInternal");
						ProcessDocumentInternal(fileName, fileName, null, processOptions);
						Logger.info("DocumentProcessor ProcessDocument end ProcessDocumentInternal");
					}
					success = true;
				}
				finally
				{
					MetricManager.Submit("BAB.PrepSourceTime", _TotalPrepareSourceTime);
					MetricManager.Submit("BAB.ProcIncludesTime", _TotalPostProcTime);
					MetricManager.Submit("BAB.ValidateTime", _TotalValidateTime);
					MetricManager.Submit("BAB.InstanceProcessingTime", _TotalProcInstancesTime);
					MetricManager.Submit("BAB.OutputPrepTime", _TotalPrepareOutputTime);
					MetricManager.Submit("BAB.DocumentProcessingTime", DateTime.Now - now);
					now = DateTime.Now;
					MetricManager.Submit("BAB.LinkedStreamsEnabled", Settings.Current.LinkedStreams);
					MetricManager.Submit("BAB.MaxMemorySize", _MaxTotalMemory);
					MetricManager.Submit("BAB.FilesProcessedCount", _FilesProcessedCount);
					MetricManager.Submit("BAB.FilesParsedCount", _FilesParsedCount);
					MetricManager.Submit("BAB.InstancesProcessedCount", _InstancesProcessedCount);
					MetricManager.Submit("BAB.InstancesCompiledCount", _InstancesCompiledCount);
					MetricManager.Submit("BAB.InstancesCopiedFromCacheCount", _InstancesCopiedFromCacheCount);
					MetricManager.Submit("BAB.ShutdownTime", DateTime.Now - now);
					MetricManager.Submit("BAB.BuildSuccessful", success);
				}
			}
		}

		public void ProcessProjectDocument(string projectPath, bool generateOutput)
		{
			Logger.info($"DocumentProcessor ProcessProjectDocument {projectPath}");
			_CompilingProject = true;
			_ProjectDefaultConfigurations = new Dictionary<string, string>();
			Settings current = Settings.Current;
			XmlSchema schema = XmlSchema.Read(Assembly.GetExecutingAssembly().GetManifestResourceStream("BinaryAssetBuilderProject.xsd"), null);
			StreamReader streamReader = new StreamReader(projectPath);
			XmlReader xmlReader = XmlReader.Create(projectPath);
			xmlReader.Settings.Schemas.Add(schema);
			BinaryAssetBuilderProject binaryAssetBuilderProject = null;
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(BinaryAssetBuilderProject));
				binaryAssetBuilderProject = xmlSerializer.Deserialize(xmlReader) as BinaryAssetBuilderProject;
			}
			catch (InvalidOperationException ex)
			{
				_Tracer.TraceError("There is an error in '{0}': {1}\n", projectPath, ex.InnerException.Message);
			}
			streamReader.Close();
			if (binaryAssetBuilderProject != null)
			{
				IDictionary<string, bool> dictionary = new SortedDictionary<string, bool>();
				BinaryStream[] stream = binaryAssetBuilderProject.Stream;
				foreach (BinaryStream binaryStream in stream)
				{
					string path = ((!Path.IsPathRooted(binaryStream.Source)) ? Path.Combine(Path.GetDirectoryName(projectPath), binaryStream.Source) : binaryStream.Source);
					path = Path.GetFullPath(path).ToLower();
					if (dictionary.TryGetValue(path, out var value) && value)
					{
						_Tracer.TraceError("{0} was specified twice in {1}, skipping duplicates", path, projectPath);
					}
					else if (binaryStream.Configuration != null && binaryStream.Configuration.Length > 0)
					{
						StreamConfiguration streamConfiguration = null;
						StreamConfiguration[] configuration = binaryStream.Configuration;
						foreach (StreamConfiguration streamConfiguration2 in configuration)
						{
							if (streamConfiguration2.Default)
							{
								if (streamConfiguration != null)
								{
									_Tracer.TraceWarning("Stream {0} has multiple default configurations. Using {1}.", path, streamConfiguration.Name);
								}
								else
								{
									streamConfiguration = streamConfiguration2;
								}
							}
						}
						if (streamConfiguration == null)
						{
							_Tracer.TraceInfo("No default configuration specified for {0}. Using {1}", binaryStream.Source, binaryStream.Configuration[0].Name);
							streamConfiguration = binaryStream.Configuration[0];
						}
						_ProjectDefaultConfigurations[path] = streamConfiguration.Name;
						StreamConfiguration[] configuration2 = binaryStream.Configuration;
						foreach (StreamConfiguration streamConfiguration3 in configuration2)
						{
							Settings.Current = SettingsLoader.GetSettingsForConfiguration(streamConfiguration3.Name);
							Settings.Current.BuildConfigurationName = streamConfiguration3.Name;
							ProcessOptions processOptions = new ProcessOptions();
							processOptions.GenerateOutput = true;
							processOptions.UsePrecompiled = true;
							processOptions.Configuration = streamConfiguration3.Name;
							processOptions.BasePatchStream = streamConfiguration3.PatchStream;
							processOptions.BaseStreamSearchPaths = streamConfiguration3.BaseStreamSearchPath;
							processOptions.RelativeBasePath = streamConfiguration3.RelativeBasePath;
							if (streamConfiguration3.StreamReference != null)
							{
								processOptions.StreamReferences = streamConfiguration3.StreamReference;
							}
							ProcessDocumentInternal(path, path, null, processOptions);
							Settings.Current = current;
						}
					}
					else
					{
						_ProjectDefaultConfigurations[path] = "";
						ProcessOptions processOptions2 = new ProcessOptions();
						processOptions2.GenerateOutput = true;
						processOptions2.UsePrecompiled = true;
						ProcessDocumentInternal(path, path, null, processOptions2);
					}
				}
			}
			_CompilingProject = false;
		}

		public AssetDeclarationDocument ProcessDocumentInternal(string logicalPath, string sourcePath, OutputManager outputManager, ProcessOptions op)
		{
			Logger.info($"DocumentProcessor ProcessDocumentInternal {logicalPath} {sourcePath}");
			DateTime now = DateTime.Now;
			AssetDeclarationDocument assetDeclarationDocument = OpenDocument(sourcePath, logicalPath, op.GenerateOutput, op.Configuration);
			if (op.GenerateOutput)
			{
				if (Settings.Current.StreamHints && Cache.DirtyStreams != null && !Cache.DirtyStreams.Contains(sourcePath) && LoadPrecompiledReference(assetDeclarationDocument, GetExpectedOutputManifest(assetDeclarationDocument.SourcePath), op.BaseStreamSearchPaths))
				{
					return assetDeclarationDocument;
				}
				if (assetDeclarationDocument.State == DocumentState.Complete)
				{
					return assetDeclarationDocument;
				}
				if (!Settings.Current.SingleFile && string.IsNullOrEmpty(assetDeclarationDocument.SourcePathFromRoot))
				{
					throw new BinaryAssetBuilderException(ErrorCode.IllegalPath, "{0} is a stream (.manifest) but does not have have {1} as its root!", sourcePath, Settings.Current.DataRoot);
				}
				string text = ShPath.Canonicalize(Path.Combine(Settings.Current.IntermediateOutputDirectory, assetDeclarationDocument.SourcePathFromRoot));
				string text2 = ShPath.Canonicalize(Path.Combine(Settings.Current.OutputDirectory, assetDeclarationDocument.SourcePathFromRoot));
				text2 = text2 + Settings.Current.StreamPostfix + Settings.Current.CustomPostfix;
				text = text + Settings.Current.StreamPostfix + Settings.Current.CustomPostfix;
				outputManager = new OutputManager(this, assetDeclarationDocument.LastOutputAssets, text2, text, op.BasePatchStream, op.RelativeBasePath, op.BaseStreamSearchPaths);
				_CurrentStreamStack.Push(sourcePath);
			}
			string fileName = Path.GetFileName(sourcePath);
			_CurrentDocumentStack.Push($"{fileName}:");
			if (assetDeclarationDocument.State == DocumentState.Complete && assetDeclarationDocument.IsLoaded && assetDeclarationDocument.XmlDocument == null)
			{
				foreach (InstanceDeclaration instance in assetDeclarationDocument.Instances)
				{
					BinaryAsset binaryAsset = outputManager.GetBinaryAsset(instance, isOutputAsset: false);
					if (binaryAsset.GetLocation(AssetLocation.All, AssetLocationOption.None) == AssetLocation.None)
					{
						_Tracer.TraceInfo("Reloading 'file://{0}' for new stream", assetDeclarationDocument.SourcePath);
						assetDeclarationDocument.State = DocumentState.Shallow;
						break;
					}
				}
			}
			if (assetDeclarationDocument.State == DocumentState.Shallow)
			{
				assetDeclarationDocument.Reinitialize(outputManager);
			}
			if (!assetDeclarationDocument.IsLoaded)
			{
				assetDeclarationDocument.ReloadIfRequired(_RequiredInheritFromSources);
			}
			_DocumentStack.Add(sourcePath);
			assetDeclarationDocument.Processing = true;
			_TotalPrepareSourceTime += DateTime.Now - now;
			ProcessIncludedDocuments(assetDeclarationDocument, outputManager, op, op.UsePrecompiled);
			if (assetDeclarationDocument.State != DocumentState.Complete)
			{
				ProcessDocumentContents(assetDeclarationDocument, outputManager, op, op.GenerateOutput);
			}
			assetDeclarationDocument.Processing = false;
			_CurrentDocumentStack.Pop();
			_MaxTotalMemory = Math.Max(_MaxTotalMemory, GC.GetTotalMemory(forceFullCollection: false));
			return assetDeclarationDocument;
		}

		private void ProcessDocumentContents(AssetDeclarationDocument document, OutputManager outputManager, ProcessOptions op, bool generateOutput)
		{
			Logger.info($"DocumentProcessor ProcessDocumentContents {document.SourcePath}");
			string configuration = op.Configuration;
			if (document.State != DocumentState.Loaded && !document.ValidateInheritFromSources())
			{
				document.InplaceLoad("inheritFrom source changed");
				ProcessIncludedDocuments(document, outputManager, op, usePrecompiled: false);
			}
			if (document.State != DocumentState.Loaded && !document.ValidateCachedDefines())
			{
				document.InplaceLoad("used definitions changed");
				ProcessIncludedDocuments(document, outputManager, op, usePrecompiled: false);
			}
			if (document.State == DocumentState.Loaded)
			{
				DateTime now = DateTime.Now;
				_FilesParsedCount++;
				document.ProcessExpressions();
				document.ProcessOverrides();
				document.Validate();
				_TotalValidateTime += DateTime.Now - now;
			}
			_InstancesProcessedCount += document.SelfInstances.Count;
			document.RecordStringHashes();
			document.MergeInstances();
			if (outputManager != null)
			{
				DateTime now2 = DateTime.Now;
				document.ProcessInstances(outputManager, ref _InstancesCompiledCount, ref _InstancesCopiedFromCacheCount);
				_TotalProcInstancesTime += DateTime.Now - now2;
			}
			document.AddStreamsHints(_CurrentStreamStack.ToArray());
			if (generateOutput)
			{
				DateTime now3 = DateTime.Now;
				_Tracer.Message("Resolving references: {0}", document.SourcePathFromRoot);
				document.PrepareOutputInstances(outputManager);
				_Tracer.Message("Generating stream: {0}", document.SourcePathFromRoot);
				outputManager.CommitManifest(document);
				outputManager.CleanOutput();
				document.UpdateOutputAssets(outputManager);
				if (Settings.Current.LinkedStreams)
				{
					outputManager.LinkStream(document);
				}
				if (Settings.Current.VersionFiles)
				{
					outputManager.CreateVersionFile(document, Settings.Current.CustomPostfix);
				}
				outputManager = null;
				_CurrentStreamStack.Pop();
				_Tracer.Message("{0} Stream complete", document.SourcePathFromRoot);
				_TotalPrepareOutputTime += DateTime.Now - now3;
			}
			_DocumentStack.RemoveAt(_DocumentStack.Count - 1);
			document.MakeComplete();
			document.MakeCacheable();
			Cache.SaveDocumentToCache(document.SourcePath, configuration, Settings.Current.TargetPlatform, document);
		}

		private AssetDeclarationDocument OpenDocument(string sourcePath, string logicalPath, bool generateOutput, string configuration)
		{
			if (!Path.IsPathRooted(sourcePath))
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Path for document {0} is not rooted.", sourcePath);
			}
			Cache.TryGetFile(sourcePath, configuration, Settings.Current.TargetPlatform, out var hashItem);
			Cache.TryGetDocument(sourcePath, configuration, Settings.Current.TargetPlatform, autoCreateDocument: true, out var document);
			if (document.Processing)
			{
				StringBuilder stringBuilder = new StringBuilder("Illegal circular document inclusion detected. Inclusion chain as follows:");
				foreach (string item in _DocumentStack)
				{
					stringBuilder.AppendFormat("\n   {0}", item);
				}
				stringBuilder.AppendFormat("\n   {0}", sourcePath);
				throw new BinaryAssetBuilderException(ErrorCode.CircularDependency, stringBuilder.ToString());
			}
			document.Open(this, hashItem, logicalPath, configuration);
			if (document.State != DocumentState.Complete)
			{
				_FilesProcessedCount++;
			}
			return document;
		}

		public string GetExpectedOutputManifest(string path)
		{
			string dataRoot = FileNameResolver.GetDataRoot(path);
			string text;
			if (!string.IsNullOrEmpty(dataRoot))
			{
				string path2 = path.Substring(dataRoot.Length + 1);
				string directoryName = Path.GetDirectoryName(path2);
				text = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(path));
			}
			else
			{
				text = Path.GetFileNameWithoutExtension(path);
			}
			return text + ".manifest";
		}

		private bool LoadPrecompiledReference(AssetDeclarationDocument parentDoc, string sourcePathFromRoot, string[] baseStreamSearchPaths)
		{
			string text = ShPath.Canonicalize(Path.Combine(Settings.Current.OutputDirectory, sourcePathFromRoot));
			if (File.Exists(text))
			{
				Manifest manifest = new Manifest();
				List<string> list = new List<string>();
				if (baseStreamSearchPaths != null)
				{
					list.AddRange(baseStreamSearchPaths);
				}
				list.Add(Settings.Current.OutputDirectory);
				try
				{
					manifest.Load(text, list.ToArray());
				}
				catch (Exception ex)
				{
					_Tracer.TraceError("Could not load {0}: {1}\n\r{2}", text, ex.Message, ex.StackTrace);
					return false;
				}
				try
				{
					ManifestHeader manifestHeader = new ManifestHeader();
					using (FileStream input = File.OpenRead(text))
					{
						manifestHeader.LoadFromStream(input, Settings.Current.BigEndian);
					}
					if (manifestHeader.IsLinked != Settings.Current.LinkedStreams || manifestHeader.Version != ManifestHeader.LatestVersion || manifestHeader.AllTypesHash != Plugins.DefaultPlugin.GetAllTypesHash())
					{
						_Tracer.TraceWarning("Could not load precompiled manifest {0}, manifest is incompatible", text);
						return false;
					}
				}
				catch (Exception innerException)
				{
					throw new BinaryAssetBuilderException(innerException, ErrorCode.LockedFile, "Unable to open '{0}'. Make sure no other application is writing to or reading from this file while the data build is running.", text);
				}
				Asset[] assets = manifest.Assets;
				foreach (Asset asset in assets)
				{
					InstanceDeclaration instanceDeclaration = new InstanceDeclaration();
					instanceDeclaration.InitializePrecompiled(asset);
					parentDoc.ReferenceInstances.Add(instanceDeclaration);
				}
				return true;
			}
			_Tracer.TraceWarning("Could not load precompiled manifest {0}, file not found", text);
			return false;
		}

		private void ProcessIncludedDocuments(AssetDeclarationDocument document, OutputManager outputManager, ProcessOptions op, bool usePrecompiled)
		{
			Logger.info($"DocumentProcessor ProcessIncludedDocuments {document.SourcePath} begin");
			DateTime now = DateTime.Now;
			string configuration = op.Configuration;
			document.TentativeInstances.Clear();
			document.AllInstances.Clear();
			document.ReferenceInstances.Clear();
			document.AllDefines.Clear();
			_ = document.SourceDirectory;
			if (document.IsLoaded)
			{
				foreach (InstanceDeclaration selfInstance in document.SelfInstances)
				{
					if (selfInstance.InheritFromHandle != null)
					{
						_RequiredInheritFromSources.TryAdd(selfInstance.InheritFromHandle);
					}
				}
			}
			foreach (InclusionItem inclusionItem in document.InclusionItems)
			{
				StreamReference streamReference = null;
				if (inclusionItem.Type == InclusionType.Reference && !Settings.Current.SingleFile && Settings.Current.DataRoot == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.NoDataRootSpecified, "DataRoot must be specified if not doing /singleFile");
				}
				string text = configuration;
				bool flag = false;
				AssetDeclarationDocument document2;
				if (inclusionItem.Type == InclusionType.Reference)
				{
					StreamReference[] streamReferences = op.StreamReferences;
					foreach (StreamReference streamReference2 in streamReferences)
					{
						if (inclusionItem.LogicalPath.EndsWith(streamReference2.ReferenceName))
						{
							text = streamReference2.ReferenceConfiguration;
							streamReference = streamReference2;
							_Tracer.TraceInfo("Attempting use of referenced stream {0} with configuration {1}", streamReference2.ReferenceName, streamReference2.ReferenceConfiguration);
							break;
						}
					}
					flag = Cache.TryGetDocument(inclusionItem.PhysicalPath, text, Settings.Current.TargetPlatform, autoCreateDocument: false, out document2);
					if (streamReference != null && document2 != null && document2.State == DocumentState.None)
					{
						bool flag2 = false;
						BuildConfiguration[] buildConfigurations = Settings.Current.BuildConfigurations;
						foreach (BuildConfiguration buildConfiguration in buildConfigurations)
						{
							if (string.IsNullOrEmpty(text) || buildConfiguration.Name.ToLower().Equals(text.ToLower()))
							{
								string path = null;
								if (string.IsNullOrEmpty(streamReference.ReferenceManifest))
								{
									GetExpectedOutputManifest(inclusionItem.PhysicalPath);
									path = Path.GetFileNameWithoutExtension(path) + buildConfiguration.StreamPostfix + ".manifest";
								}
								else
								{
									path = streamReference.ReferenceManifest;
								}
								document2.FromLastHack();
								if (!LoadPrecompiledReference(document2, path, op.BaseStreamSearchPaths))
								{
									throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Explicitly referenced external stream not found with desired build configuration.  Halting.");
								}
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Explicitly referenced stream was not found built with configuration {0}", text);
						}
					}
					else if (!flag || document2 == null || document2.State != DocumentState.Complete)
					{
						if (_CompilingProject && _ProjectDefaultConfigurations.TryGetValue(inclusionItem.PhysicalPath.ToLower(), out var value))
						{
							FileHashItem hashItem = null;
							text = value;
							flag = Cache.TryGetFile(inclusionItem.PhysicalPath, text, Settings.Current.TargetPlatform, out hashItem);
							_Tracer.TraceInfo("Stream {0} references stream {1} which does not have a '{2}' configuration, using default configuration '{3}'", document.SourcePath, inclusionItem.LogicalPath, configuration, text);
						}
						else if (usePrecompiled)
						{
							_Tracer.TraceInfo("Stream {0} references stream {1} which has not been compiled, attempting to use precompiled", document.SourcePath, inclusionItem.PhysicalPath);
							if (LoadPrecompiledReference(document, GetExpectedOutputManifest(inclusionItem.PhysicalPath), op.BaseStreamSearchPaths) || _CompilingProject)
							{
								continue;
							}
						}
					}
				}
				else
				{
					flag = Cache.TryGetDocument(inclusionItem.PhysicalPath, configuration, Settings.Current.TargetPlatform, autoCreateDocument: false, out document2);
				}
				if (!flag)
				{
					if (Settings.Current.ErrorLevel > 0)
					{
						throw new BinaryAssetBuilderException(ErrorCode.FileNotFound, "Input file '{0}' not found (referenced from file://{1}). Treating it as empty.", inclusionItem.LogicalPath, document.SourcePath);
					}
					_Tracer.TraceError("Input file '{0}' not found (referenced from file://{1}). Treating it as empty.", inclusionItem.LogicalPath, document.SourcePath);
				}
				bool flag3 = inclusionItem.Type == InclusionType.Reference && !usePrecompiled;
				_TotalPostProcTime += DateTime.Now - now;
				ProcessOptions processOptions = new ProcessOptions();
				processOptions.GenerateOutput = flag3;
				processOptions.UsePrecompiled = usePrecompiled;
				processOptions.Configuration = text;
				if (streamReference == null)
				{
					document2 = ProcessDocumentInternal(inclusionItem.LogicalPath, inclusionItem.PhysicalPath, flag3 ? null : outputManager, processOptions);
					now = DateTime.Now;
					inclusionItem.Document = document2;
					document.AllDefines.AddDefinitions(document2.AllDefines);
				}
				switch (inclusionItem.Type)
				{
				case InclusionType.Reference:
					document.ReferenceInstances.Add(document2.ReferenceInstances);
					document.ReferenceInstances.Add(document2.Instances);
					break;
				case InclusionType.All:
					document.ReferenceInstances.Add(document2.ReferenceInstances);
					document.AllInstances.Add(document2.Instances);
					document.TentativeInstances.Add(document2.TentativeInstances);
					break;
				case InclusionType.Instance:
					document.TentativeInstances.Add(document2.Instances);
					document.TentativeInstances.Add(document2.TentativeInstances);
					break;
				}
				document2.Reset();
				_TotalPostProcTime += DateTime.Now - now;
			}
			foreach (InstanceDeclaration referenceInstance in document.ReferenceInstances)
			{
				if (document.AllInstances.TryGetValue(referenceInstance.Handle, out var value2) && value2.Document == referenceInstance.Document)
				{
					document.AllInstances.Remove(value2);
				}
				if (document.TentativeInstances.TryGetValue(referenceInstance.Handle, out value2) && value2.Document == referenceInstance.Document)
				{
					document.TentativeInstances.Remove(value2);
				}
			}
			document.EvaluateDefinitions();
			
			Logger.info($"DocumentProcessor ProcessIncludedDocuments {document.SourcePath} end");
		}

		private void TryDeleteFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				try
				{
					File.Delete(filePath);
				}
				catch (Exception innerException)
				{
					throw new BinaryAssetBuilderException(innerException, ErrorCode.LockedFile, "Unable to delete '{0}'. Make sure no other application is writing to or reading from this file while the data build is running.", filePath);
				}
			}
		}

		public void AddLastWrittenAsset(BinaryAsset asset)
		{
			if (!_LastWrittenAssets.ContainsKey(asset.FileBase))
			{
				AssetLocationInfo assetLocationInfo = new AssetLocationInfo();
				assetLocationInfo.AssetOutputDirectory = asset.AssetOutputDirectory;
				assetLocationInfo.CustomDataOutputDirectory = asset.CustomDataOutputDirectory;
				_LastWrittenAssets.Add(asset.FileBase, assetLocationInfo);
			}
		}

		public AssetLocationInfo GetLastWrittenAsset(string key)
		{
			AssetLocationInfo value = null;
			_LastWrittenAssets.TryGetValue(key, out value);
			return value;
		}

		public void AddCompileTime(InstanceHandle handle, TimeSpan instanceCompileTime)
		{
			if (!_TypeProcessingTime.TryGetValue(handle.TypeId, out var value))
			{
				value = new TypeCompileData(handle.TypeName);
				_TypeProcessingTime.Add(handle.TypeId, value);
			}
			value.InstancesProcessed++;
			if (instanceCompileTime > value.LongestProcessTime)
			{
				value.LongestProcessTime = instanceCompileTime;
				value.LongestProcessInstance = handle.InstanceName;
			}
			value.TotalProcessTime += instanceCompileTime;
		}

		public void OutputTypeCompileTimes()
		{
			TimeSpan timeSpan = new TimeSpan(0L);
			foreach (KeyValuePair<uint, TypeCompileData> item in _TypeProcessingTime)
			{
				TypeCompileData value = item.Value;
				_Tracer.Message("{0}:", value.TypeName);
				_Tracer.Message("      Instances Processed: {0}", value.InstancesProcessed);
				_Tracer.Message("      Total Processing Time: {0}", value.TotalProcessTime);
				_Tracer.Message("      Longest Processing Time: {0} ({1})", value.LongestProcessTime, value.LongestProcessInstance);
				_Tracer.Message("      Average Processing Time: {0} seconds", value.TotalProcessTime.TotalSeconds / (double)value.InstancesProcessed);
				timeSpan += value.TotalProcessTime;
			}
			_Tracer.Message("Total Processing Time for All Types: {0}", timeSpan);
		}
	}
}
