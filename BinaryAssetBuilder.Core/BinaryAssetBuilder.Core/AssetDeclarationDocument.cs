using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using BinaryAssetBuilder.Utility;
using EALAHash;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class AssetDeclarationDocument : IXmlSerializable
	{
		private class XmlNodeWithMetaData
		{
			public XmlNode Node;

			public int LineNumber;

			public XmlNodeWithMetaData(XmlNode node, int line)
			{
				Node = node;
				LineNumber = line;
			}

			public bool Equals(XmlNodeWithMetaData other)
			{
				return Node.Equals(other.Node);
			}
		}

		private class DependencyComparer : IComparer<InstanceDeclaration>
		{
			public int Compare(InstanceDeclaration left, InstanceDeclaration right)
			{
				if (left == null || right == null)
				{
					throw new ArgumentNullException();
				}
				if (right.AllDependentInstances.Count != left.AllDependentInstances.Count)
				{
					return left.AllDependentInstances.Count.CompareTo(right.AllDependentInstances.Count);
				}
				bool flag = right.AllDependentInstances != null && right.AllDependentInstances.Contains(left.Handle);
				bool flag2 = left.AllDependentInstances != null && left.AllDependentInstances.Contains(right.Handle);
				if (flag)
				{
					if (flag2)
					{
						throw new BinaryAssetBuilderException(ErrorCode.CircularDependency, $"Illegal circular dependency found between {left} and {right}.  This should have been caught during parsing.");
					}
					return -1;
				}
				if (flag2)
				{
					return 1;
				}
				return string.Compare(left.Handle.TypeName, right.Handle.TypeName);
			}
		}

		private class TypeDepCompare : IComparer<InstanceDeclaration>
		{
			private IDictionary<uint, int> _TypeDependencies;

			public int Compare(InstanceDeclaration left, InstanceDeclaration right)
			{
				int num = _TypeDependencies[left.Handle.TypeId].CompareTo(_TypeDependencies[right.Handle.TypeId]);
				if (num == 0)
				{
					int num2 = string.Compare(left.Handle.TypeName, right.Handle.TypeName);
					if (num2 == 0)
					{
						return string.Compare(left.Handle.InstanceName, right.Handle.InstanceName);
					}
					return num2;
				}
				return num;
			}

			public TypeDepCompare(IDictionary<uint, int> typeDependencies)
			{
				_TypeDependencies = typeDependencies;
			}
		}

		[Serializable]
		private class LastState
		{
			public uint DocumentHash;

			public uint DependentFileHash;

			public List2<string> DependentFiles;

			public uint IncludePathHash;

			public List2<InclusionItem> InclusionItems;

			public List2<OutputAsset> OutputAssets;

			public List2<Definition> SelfDefines;

			public List2<InstanceDeclaration> SelfInstances;

			public List2<DefinitionPair> UsedDefines;

			public List2<string> StreamHints;

			public LastState(CurrentState current)
			{
				DocumentHash = current.DocumentHash;
				IncludePathHash = current.IncludePathHash;
				InclusionItems = ((current.InclusionItems.Count > 0) ? new List2<InclusionItem>(current.InclusionItems) : null);
				DependentFileHash = current.DependentFileHash;
				DependentFiles = ((current.DependentFiles.Count > 0) ? new List2<string>(current.DependentFiles) : null);
				OutputAssets = ((current.OutputAssets.Count > 0) ? new List2<OutputAsset>(current.OutputAssets) : null);
				SelfDefines = ((current.SelfDefines.Count > 0) ? new List2<Definition>(current.SelfDefines) : null);
				SelfInstances = ((current.SelfInstances.Count > 0) ? new List2<InstanceDeclaration>(current.SelfInstances) : null);
				List2<DefinitionPair> list = new List2<DefinitionPair>();
				foreach (KeyValuePair<string, string> usedDefine in current.UsedDefines)
				{
					list.Add(new DefinitionPair
					{
						Name = usedDefine.Key,
						EvaluatedValue = usedDefine.Value
					});
					UsedDefines = new List2<DefinitionPair>(list);
				}
				StreamHints = ((current.StreamHints.Count > 0) ? new List2<string>(current.StreamHints) : null);
			}

			public LastState(XmlReader reader)
			{
				reader.MoveToAttribute("d");
				string[] array = reader.Value.Split(';');
				DocumentHash = Convert.ToUInt32(array[0]);
				DependentFileHash = Convert.ToUInt32(array[1]);
				IncludePathHash = Convert.ToUInt32(array[2]);
				reader.Read();
				if (reader.IsStartElement())
				{
					ReadOldStrings(reader);
					object obj = XmlHelper.ReadStringArrayElement(reader, "sf");
					DependentFiles = ((obj == null) ? null : new List2<string>(obj as string[]));
					obj = XmlHelper.ReadStringArrayElement(reader, "dsf");
					StreamHints = ((obj == null) ? null : new List2<string>(obj as string[]));
					obj = XmlHelper.ReadCollection(reader, "iic", typeof(InclusionItem));
					InclusionItems = ((obj == null) ? null : new List2<InclusionItem>(obj as InclusionItem[]));
					obj = XmlHelper.ReadCollection(reader, "oac", typeof(OutputAsset));
					OutputAssets = ((obj == null) ? null : new List2<OutputAsset>(obj as OutputAsset[]));
					obj = XmlHelper.ReadCollection(reader, "sdc", typeof(Definition));
					SelfDefines = ((obj == null) ? null : new List2<Definition>(obj as Definition[]));
					obj = XmlHelper.ReadCollection(reader, "sic", typeof(InstanceDeclaration));
					SelfInstances = ((obj == null) ? null : new List2<InstanceDeclaration>(obj as InstanceDeclaration[]));
					obj = XmlHelper.ReadCollection(reader, "udc", typeof(DefinitionPair));
					UsedDefines = ((obj == null) ? null : new List2<DefinitionPair>(obj as DefinitionPair[]));
					reader.Read();
				}
			}

			private void ReadOldStrings(XmlReader reader)
			{
				string[] array = XmlHelper.ReadStringArrayElement(reader, "s");
				string[] array2 = XmlHelper.ReadStringArrayElement(reader, "p");
				if (array != null)
				{
					string[] array3 = array;
					foreach (string str in array3)
					{
						HashProvider.RecordHash("STRINGHASH", str);
					}
				}
				if (array2 != null)
				{
					string[] array4 = array2;
					foreach (string str2 in array4)
					{
						HashProvider.RecordHash("POID", str2);
					}
				}
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteStartElement("ad");
				writer.WriteAttributeString("d", $"{DocumentHash};{DependentFileHash};{IncludePathHash}");
				XmlHelper.WriteStringArrayElement(writer, "sf", (DependentFiles == null) ? null : DependentFiles.ToArray());
				XmlHelper.WriteStringArrayElement(writer, "dsf", (StreamHints == null) ? null : StreamHints.ToArray());
				XmlHelper.WriteCollection(writer, "iic", InclusionItems);
				XmlHelper.WriteCollection(writer, "oac", OutputAssets);
				XmlHelper.WriteCollection(writer, "sdc", SelfDefines);
				XmlHelper.WriteCollection(writer, "sic", SelfInstances);
				XmlHelper.WriteCollection(writer, "udc", UsedDefines);
				writer.WriteEndElement();
			}
		}

		private class CurrentState
		{
			public uint DocumentHash;

			public string SourcePath;

			public string SourceDirectory;

			public string SourcePathFromRoot;

			public string LogicalSourcePath;

			public uint DependentFileHash;

			public uint IncludePathHash;

			public uint OutputChecksum;

			public bool IsLoaded;

			public bool Processing;

			public string ChangeReason;

			public bool Changed;

			public DocumentState State;

			public List2<Definition> SelfDefines;

			public InstanceSet SelfInstances;

			public List2<string> DependentFiles;

			public List2<InclusionItem> InclusionItems;

			public IDictionary<string, string> UsedDefines;

			public bool VerificationErrors;

			public FileHashItem HashItem;

			public List2<InstanceDeclaration> OutputInstances = new List2<InstanceDeclaration>();

			public DefinitionSet AllDefines = new DefinitionSet();

			public InstanceSet Instances = new InstanceSet();

			public InstanceSet AllInstances = new InstanceSet();

			public InstanceSet TentativeInstances = new InstanceSet();

			public InstanceSet ReferenceInstances = new InstanceSet();

			public StringDictionary Tags = new StringDictionary();

			public XmlDocument XmlDocument;

			public XmlNamespaceManager NamespaceManager;

			public DocumentProcessor DocumentProcessor;

			public List2<OutputAsset> OutputAssets = new List2<OutputAsset>();

			public List2<string> StreamHints;

			public LinkedList<XmlNodeWithMetaData> NodeSourceInfoSet;

			public IDictionary<InstanceHandle, InstanceDeclaration> OutputInstanceSet;

			public XmlReader XmlReader;

			public string ConfigurationName;

			public List2<OutputAsset> LastOutputAssets;

			public uint LastDocumentHash;

			public uint LastDependentFileHash;

			public uint LastIncludePathHash;

			public CurrentState()
			{
			}

			public CurrentState(DocumentProcessor documentProcessor, FileHashItem hashItem, string logicalSourcePath, string configuration)
			{
				DocumentProcessor = documentProcessor;
				HashItem = hashItem;
				SourcePath = hashItem.Path;
				LogicalSourcePath = logicalSourcePath;
				SourceDirectory = Path.GetDirectoryName(SourcePath);
				DocumentHash = HashItem.Hash;
				ConfigurationName = configuration;
				string dataRoot = FileNameResolver.GetDataRoot(SourcePath);
				if (!string.IsNullOrEmpty(dataRoot))
				{
					string text = SourcePath.Substring(dataRoot.Length + 1);
					string directoryName = Path.GetDirectoryName(text);
					SourcePathFromRoot = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(SourcePath));
					LogicalSourcePath = "DATA:" + text.Replace('\\', '/');
				}
				else
				{
					SourcePathFromRoot = Path.GetFileNameWithoutExtension(SourcePath);
				}
			}

			public void FromLast(AssetDeclarationDocument document, LastState last)
			{
				InclusionItems = ((last.InclusionItems != null) ? new List2<InclusionItem>(last.InclusionItems) : new List2<InclusionItem>());
				DependentFiles = ((last.DependentFiles != null) ? new List2<string>(last.DependentFiles) : new List2<string>());
				StreamHints = ((last.StreamHints != null) ? new List2<string>(last.StreamHints) : new List2<string>());
				UsedDefines = new SortedDictionary<string, string>();
				if (last.UsedDefines != null)
				{
					foreach (DefinitionPair usedDefine in last.UsedDefines)
					{
						UsedDefines[usedDefine.Name] = usedDefine.EvaluatedValue;
					}
				}
				SelfDefines = ((last.SelfDefines != null) ? new List2<Definition>(last.SelfDefines) : new List2<Definition>());
				foreach (Definition selfDefine in SelfDefines)
				{
					selfDefine.Document = document;
				}
				SelfInstances = ((last.SelfInstances != null) ? new InstanceSet(document, last.SelfInstances) : new InstanceSet());
				Changed = last.DocumentHash != DocumentHash;
				LastOutputAssets = last.OutputAssets;
				LastDocumentHash = last.DocumentHash;
				LastIncludePathHash = last.IncludePathHash;
				LastDependentFileHash = last.DependentFileHash;
				State = DocumentState.Shallow;
			}

			public void FromScratch()
			{
				InclusionItems = new List2<InclusionItem>();
				DependentFiles = new List2<string>();
				UsedDefines = new SortedDictionary<string, string>();
				SelfDefines = new List2<Definition>();
				SelfInstances = new InstanceSet();
				StreamHints = new List2<string>();
				State = DocumentState.None;
			}
		}

		private enum LoadType
		{
			InplaceLoad,
			FromScratch
		}

		private static Tracer _Tracer = Tracer.GetTracer("DocumentProcessor", "Provides XML processing functionality");

		private LastState _Last;

		[NonSerialized]
		private CurrentState _Current;

		[NonSerialized]
		private bool _processing;

		[NonSerialized]
		public bool ReloadedForInheritance;

		public FileHashItem HashItem => _Current.HashItem;

		public List2<OutputAsset> LastOutputAssets => _Current.LastOutputAssets;

		public string SourcePathFromRoot => _Current.SourcePathFromRoot;

		public string SourcePath => _Current.SourcePath;

		public InstanceSet SelfInstances => _Current.SelfInstances;

		public List2<Definition> SelfDefines => _Current.SelfDefines;

		public List2<InclusionItem> InclusionItems => _Current.InclusionItems;

		public string LogicalSourcePath => _Current.LogicalSourcePath;

		public uint OutputChecksum => _Current.OutputChecksum;

		public bool IsLoaded => _Current.IsLoaded;

		public DefinitionSet AllDefines => _Current.AllDefines;

		public IDictionary<string, string> UsedDefines => _Current.UsedDefines;

		public List2<InstanceDeclaration> OutputInstances => _Current.OutputInstances;

		public InstanceSet Instances => _Current.Instances;

		public InstanceSet AllInstances => _Current.AllInstances;

		public InstanceSet TentativeInstances => _Current.TentativeInstances;

		public InstanceSet ReferenceInstances => _Current.ReferenceInstances;

		public DocumentState State
		{
			get
			{
				if (_Current != null)
				{
					return _Current.State;
				}
				return DocumentState.None;
			}
			set
			{
				_Current.State = value;
			}
		}

		public StringDictionary Tags => _Current.Tags;

		public XmlDocument XmlDocument => _Current.XmlDocument;

		public XmlNamespaceManager NamespaceManager => _Current.NamespaceManager;

		public string SourceDirectory => _Current.SourceDirectory;

		public bool VerificationErrors => _Current.VerificationErrors;

		public List2<string> StreamHints => _Last.StreamHints;

		public bool Processing
		{
			get
			{
				return _processing;
			}
			set
			{
				_processing = value;
			}
		}

		public void WriteXml(XmlWriter writer)
		{
			if (_Last != null)
			{
				_Last.WriteXml(writer);
			}
		}

		public void ReadXml(XmlReader reader)
		{
			_Last = new LastState(reader);
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void SetChanged(string reason)
		{
			_Current.ChangeReason = ((reason == null && _Current.ChangeReason == null) ? string.Empty : reason);
			_Current.Changed = true;
		}

		public void Reset()
		{
			_Current.XmlDocument = null;
			if (ReloadedForInheritance)
			{
				State = DocumentState.Complete;
				ReloadedForInheritance = false;
			}
		}

		public AssetDeclarationDocument()
		{
		}

		public AssetDeclarationDocument(DocumentProcessor documentProcessor, FileHashItem hashItem, string logicalPath, string configuration)
		{
			Open(documentProcessor, hashItem, logicalPath, configuration);
		}

		public void Reinitialize(OutputManager outputManager)
		{
			if (_Current.HashItem == null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.DependencyCacheFailure, "File hashing information for cached document missing: {0}", _Current.SourcePath);
			}
			string text = null;
			if (_Current.LastDocumentHash != _Current.DocumentHash)
			{
				text = "content changed";
			}
			else
			{
				UpdateDocumentHashes();
				if (_Current.LastDependentFileHash != _Current.DependentFileHash)
				{
					text = "dependent file changed";
				}
				else
				{
					foreach (InstanceDeclaration selfInstance in _Current.SelfInstances)
					{
						BinaryAsset binaryAsset = outputManager.GetBinaryAsset(selfInstance, isOutputAsset: false);
						if (binaryAsset != null && binaryAsset.GetLocation(AssetLocation.All, AssetLocationOption.None) == AssetLocation.None)
						{
							text = "previous output missing";
							break;
						}
						ExtendedTypeInformation extendedTypeInformation = _Current.DocumentProcessor.Plugins.GetExtendedTypeInformation(selfInstance.Handle.TypeId);
						if (selfInstance.Handle.TypeHash != extendedTypeInformation.TypeHash)
						{
							text = "type hash changed";
							break;
						}
						if (selfInstance.ProcessingHash != extendedTypeInformation.ProcessingHash)
						{
							text = "plugin output changed";
							break;
						}
					}
				}
			}
			if (text != null)
			{
				Load(text);
			}
			else if (_Current.LastIncludePathHash != _Current.IncludePathHash)
			{
				foreach (InclusionItem inclusionItem in _Current.InclusionItems)
				{
					inclusionItem.PhysicalPath = FileNameResolver.ResolvePath(SourceDirectory, inclusionItem.LogicalPath).ToLower();
				}
			}
			_Current.StreamHints.Clear();
		}

		public void ReloadIfRequired(InstanceHandleSet requiredOverrideSources)
		{
			string text = null;
			InstanceHandleSet instanceHandleSet = new InstanceHandleSet();
			foreach (InstanceHandle requiredOverrideSource in requiredOverrideSources)
			{
				if (_Current.SelfInstances.Contains(requiredOverrideSource))
				{
					instanceHandleSet.Add(requiredOverrideSource);
				}
			}
			if (instanceHandleSet.Count > 0)
			{
				if (text == null)
				{
					text = "inheritFrom target changed";
				}
				foreach (InstanceHandle item in instanceHandleSet)
				{
					requiredOverrideSources.Remove(item);
				}
			}
			if (text != null)
			{
				if (State == DocumentState.Complete && !IsLoaded)
				{
					PrevalidatedLoad(text);
				}
				else
				{
					Load(text);
				}
			}
		}

		public void UpdateOutputAssets(OutputManager outputManager)
		{
			foreach (BinaryAsset value in outputManager.Assets.Values)
			{
				if (value.IsOutputAsset)
				{
					_Current.OutputAssets.Add(new OutputAsset(value));
				}
			}
		}

		private void UpdateDocumentHashes()
		{
			if (_Current.DependentFiles.Count == 0)
			{
				_Current.DependentFileHash = 0u;
			}
			else
			{
				using MemoryStream memoryStream = new MemoryStream();
				BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
				foreach (string dependentFile in _Current.DependentFiles)
				{
					FileHashItem item = null;
					if (TryGetFileHashItem(dependentFile, out item))
					{
						binaryWriter.Write(HashProvider.GetTextHash(item.Path.ToLower()));
						binaryWriter.Write(item.Hash);
					}
				}
				byte[] array = memoryStream.ToArray();
				if (array.Length > 0)
				{
					_Current.DependentFileHash = FastHash.GetHashCode(array);
				}
				else
				{
					_Current.DependentFileHash = 0u;
				}
			}
			if (_Current.InclusionItems.Count == 0)
			{
				_Current.IncludePathHash = 0u;
				return;
			}
			_Current.IncludePathHash = (uint)_Current.InclusionItems.Count;
			foreach (InclusionItem inclusionItem in _Current.InclusionItems)
			{
				FileHashItem item2 = null;
				if (TryGetFileHashItem(inclusionItem.LogicalPath, out item2))
				{
					_Current.IncludePathHash = FastHash.GetHashCode(_Current.IncludePathHash, item2.Path.ToLower());
				}
			}
		}

		public void LoadXml(bool updateDependentFiles)
		{
			if (_Current.XmlDocument != null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Document {0} attempted to load twice.", _Current.SourcePath);
			}
			FileNameXmlResolver xmlResolver = FileNameResolver.GetXmlResolver(SourceDirectory);
			if (!File.Exists(_Current.SourcePath))
			{
				string s = "<?xml version='1.0' encoding='UTF-8'?>\n<AssetDeclaration xmlns=\"uri:ea.com:eala:asset\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n</AssetDeclaration>";
				_Current.XmlReader = XmlReader.Create(new StringReader(s));
			}
			else
			{
				_Current.XmlReader = XmlReader.Create(_Current.SourcePath);
			}
			_Current.XmlDocument = new XmlDocument();
			XmlReader xmlReader = XIncludingReaderWrapper.GetReader(_Current.XmlReader, xmlResolver);
			if (xmlReader == null)
			{
				xmlReader = _Current.XmlReader;
			}
			_Current.NodeSourceInfoSet = new LinkedList<XmlNodeWithMetaData>();
			try
			{
				bool flag = _Current.LogicalSourcePath.EndsWith(".xml");
				if (flag)
				{
					_Current.XmlDocument.NodeInserted += NodeInsertedHandler;
				}
				_Current.XmlDocument.Load(xmlReader);
				if (flag)
				{
					_Current.XmlDocument.NodeInserted -= NodeInsertedHandler;
				}
			}
			catch (XmlSchemaValidationException innerException)
			{
				throw new BinaryAssetBuilderException(innerException, ErrorCode.SchemaValidation);
			}
			catch (XmlException innerException2)
			{
				throw new BinaryAssetBuilderException(innerException2, ErrorCode.XmlFormattingError);
			}
			finally
			{
				_Current.XmlReader.Close();
			}
			if (updateDependentFiles)
			{
				foreach (PathMapItem path in xmlResolver.Paths)
				{
					_Current.DependentFiles.Add(path.SourceUri.ToLower());
				}
			}
			_Current.NamespaceManager = new XmlNamespaceManager(xmlReader.NameTable);
			_Current.NamespaceManager.AddNamespace("ea", "uri:ea.com:eala:asset");
			_Current.NamespaceManager.AddNamespace("ms", "urn:schemas-microsoft-com:xslt");
		}

		public void InplaceLoad(string reason)
		{
			InternalLoad(reason, LoadType.InplaceLoad);
		}

		private void Load(string reason)
		{
			_Current.FromScratch();
			InternalLoad(reason, LoadType.FromScratch);
		}

		private void InternalLoad(string reason, LoadType type)
		{
			SetChanged(reason);
			if (State == DocumentState.Shallow)
			{
				_Tracer.TraceInfo("Reloading 'file://{0}'. Reason: {1}", SourcePath, _Current.ChangeReason);
			}
			else
			{
				_Tracer.TraceInfo("Loading 'file://{0}'.", SourcePath);
			}
			LoadXml(type == LoadType.FromScratch);
			GatherUnvalidatedTags();
			GatherDefines();
			if (type != 0)
			{
				GatherUnvalidatedIncludes();
			}
			GatherUnvalidatedInstances();
			_Current.State = DocumentState.Loaded;
			_Current.IsLoaded = true;
		}

		private void PrevalidatedLoad(string reason)
		{
			if (_Current.State != DocumentState.Complete)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Prevalidation load for {0} was called because of '{1}' when document has not been previously validated", SourcePath, reason);
			}
			LoadXml(updateDependentFiles: false);
			GatherUnvalidatedTags();
			GatherDefines();
			GatherUnvalidatedIncludes();
			GatherUnvalidatedInstances();
			_Current.State = DocumentState.Loaded;
			_Current.IsLoaded = true;
			ReloadedForInheritance = true;
		}

		private void NodeInsertedHandler(object sender, XmlNodeChangedEventArgs e)
		{
			IXmlLineInfo xmlLineInfo = _Current.XmlReader as IXmlLineInfo;
			if (xmlLineInfo.HasLineInfo())
			{
				_Current.NodeSourceInfoSet.AddFirst(new XmlNodeWithMetaData(e.Node, xmlLineInfo.LineNumber));
			}
		}

		public void FromLastHack()
		{
			if (_Current == null)
			{
				_Current = new CurrentState();
				_Current.FromLast(this, _Last);
			}
		}

		public void Open(DocumentProcessor documentProcessor, FileHashItem hashItem, string logicalPath, string configuration)
		{
			if (_Current == null || _Current.State != DocumentState.Complete)
			{
				_Current = new CurrentState(documentProcessor, hashItem, logicalPath, configuration);
				if (_Last != null)
				{
					_Current.FromLast(this, _Last);
				}
				else
				{
					Load("New Document");
				}
			}
		}

		public void ResetState()
		{
			_Current = null;
		}

		public override string ToString()
		{
			return $"{Path.GetFileName(SourcePath)} {State}";
		}

		public void MakeCacheable()
		{
			if (_Current == null || _Last != null)
			{
				return;
			}
			_Last = new LastState(_Current);
			foreach (InstanceDeclaration selfInstance in _Current.SelfInstances)
			{
				selfInstance.MakeCacheable();
			}
		}

		public void MakeComplete()
		{
			_Current.State = DocumentState.Complete;
			foreach (InstanceDeclaration selfInstance in SelfInstances)
			{
				selfInstance.MakeComplete();
			}
			_Last = null;
		}

		public void AddStreamsHints(string[] streams)
		{
			foreach (string item in streams)
			{
				if (!_Current.StreamHints.Contains(item))
				{
					_Current.StreamHints.Add(item);
				}
			}
		}

		public void Validate()
		{
			_Current.XmlDocument.Schemas.Add(_Current.DocumentProcessor.SchemaSet.Schemas);
			_Current.XmlDocument.Validate(HandleValidationEvents);
			_Current.NodeSourceInfoSet.Clear();
			_Current.NodeSourceInfoSet = null;
			ValidateInstances();
			UpdateDocumentHashes();
		}

		private bool TryGetFileHashItem(string logicalPath, out FileHashItem item)
		{
			string path = FileNameResolver.ResolvePath(SourceDirectory, logicalPath);
			return _Current.DocumentProcessor.Cache.TryGetFile(path, _Current.ConfigurationName, Settings.Current.TargetPlatform, out item);
		}

		public void HandleValidationEvents(object sender, ValidationEventArgs e)
		{
			if (e.Exception is XmlSchemaValidationException ex)
			{
				XmlNode node = ex.SourceObject as XmlNode;
				LinkedListNode<XmlNodeWithMetaData> linkedListNode = _Current.NodeSourceInfoSet.Find(new XmlNodeWithMetaData(node, 0));
				if (linkedListNode != null && linkedListNode.Value != null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.SchemaValidation, "XML validation error encountered in '{0}' near line {1}:\n   {2}", SourcePath, linkedListNode.Value.LineNumber, e.Message);
				}
			}
			throw new BinaryAssetBuilderException(e.Exception, ErrorCode.SchemaValidation, "XML validation error encountered in '{0}'", SourcePath);
		}

		public void EvaluateDefinitions()
		{
			IExpressionEvaluator evaluator = ExpressionEvaluatorWrapper.GetEvaluator(this);
			foreach (Definition selfDefine in SelfDefines)
			{
				if (selfDefine.OriginalValue != null)
				{
					selfDefine.EvaluatedValue = null;
					evaluator.EvaluateDefinition(selfDefine);
				}
				Definition value = null;
				if (AllDefines.TryGetValue(selfDefine.Name, out value) && selfDefine.Document != value.Document && !selfDefine.IsOverride)
				{
					throw new BinaryAssetBuilderException(ErrorCode.DuplicateDefine, "Definition {0} defined in {1} is already defined in {2}", selfDefine.Name, selfDefine.Document.SourcePath, value.Document.SourcePath);
				}
				AllDefines[selfDefine.Name] = selfDefine;
			}
		}

		public bool ValidateCachedDefines()
		{
			foreach (KeyValuePair<string, string> usedDefine in UsedDefines)
			{
				if (usedDefine.Value != AllDefines.GetEvaluatedValue(usedDefine.Key))
				{
					return false;
				}
			}
			return true;
		}

		public bool ValidateInheritFromSources()
		{
			foreach (InstanceDeclaration selfInstance in SelfInstances)
			{
				if (selfInstance.InheritFromHandle == null)
				{
					continue;
				}
				FindLocation location = FindLocation.None;
				InstanceDeclaration instanceDeclaration = FindInstance(selfInstance.InheritFromHandle, (selfInstance.InheritFromHandle == selfInstance.Handle) ? FindLocation.Self : FindLocation.None, out location);
				if (instanceDeclaration == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0} attempts to inherit from non-existing instance {1}.", selfInstance, selfInstance.InheritFromHandle);
				}
				switch (location)
				{
				case FindLocation.Tentative:
				{
					bool flag = false;
					foreach (InclusionItem inclusionItem in _Current.InclusionItems)
					{
						if (inclusionItem.Document == instanceDeclaration.Document)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0} attempts to inherit from instance {1} which is not directly included.", selfInstance, selfInstance.InheritFromHandle);
					}
					break;
				}
				default:
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0} attempts to inherit from instance {1} which is not included by 'instance'.", selfInstance, selfInstance.InheritFromHandle);
				case FindLocation.Self:
					break;
				}
				if (instanceDeclaration.PrevalidationXmlHash != selfInstance.InheritFromXmlHash)
				{
					return false;
				}
			}
			return true;
		}

		public void ProcessExpressions()
		{
			IExpressionEvaluator evaluator = ExpressionEvaluatorWrapper.GetEvaluator(this);
			if (evaluator == null)
			{
				return;
			}
			foreach (InstanceDeclaration selfInstance in SelfInstances)
			{
				ProcessExpressionsInNode(evaluator, selfInstance.XmlNode);
			}
		}

		private void ProcessExpressionsInNode(IExpressionEvaluator eval, XmlNode node)
		{
			if (node.Attributes != null)
			{
				foreach (XmlAttribute attribute in node.Attributes)
				{
					if (attribute.Value.Length > 0 && attribute.Value[0] == '=')
					{
						attribute.Value = eval.Evaluate(attribute.Value);
					}
				}
			}
			if (node.Value != null && node.Value.Length > 0 && node.Value[0] == '=')
			{
				node.Value = eval.Evaluate(node.Value);
			}
			if (node.ChildNodes == null)
			{
				return;
			}
			foreach (XmlNode childNode in node.ChildNodes)
			{
				ProcessExpressionsInNode(eval, childNode);
			}
		}

		private void OverrideInstance(InstanceDeclaration instance)
		{
			if (instance.InheritFromHandle != null)
			{
				XmlNode xmlNode = null;
				FindLocation location = FindLocation.None;
				InstanceDeclaration instanceDeclaration = FindInstance(instance.InheritFromHandle, (instance.InheritFromHandle == instance.Handle) ? FindLocation.Self : FindLocation.None, out location);
				if (instanceDeclaration == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0} attempts to inherit from or override non-existing instance {1}", instance.Handle, instance.InheritFromHandle);
				}
				if (!instanceDeclaration.IsInheritable && location != FindLocation.Self)
				{
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {1} cannot be overriden because it is not of type BaseInheritableAsset", instance.Handle, instance.InheritFromHandle);
				}
				switch (location)
				{
				case FindLocation.Tentative:
					foreach (InclusionItem inclusionItem in _Current.InclusionItems)
					{
						if (inclusionItem.Document == instanceDeclaration.Document)
						{
							xmlNode = instanceDeclaration.XmlNode;
							break;
						}
					}
					break;
				case FindLocation.Self:
					if (instanceDeclaration.PrevalidationXmlHash == 0)
					{
						OverrideInstance(instanceDeclaration);
					}
					xmlNode = instanceDeclaration.XmlNode;
					break;
				case FindLocation.None:
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0} attempts to inherit from an instance {1} which could not be found.", instance.Handle, instance.InheritFromHandle);
				default:
					throw new BinaryAssetBuilderException(ErrorCode.InheritFromError, "Instance {0}\n  in document '{1}'\n  attempts to inherit from instance {2}\n  from document '{3}'\n  which does not appear to be included by 'instance'.", instance.Handle, instance.Document.SourcePath, instanceDeclaration, instanceDeclaration.Document.SourcePath);
				}
				if (xmlNode == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Instance {0} attempts to inherit from instance {1} but source XML is missing.", instance.Handle, instance.InheritFromHandle);
				}
				XmlNode xmlNode2 = NodeJoiner.Override(_Current.DocumentProcessor.SchemaSet.Schemas, XmlDocument, xmlNode, instance.XmlNode);
				XmlNode parentNode = instance.XmlNode.ParentNode;
				parentNode.RemoveChild(instance.XmlNode);
				parentNode.AppendChild(xmlNode2);
				instance.XmlNode = xmlNode2;
				instance.InheritFromXmlHash = instanceDeclaration.PrevalidationXmlHash;
			}
			XmlNode node = instance.XmlNode;
			instance.PrevalidationXmlHash = HashProvider.GetXmlHash(ref node);
		}

		public void MergeInstances()
		{
			Instances.Clear();
			Instances.Add(SelfInstances);
			Instances.Add(AllInstances);
		}

		public void ProcessInstances(OutputManager outputManager, ref int instancesCompiledCount, ref int instancesCopiedFromCacheCount)
		{
			Logger.info($"AssetDeclarationDocument.ProcessInstances begin: {SourcePath} | SelfInstances count {SelfInstances.Count}");
			if (ReloadedForInheritance)
			{
				return;
			}
			foreach (InstanceDeclaration selfInstance in SelfInstances)
			{
				ExtendedTypeInformation extendedTypeInformation = _Current.DocumentProcessor.Plugins.GetExtendedTypeInformation(selfInstance.Handle.TypeId);
				selfInstance.HasCustomData = extendedTypeInformation.HasCustomData;
				BinaryAsset binaryAsset = outputManager.GetBinaryAsset(selfInstance, isOutputAsset: true);
				if (binaryAsset != null)
				{
					if (binaryAsset.GetLocation(AssetLocation.All, AssetLocationOption.None) == AssetLocation.None)
					{
						CompileInstance(binaryAsset, selfInstance);
					}
					VerifyInstance(binaryAsset, selfInstance);
					AssetLocation assetLocation = binaryAsset.Commit();
					CountCommitSource(assetLocation, ref instancesCompiledCount, ref instancesCopiedFromCacheCount);
					if (assetLocation != AssetLocation.BasePatchStream)
					{
						_Current.DocumentProcessor.AddLastWrittenAsset(binaryAsset);
					}
					if (!selfInstance.IsInheritable)
					{
						selfInstance.XmlNode = null;
					}
				}
			}
			Logger.info($"AssetDeclarationDocument.ProcessInstances end");
		}

		private void CountCommitSource(AssetLocation commitSource, ref int instancesCompiledCount, ref int instancesCopiedFromCacheCount)
		{
			switch (commitSource)
			{
			case AssetLocation.Memory:
				instancesCompiledCount++;
				break;
			case AssetLocation.Local:
				instancesCopiedFromCacheCount++;
				break;
			case AssetLocation.Cache:
				instancesCopiedFromCacheCount++;
				break;
			}
		}

		private void VerifyInstance(BinaryAsset asset, InstanceDeclaration declaration)
		{
			if (_Current.IsLoaded)
			{
				IAssetBuilderVerifierPlugin plugin = _Current.DocumentProcessor.VerifierPlugins.GetPlugin(asset.Instance.Handle.TypeId);
				if (!plugin.VerifyInstance(declaration))
				{
					_Current.VerificationErrors = true;
					throw new BinaryAssetBuilderException(ErrorCode.GameDataVerification, "FATAL: An asset failed the Game Data Verification step.  See previous output.");
				}
			}
		}

		private void CompileInstance(BinaryAsset asset, InstanceDeclaration declaration)
		{
			_Tracer.Message("{0}: Compiling {1}", Path.GetFileName(_Current.SourcePath), asset.Instance.ToString());
			if (asset.Instance.XmlNode == null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.DependencyCacheFailure, "Need to compile instance {0} but XML is not loaded. This is a bug. Please notify cnc3technicalarchitecture@ea.com.", asset.Instance);
			}
			if (declaration.HasCustomData)
			{
				declaration.CustomDataPath = Path.Combine(asset.CustomDataOutputDirectory, asset.FileBase + ".cdata");
				if (!Directory.Exists(asset.CustomDataOutputDirectory))
				{
					Directory.CreateDirectory(asset.CustomDataOutputDirectory);
				}
			}
			DateTime now = DateTime.Now;
			IAssetBuilderPlugin plugin = _Current.DocumentProcessor.Plugins.GetPlugin(asset.Instance.Handle.TypeId);
			asset.Buffer = plugin.ProcessInstance(asset.Instance);
			_Current.DocumentProcessor.AddCompileTime(asset.Instance.Handle, DateTime.Now - now);
		}

		public void ProcessOverrides()
		{
			foreach (InstanceDeclaration selfInstance in SelfInstances)
			{
				OverrideInstance(selfInstance);
			}
		}

		private void GatherUnvalidatedTags()
		{
			Tags.Clear();
			XPathNavigator xPathNavigator = _Current.XmlDocument.CreateNavigator();
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Evaluate("/ea:AssetDeclaration/ea:Tags/child::*", _Current.NamespaceManager) as XPathNodeIterator;
			foreach (XPathNavigator item in xPathNodeIterator)
			{
				_Current.Tags.Add(item.GetAttribute("name", ""), item.GetAttribute("value", ""));
			}
		}

		private void GatherDefines()
		{
			SelfDefines.Clear();
			XPathNavigator xPathNavigator = _Current.XmlDocument.CreateNavigator();
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Evaluate("/ea:AssetDeclaration/ea:Defines/child::*", _Current.NamespaceManager) as XPathNodeIterator;
			foreach (XPathNavigator item in xPathNodeIterator)
			{
				Definition definition = new Definition();
				definition.Document = this;
				definition.OriginalValue = item.GetAttribute("value", "");
				definition.Name = item.GetAttribute("name", "");
				definition.IsOverride = item.GetAttribute("override", "") == "true";
				_Current.SelfDefines.Add(definition);
			}
		}

		private void GatherUnvalidatedIncludes()
		{
			InclusionItems.Clear();
			XPathNavigator xPathNavigator = _Current.XmlDocument.CreateNavigator();
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Evaluate("/ea:AssetDeclaration/ea:Includes/child::*", _Current.NamespaceManager) as XPathNodeIterator;
			foreach (XPathNavigator item in xPathNodeIterator)
			{
				string text = item.GetAttribute("source", "").Trim().ToLower();
				string physicalPath = FileNameResolver.ResolvePath(SourceDirectory, text).ToLower();
				InclusionItem inclusionItem = new InclusionItem(text, physicalPath, (InclusionType)Enum.Parse(typeof(InclusionType), item.GetAttribute("type", ""), ignoreCase: true));
				_Current.InclusionItems.Add(inclusionItem);
				if (inclusionItem.Type == InclusionType.Instance)
				{
					_Current.DependentFiles.Add(text);
				}
			}
		}

		private void GatherUnvalidatedInstances()
		{
			SelfInstances.Clear();
			XmlNodeList xmlNodeList = _Current.XmlDocument.SelectNodes("/ea:AssetDeclaration/child::*", _Current.NamespaceManager);
			foreach (XmlNode item in xmlNodeList)
			{
				if (item.Name == "Includes" || item.Name == "Tags" || item.Name == "Defines")
				{
					continue;
				}
				InstanceDeclaration instanceDeclaration = new InstanceDeclaration(this);
				instanceDeclaration.XmlNode = item;
				if (!SelfInstances.TryAdd(instanceDeclaration))
				{
					InstanceDeclaration instanceDeclaration2 = SelfInstances[instanceDeclaration.Handle];
					if (instanceDeclaration.Handle.InstanceName == instanceDeclaration2.Handle.InstanceName)
					{
						throw new BinaryAssetBuilderException(ErrorCode.DuplicateInstance, "Duplicate Instance: {0}, in {1} and {2}", instanceDeclaration, instanceDeclaration.Document.SourcePath, instanceDeclaration2.Document.SourcePath);
					}
					throw new BinaryAssetBuilderException(ErrorCode.DuplicateInstance, "Duplicate Instance: {0} (other is {1})", instanceDeclaration, instanceDeclaration2);
				}
			}
		}

		private void ValidateInstances()
		{
			StringCollection derivedTypes = _Current.DocumentProcessor.SchemaSet.GetDerivedTypes("BaseInheritableAsset");
			foreach (InstanceDeclaration selfInstance in _Current.SelfInstances)
			{
				ExtendedTypeInformation extendedTypeInformation = _Current.DocumentProcessor.Plugins.GetExtendedTypeInformation(selfInstance.Handle.TypeId);
				uint textHash = HashProvider.GetTextHash(extendedTypeInformation.ProcessingHash, 11u.ToString());
				selfInstance.Handle.TypeHash = extendedTypeInformation.TypeHash;
				XmlNode node = selfInstance.XmlNode;
				selfInstance.Handle.InstanceHash = HashProvider.GetXmlHash(textHash, ref node);
				selfInstance.ProcessingHash = extendedTypeInformation.ProcessingHash;
				if (derivedTypes != null)
				{
					selfInstance.IsInheritable = derivedTypes.Contains(selfInstance.XmlNode.SchemaInfo.SchemaType.Name);
				}
				if (selfInstance.Handle.TypeName != selfInstance.XmlNode.SchemaInfo.SchemaType.Name)
				{
					throw new BinaryAssetBuilderException(ErrorCode.SchemaValidation, "Type name and element name do not match for {0}.", selfInstance.Handle);
				}
				if (selfInstance.Handle.TypeHash == 0)
				{
					_Tracer.TraceWarning("No type hash found for type {0}.", selfInstance.Handle.TypeName);
				}
				if (Settings.Current.OutputIntermediateXml)
				{
					OutputIntermediateXml(selfInstance);
				}
				XPathNavigator xPathNavigator = selfInstance.XmlNode.CreateNavigator();
				XPathNodeIterator xPathNodeIterator = xPathNavigator.SelectDescendants("", "uri:ea.com:eala:asset", matchSelf: true);
				MemoryStream memoryStream = new MemoryStream();
				BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
				foreach (XPathNavigator item2 in xPathNodeIterator)
				{
					HandleReferenceType(selfInstance, item2, binaryWriter);
					if (item2.HasAttributes)
					{
						item2.MoveToFirstAttribute();
						do
						{
							HandleReferenceType(selfInstance, item2, binaryWriter);
						}
						while (item2.MoveToNextAttribute());
						item2.MoveToParent();
					}
					if (item2.SchemaInfo != null && item2.SchemaInfo.SchemaType != null && item2.SchemaInfo.SchemaType.Name != null)
					{
						item2.CreateAttribute("", "TypeId", "", HashProvider.GetCaseSensitiveSymbolHash(item2.SchemaInfo.SchemaType.Name).ToString());
					}
				}
				foreach (string referencedFile in selfInstance.ReferencedFiles)
				{
					FileHashItem item = null;
					TryGetFileHashItem(referencedFile, out item);
					binaryWriter.Write(item.Hash);
				}
				if (memoryStream.Length > 0)
				{
					selfInstance.Handle.InstanceHash ^= FastHash.GetHashCode(memoryStream.GetBuffer());
				}
			}
		}

		private void OutputIntermediateXml(InstanceDeclaration instance)
		{
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.NewLineOnAttributes = true;
			xmlWriterSettings.Encoding = Encoding.UTF8;
			xmlWriterSettings.Indent = true;
			xmlWriterSettings.IndentChars = "\t";
			string text = Path.Combine(Settings.Current.IntermediateOutputDirectory, "FinalXml_" + Settings.Current.TargetPlatform);
			string text2 = Path.Combine(instance.Handle.TypeName, instance.Handle.InstanceName);
			Directory.CreateDirectory(text);
			Directory.CreateDirectory(Path.Combine(text, instance.Handle.TypeName));
			string outputFileName = Path.Combine(text, text2 + Path.GetExtension(LogicalSourcePath));
			try
			{
				XmlWriter xmlWriter = XmlWriter.Create(outputFileName, xmlWriterSettings);
				XmlElement xmlElement = ReconstructAssetDeclaration(instance);
				xmlElement.WriteTo(xmlWriter);
				xmlWriter.Close();
			}
			catch (Exception ex)
			{
				_Tracer.TraceWarning("Warning: Unable to output {0}, reason: {1}", text2, ex.Message);
			}
		}

		private XmlElement ReconstructAssetDeclaration(InstanceDeclaration instance)
		{
			XmlElement xmlElement = XmlDocument.CreateElement("AssetDeclaration", Settings.Current.AssetNamespace);
			XmlElement xmlElement2 = XmlDocument.CreateElement("Tags", Settings.Current.AssetNamespace);
			XmlElement xmlElement3 = XmlDocument.CreateElement("Tag", Settings.Current.AssetNamespace);
			XmlAttribute xmlAttribute = XmlDocument.CreateAttribute("name");
			xmlAttribute.Value = "SourceXml";
			XmlAttribute xmlAttribute2 = XmlDocument.CreateAttribute("tag");
			xmlAttribute2.Value = LogicalSourcePath;
			xmlElement3.Attributes.Append(xmlAttribute);
			xmlElement3.Attributes.Append(xmlAttribute2);
			xmlElement2.AppendChild(xmlElement3);
			xmlElement.AppendChild(xmlElement2);
			xmlElement.AppendChild(instance.XmlNode);
			return xmlElement;
		}

		public void RecordStringHashes()
		{
			foreach (InstanceDeclaration selfInstance in _Current.SelfInstances)
			{
				HashProvider.RecordHash(selfInstance.Handle);
				foreach (InstanceHandle referencedInstance in selfInstance.ReferencedInstances)
				{
					HashProvider.RecordHash(referencedInstance);
				}
				foreach (InstanceHandle weakReferencedInstance in selfInstance.WeakReferencedInstances)
				{
					HashProvider.RecordHash(weakReferencedInstance);
				}
			}
		}

		private string GetRefTypeName(XmlAttribute[] unhandledAttributes)
		{
			if (unhandledAttributes != null)
			{
				foreach (XmlAttribute xmlAttribute in unhandledAttributes)
				{
					string localName;
					if (xmlAttribute.NamespaceURI == "uri:ea.com:eala:asset:schema" && (localName = xmlAttribute.LocalName) != null && localName == "refType")
					{
						return xmlAttribute.Value;
					}
				}
			}
			return null;
		}

		private void HandleReferenceType(InstanceDeclaration instance, XPathNavigator navigator, BinaryWriter refTypeIds)
		{
			if (navigator.SchemaInfo == null || navigator.SchemaInfo.SchemaType == null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Element {0} in instance {1} doesn't have a schema type.", navigator.Name, instance);
			}
			bool flag = XmlSchemaType.IsDerivedFrom(navigator.SchemaInfo.SchemaType, _Current.DocumentProcessor.SchemaSet.XmlWeakReferenceType, XmlSchemaDerivationMethod.None);
			bool flag2 = !flag && XmlSchemaType.IsDerivedFrom(navigator.SchemaInfo.SchemaType, _Current.DocumentProcessor.SchemaSet.XmlAssetReferenceType, XmlSchemaDerivationMethod.None);
			if (flag || flag2)
			{
				HandleAssetReferenceType(navigator, ref instance, refTypeIds, flag2);
			}
			else if (XmlSchemaType.IsDerivedFrom(navigator.SchemaInfo.SchemaType, _Current.DocumentProcessor.SchemaSet.XmlFileReferenceType, XmlSchemaDerivationMethod.None))
			{
				HandleFileReferenceType(navigator, ref instance);
			}
			else if (_Current.DocumentProcessor.SchemaSet.IsHashableType(navigator.SchemaInfo.SchemaType))
			{
				HashProvider.RecordHash(navigator.SchemaInfo.SchemaType, navigator.Value);
			}
		}

		private void HandleFileReferenceType(XPathNavigator navigator, ref InstanceDeclaration instance)
		{
			string text = navigator.Value.Trim().ToLower();
			FileHashItem item = null;
			TryGetFileHashItem(text, out item);
			string value = item.Path.ToLower();
			_Current.DependentFiles.Add(text);
			navigator.SetValue(value);
			instance.ReferencedFiles.Add(text);
		}

		private void HandleAssetReferenceType(XPathNavigator navigator, ref InstanceDeclaration instance, BinaryWriter refTypeIds, bool isAssetRef)
		{
			string[] array = navigator.Value.Trim().Split('\\');
			string text = array[0];
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			InstanceHandle instanceHandle = new InstanceHandle(text);
			string text2 = null;
			if (navigator.SchemaInfo.SchemaElement != null)
			{
				text2 = GetRefTypeName(navigator.SchemaInfo.SchemaElement.UnhandledAttributes);
			}
			else if (navigator.SchemaInfo.SchemaAttribute != null)
			{
				text2 = GetRefTypeName(navigator.SchemaInfo.SchemaAttribute.UnhandledAttributes);
			}
			if (text2 == null && navigator.SchemaInfo.SchemaType != null)
			{
				text2 = GetRefTypeName(navigator.SchemaInfo.SchemaType.UnhandledAttributes);
				if (text2 == null && navigator.SchemaInfo.SchemaType.DerivedBy == XmlSchemaDerivationMethod.Extension)
				{
					text2 = GetRefTypeName(navigator.SchemaInfo.SchemaType.BaseXmlSchemaType.UnhandledAttributes);
				}
			}
			if (text2 == null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Asset reference to '{0}' in '{1}' does not have type (xas:refType missing in schema).", text, instance);
			}
			refTypeIds.Write(HashProvider.GetCaseSensitiveSymbolHash(text2));
			if (instanceHandle.TypeId == 0)
			{
				instanceHandle.TypeName = text2;
			}
			else if (isAssetRef)
			{
				XmlSchemaType xmlType = _Current.DocumentProcessor.SchemaSet.GetXmlType(instanceHandle.TypeName);
				XmlSchemaType xmlType2 = _Current.DocumentProcessor.SchemaSet.GetXmlType(text2);
				if (xmlType2 == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Unable to establish schema type of underlying reference type '{0}'. Make sure it is defined and included in the schema set.", text2);
				}
				if (xmlType == null)
				{
					throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Unable to establish schema type of referenced instance '{0}'. Make sure it is defined and included in the schema set.", instanceHandle.Name);
				}
				if (!XmlSchemaType.IsDerivedFrom(xmlType, xmlType2, XmlSchemaDerivationMethod.None))
				{
					throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Type of instance '{0}' referenced from '{1}' does not appear to be equal to or derived from required reference type '{2}'.", instanceHandle.Name, instance.Handle.Name, text2);
				}
			}
			if (isAssetRef)
			{
				instance.ReferencedInstances.Add(instanceHandle);
				navigator.SetValue($"{text}\\{instance.ReferencedInstances.Count - 1}");
			}
			else
			{
				instance.WeakReferencedInstances.Add(instanceHandle);
			}
		}

		public InstanceDeclaration FindInstance(InstanceHandle handle, FindLocation skipLocation, out FindLocation location)
		{
			location = FindLocation.None;
			InstanceDeclaration value = null;
			if (_Current.SelfInstances != null && skipLocation != FindLocation.Self && _Current.SelfInstances.TryGetValue(handle, out value))
			{
				location = FindLocation.Self;
				return value;
			}
			if (_Current.AllInstances != null && skipLocation != FindLocation.All && _Current.AllInstances.TryGetValue(handle, out value))
			{
				location = FindLocation.All;
				return value;
			}
			if (_Current.TentativeInstances != null && skipLocation != FindLocation.Tentative && _Current.TentativeInstances.TryGetValue(handle, out value))
			{
				location = FindLocation.Tentative;
				return value;
			}
			if (_Current.ReferenceInstances != null && skipLocation != FindLocation.External && _Current.ReferenceInstances.TryGetValue(handle, out value))
			{
				location = FindLocation.External;
				return value;
			}
			return null;
		}

		private InstanceDeclaration ResolveReference(InstanceDeclaration parentInstance, InstanceHandle referenceHandle, out FindLocation location)
		{
			InstanceDeclaration instanceDeclaration = FindInstance(referenceHandle, FindLocation.None, out location);
			if (instanceDeclaration == null)
			{
				InstanceHandle instanceHandle = new InstanceHandle(referenceHandle.TypeName, referenceHandle.InstanceName);
				StringCollection derivedTypes = _Current.DocumentProcessor.SchemaSet.GetDerivedTypes(referenceHandle.TypeName);
				if (derivedTypes != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					int num = 0;
					StringEnumerator enumerator = derivedTypes.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							string current = enumerator.Current;
							instanceHandle.TypeName = current;
							FindLocation location2;
							InstanceDeclaration instanceDeclaration2 = FindInstance(instanceHandle, FindLocation.None, out location2);
							if (instanceDeclaration2 != null)
							{
								instanceDeclaration = instanceDeclaration2;
								location = location2;
								if (num > 0)
								{
									stringBuilder.AppendFormat(", ");
								}
								stringBuilder.AppendFormat("'{0}'", instanceHandle.Name);
								num++;
							}
						}
					}
					finally
					{
						if (enumerator is IDisposable disposable)
						{
							disposable.Dispose();
						}
					}
					if (num > 1)
					{
						throw new BinaryAssetBuilderException(ErrorCode.ReferencingError, "Reference to instance '{0}' from '{1}' in 'file://{2}' is ambiguous. Possible matches: {3}.", referenceHandle.Name, parentInstance.Handle.Name, parentInstance.Document.SourcePath, stringBuilder);
					}
				}
			}
			return instanceDeclaration;
		}

		private void AddOutputInstance(InstanceDeclaration instance)
		{
			if (instance.Handle.TypeHash == 0 || _Current.OutputInstanceSet.ContainsKey(instance.Handle))
			{
				return;
			}
			_Current.OutputInstanceSet.Add(instance.Handle, instance);
			if (instance.ValidatedReferencedInstances != null)
			{
				foreach (InstanceHandle validatedReferencedInstance in instance.ValidatedReferencedInstances)
				{
					FindLocation location = FindLocation.None;
					InstanceDeclaration instanceDeclaration = ResolveReference(instance, validatedReferencedInstance, out location);
					if (instanceDeclaration != null && location != FindLocation.External)
					{
						AddOutputInstance(instanceDeclaration);
					}
				}
				return;
			}
			instance.ValidatedReferencedInstances = new List2<InstanceHandle>();
			instance.AllDependentInstances = new InstanceHandleSet();
			foreach (InstanceHandle referencedInstance in instance.ReferencedInstances)
			{
				FindLocation location2 = FindLocation.None;
				InstanceDeclaration instanceDeclaration2 = ResolveReference(instance, referencedInstance, out location2);
				if (instanceDeclaration2 != null)
				{
					if (location2 != FindLocation.External)
					{
						instance.AllDependentInstances.TryAdd(instanceDeclaration2.Handle);
						AddOutputInstance(instanceDeclaration2);
						if (instanceDeclaration2.AllDependentInstances != null)
						{
							foreach (InstanceHandle allDependentInstance in instanceDeclaration2.AllDependentInstances)
							{
								instance.AllDependentInstances.TryAdd(allDependentInstance);
							}
						}
					}
					instance.ValidatedReferencedInstances.Add(instanceDeclaration2.Handle);
				}
				else
				{
					if (Settings.Current.ErrorLevel > 0)
					{
						throw new BinaryAssetBuilderException(ErrorCode.UnknownReference, "Unknown referenced asset: {0}", referencedInstance);
					}
					if (_Current.DocumentProcessor.MissingReferences.TryAdd(referencedInstance))
					{
						_Tracer.TraceWarning("Unknown asset '{0}' referenced from '{1}' in 'file://{2}'", referencedInstance.Name, instance.Handle.Name, instance.Document.SourcePath);
					}
					instance.ValidatedReferencedInstances.Add(referencedInstance);
				}
			}
			foreach (InstanceHandle weakReferencedInstance in instance.WeakReferencedInstances)
			{
				FindLocation location3 = FindLocation.None;
				InstanceDeclaration instanceDeclaration3 = ResolveReference(instance, weakReferencedInstance, out location3);
				if (instanceDeclaration3 != null && location3 == FindLocation.Tentative)
				{
					AddOutputInstance(instanceDeclaration3);
					instance.ValidatedReferencedInstances.Add(instanceDeclaration3.Handle);
				}
			}
			foreach (string referencedFile in instance.ReferencedFiles)
			{
				FileHashItem item = null;
				if (!instance.Document.TryGetFileHashItem(referencedFile, out item))
				{
					if (Settings.Current.ErrorLevel > 0)
					{
						throw new BinaryAssetBuilderException(ErrorCode.FileNotFound, "Referenced file not found: {0}", referencedFile);
					}
					_Tracer.TraceWarning("Referenced file not found: {0}", referencedFile);
				}
			}
		}

		public void StableSort(OutputManager outputManager)
		{
			_Tracer.TraceInfo("Stable sorting assets");
			List2<InstanceDeclaration> finalList = new List2<InstanceDeclaration>();
			if (outputManager.BasePatchStreamManifest != null)
			{
				_Tracer.TraceInfo("Merging base stream assets from {0}", outputManager.BasePatchStream);
				StableSort_InitializeFromBaseManifest(ref outputManager, ref _Current.OutputInstances, ref finalList);
			}
			_Tracer.TraceInfo("Sorting by schema dependencies");
			TypeDepCompare typeDepCompare = new TypeDepCompare(_Current.DocumentProcessor.SchemaSet.AssetDependencies);
			_Current.OutputInstances.Sort(typeDepCompare);
			_Tracer.TraceInfo("Sorting by asset dependencies");
			foreach (InstanceDeclaration outputInstance in _Current.OutputInstances)
			{
				int index = StableSort_CalcInsertPosition(finalList, outputInstance, typeDepCompare);
				finalList.Insert(index, outputInstance);
			}
			_Current.OutputInstances = finalList;
		}

		private static void StableSort_InitializeFromBaseManifest(ref OutputManager outputManager, ref List2<InstanceDeclaration> outputInstances, ref List2<InstanceDeclaration> finalList)
		{
			Manifest basePatchStreamManifest = outputManager.BasePatchStreamManifest;
			Asset[] assets = basePatchStreamManifest.Assets;
			foreach (Asset baseAsset in assets)
			{
				InstanceDeclaration instanceDeclaration = FindInstance(baseAsset, outputInstances);
				if (instanceDeclaration != null)
				{
					if (outputManager.GetBinaryAsset(instanceDeclaration, isOutputAsset: false).GetLocation(AssetLocation.BasePatchStream, AssetLocationOption.None) == AssetLocation.BasePatchStream)
					{
						_Tracer.TraceInfo("Using base stream instance {0}:{1}", instanceDeclaration.Handle.TypeName, instanceDeclaration.Handle.InstanceName);
						finalList.Add(instanceDeclaration);
						outputInstances.Remove(instanceDeclaration);
					}
					else
					{
						_Tracer.TraceInfo("Using patched instance {0}:{1}", instanceDeclaration.Handle.TypeName, instanceDeclaration.Handle.InstanceName);
					}
				}
			}
		}

		private static InstanceDeclaration FindInstance(Asset baseAsset, List2<InstanceDeclaration> instances)
		{
			foreach (InstanceDeclaration instance in instances)
			{
				if (baseAsset.TypeId == instance.Handle.TypeId && baseAsset.InstanceId == instance.Handle.InstanceId)
				{
					return instance;
				}
			}
			return null;
		}

		private static int StableSort_CalcInsertPosition(List2<InstanceDeclaration> finalList, InstanceDeclaration instance, TypeDepCompare depCompare)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			uint num5 = 0u;
			uint num6 = 0u;
			bool flag = false;
			foreach (InstanceDeclaration final in finalList)
			{
				if (final.Handle.TypeId != num6)
				{
					num3 = num2;
					num6 = final.Handle.TypeId;
				}
				if (instance.AllDependentInstances != null && instance.AllDependentInstances.Contains(final.Handle))
				{
					num = num2 + 1;
					num4 = num;
					if (instance.Handle.TypeId != final.Handle.TypeId)
					{
						flag = false;
						num5 = final.Handle.TypeId;
					}
				}
				else
				{
					if (final.AllDependentInstances != null && final.AllDependentInstances.Contains(instance.Handle))
					{
						if (num3 > num4)
						{
							if (instance.Handle.TypeId != final.Handle.TypeId)
							{
								return num3;
							}
							return num;
						}
						return num;
					}
					if (instance.Handle.TypeId == final.Handle.TypeId)
					{
						if (!flag)
						{
							num = num2;
							flag = true;
						}
						if (string.Compare(instance.Handle.InstanceName, final.Handle.InstanceName) > 0)
						{
							num = num2 + 1;
						}
					}
					else if (final.Handle.TypeId == num5)
					{
						num = num2 + 1;
					}
					else
					{
						num5 = 0u;
						if (depCompare.Compare(instance, final) > 0)
						{
							num = num2 + 1;
						}
					}
				}
				num2++;
			}
			return num;
		}

		public void PrepareOutputInstances(OutputManager outputManager)
		{
			_Current.OutputInstanceSet = new SortedDictionary<InstanceHandle, InstanceDeclaration>();
			foreach (InstanceDeclaration instance in Instances)
			{
				AddOutputInstance(instance);
			}
			foreach (InstanceDeclaration value in _Current.OutputInstanceSet.Values)
			{
				BinaryAsset binaryAsset = outputManager.GetBinaryAsset(value, isOutputAsset: true);
				if (binaryAsset != null)
				{
					AssetLocation location = binaryAsset.GetLocation(AssetLocation.All, AssetLocationOption.None);
					if (location == AssetLocation.None)
					{
						location = binaryAsset.GetLocation(AssetLocation.Local | AssetLocation.Cache, AssetLocationOption.ForceUpdate);
					}
					if (location == AssetLocation.None)
					{
						throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Asset {0} not available.", value);
					}
					if ((location & AssetLocation.Output) == 0)
					{
						binaryAsset.Commit();
					}
					_Current.OutputInstances.Add(value);
					if (Settings.Current.OutputAssetReport)
					{
						AssetReport.RecordAsset(value, binaryAsset);
					}
				}
			}
			if (Settings.Current.StableSort)
			{
				StableSort(outputManager);
			}
			else
			{
				_Current.OutputInstances.Sort(new DependencyComparer());
			}
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			foreach (InstanceDeclaration outputInstance in _Current.OutputInstances)
			{
				binaryWriter.Write(outputInstance.Handle.TypeId);
				binaryWriter.Write(outputInstance.Handle.TypeHash);
				binaryWriter.Write(outputInstance.Handle.InstanceId);
				binaryWriter.Write(outputInstance.Handle.InstanceHash);
				binaryWriter.Write(outputInstance.ReferencedInstances.Count);
			}
			if (memoryStream.Length > 0)
			{
				_Current.OutputChecksum = FastHash.GetHashCode(memoryStream.GetBuffer());
			}
			else
			{
				_Current.OutputChecksum = 0u;
			}
			_Current.OutputInstanceSet = null;
		}

		public void CacheFromDocument(AssetDeclarationDocument other)
		{
			if (other._Last != null)
			{
				_Last = other._Last;
			}
			else
			{
				if (other._Current == null || _Last != null)
				{
					return;
				}
				_Last = new LastState(other._Current);
				List2<InstanceDeclaration> list = new List2<InstanceDeclaration>();
				foreach (InstanceDeclaration selfInstance in other._Current.SelfInstances)
				{
					InstanceDeclaration instanceDeclaration = new InstanceDeclaration();
					instanceDeclaration.CacheFromInstance(selfInstance);
					list.Add(instanceDeclaration);
				}
				_Last.SelfInstances = new List2<InstanceDeclaration>(list);
			}
		}
	}
}
