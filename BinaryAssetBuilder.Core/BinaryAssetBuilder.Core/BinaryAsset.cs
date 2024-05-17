using System;
using System.IO;
using System.Threading;
using BinaryAssetBuilder.Utility;

namespace BinaryAssetBuilder.Core
{
	public class BinaryAsset
	{
		private enum Availability
		{
			Invalid,
			Missing,
			Present
		}

		private static Tracer _Tracer = Tracer.GetTracer("BinaryAsset", "Provides output management for assets");

		private InstanceDeclaration _Instance;

		private string _CacheDirectory;

		private string _AssetOutputDirectory;

		private string _CustomDataOutputDirectory;

		private string _AssetFileName;

		private string _CustomDataFileName;

		private AssetBuffer _Buffer;

		private bool _IsOutputAsset;

		private Availability _MemoryAvailability;

		private Availability _CacheAvailability;

		private Availability _OutputAvailability;

		private AssetLocationInfo _LastLocationInfo;

		private Availability _LocalAvailability;

		private Availability _BasePatchStreamAvailability;

		private AssetHeader _AssetHeader;

		private OutputManager _Parent;

		private static byte[] _CopyBuffer = new byte[1048576];

		public AssetBuffer Buffer
		{
			get
			{
				return _Buffer;
			}
			set
			{
				_Buffer = value;
				UpdateMemoryAvailability(forceUpdate: true);
			}
		}

		public bool IsOutputAsset
		{
			get
			{
				return _IsOutputAsset;
			}
			set
			{
				_IsOutputAsset = value;
			}
		}

		public string FileBase => _Instance.Handle.FileBase;

		public InstanceDeclaration Instance
		{
			get
			{
				return _Instance;
			}
			set
			{
				_Instance = value;
			}
		}

		public int InstanceFileSize
		{
			get
			{
				if (_AssetHeader == null)
				{
					UpdateAssetHeader(forceUpdate: false);
				}
				return _AssetHeader.InstanceDataSize;
			}
		}

		public int RelocationFileSize
		{
			get
			{
				if (_AssetHeader == null)
				{
					UpdateAssetHeader(forceUpdate: false);
				}
				return _AssetHeader.RelocationDataSize;
			}
		}

		public int ImportsFileSize
		{
			get
			{
				if (_AssetHeader == null)
				{
					UpdateAssetHeader(forceUpdate: false);
				}
				return _AssetHeader.ImportsDataSize;
			}
		}

		public string AssetFileName => _AssetFileName;

		public string AssetOutputDirectory => _AssetOutputDirectory;

		public string CustomDataOutputDirectory => _CustomDataOutputDirectory;

		private void UpdateMemoryAvailability(bool forceUpdate)
		{
			if (_MemoryAvailability == Availability.Invalid || forceUpdate)
			{
				if (_Buffer != null && _Buffer.InstanceData != null && _Buffer.RelocationData != null && _Buffer.ImportsData != null)
				{
					_MemoryAvailability = Availability.Present;
				}
				else
				{
					_MemoryAvailability = Availability.Missing;
				}
			}
		}

		private void UpdateCacheAvailability(bool forceUpdate)
		{
			if (_CacheAvailability != 0 && !forceUpdate)
			{
				return;
			}
			if (string.IsNullOrEmpty(_CacheDirectory))
			{
				_CacheAvailability = Availability.Missing;
				return;
			}
			string path = Path.Combine(_CacheDirectory, _AssetFileName);
			string path2 = Path.Combine(_CacheDirectory, _CustomDataFileName);
			if (File.Exists(path) && (!_Instance.HasCustomData || File.Exists(path2)))
			{
				_CacheAvailability = Availability.Present;
			}
			else
			{
				_CacheAvailability = Availability.Missing;
			}
		}

		private void UpdateOutputAvailability(bool forceUpdate)
		{
			if (_OutputAvailability == Availability.Invalid || forceUpdate)
			{
				string path = Path.Combine(_AssetOutputDirectory, _AssetFileName);
				string path2 = Path.Combine(_CustomDataOutputDirectory, _CustomDataFileName);
				if (File.Exists(path) && (File.Exists(path2) || !_Instance.HasCustomData))
				{
					_OutputAvailability = Availability.Present;
				}
				else
				{
					_OutputAvailability = Availability.Missing;
				}
			}
		}

		private void UpdateLocalAvailability(bool forceUpdate)
		{
			if (_LocalAvailability == Availability.Invalid || forceUpdate)
			{
				_LastLocationInfo = _Parent.DocumentProcessor.GetLastWrittenAsset(FileBase);
				if (_LastLocationInfo != null)
				{
					_LocalAvailability = Availability.Present;
				}
				else
				{
					_LocalAvailability = Availability.Missing;
				}
			}
		}

		private void UpdateBasePatchStreamAvailability(bool forceUpdate)
		{
			if (_BasePatchStreamAvailability == Availability.Invalid || forceUpdate)
			{
				if (_Parent.BasePatchStreamAssets != null && _Parent.BasePatchStreamAssets.TryGetValue(FileBase, out _AssetHeader))
				{
					_BasePatchStreamAvailability = Availability.Present;
				}
				else
				{
					_BasePatchStreamAvailability = Availability.Missing;
				}
			}
		}

		public AssetLocation GetLocation(AssetLocation locationFilter, AssetLocationOption options)
		{
			AssetLocation assetLocation = AssetLocation.None;
			bool forceUpdate = (options & AssetLocationOption.ForceUpdate) != 0;
			bool flag = (options & AssetLocationOption.ReturnAll) == 0;
			if ((locationFilter & AssetLocation.BasePatchStream) != 0)
			{
				UpdateBasePatchStreamAvailability(forceUpdate);
				if (_BasePatchStreamAvailability == Availability.Present)
				{
					if (flag)
					{
						return AssetLocation.BasePatchStream;
					}
					assetLocation |= AssetLocation.BasePatchStream;
				}
			}
			if ((locationFilter & AssetLocation.Output) != 0)
			{
				UpdateOutputAvailability(forceUpdate);
				if (_OutputAvailability == Availability.Present)
				{
					if (flag)
					{
						return AssetLocation.Output;
					}
					assetLocation |= AssetLocation.Output;
				}
			}
			if ((locationFilter & AssetLocation.Memory) != 0)
			{
				UpdateMemoryAvailability(forceUpdate);
				if (_MemoryAvailability == Availability.Present)
				{
					if (flag)
					{
						return AssetLocation.Memory;
					}
					assetLocation |= AssetLocation.Memory;
				}
			}
			if ((locationFilter & AssetLocation.Local) != 0)
			{
				UpdateLocalAvailability(forceUpdate);
				if (_LocalAvailability == Availability.Present)
				{
					if (flag)
					{
						return AssetLocation.Local;
					}
					assetLocation |= AssetLocation.Local;
				}
			}
			if ((locationFilter & AssetLocation.Cache) != 0)
			{
				UpdateCacheAvailability(forceUpdate);
				if (_CacheAvailability == Availability.Present)
				{
					if (flag)
					{
						return AssetLocation.Cache;
					}
					assetLocation |= AssetLocation.Cache;
				}
			}
			return assetLocation;
		}

		private void UpdateAssetHeader(bool forceUpdate)
		{
			if (_AssetHeader != null && !forceUpdate)
			{
				return;
			}
			AssetLocation location = GetLocation(AssetLocation.Memory | AssetLocation.Output, AssetLocationOption.ReturnAll);
			if (location == AssetLocation.None)
			{
				_AssetHeader = null;
			}
			else if ((location & AssetLocation.Memory) != 0)
			{
				_AssetHeader = new AssetHeader();
				_AssetHeader.ImportsDataSize = _Buffer.ImportsData.Length;
				_AssetHeader.InstanceDataSize = _Buffer.InstanceData.Length;
				_AssetHeader.RelocationDataSize = _Buffer.RelocationData.Length;
				_AssetHeader.TypeId = Instance.Handle.TypeId;
				_AssetHeader.TypeHash = Instance.Handle.TypeHash;
				_AssetHeader.InstanceHash = Instance.Handle.InstanceHash;
				_AssetHeader.InstanceId = Instance.Handle.InstanceId;
			}
			else if ((location & AssetLocation.Output) != 0)
			{
				long fileLength = 0L;
				string text = Path.Combine(_AssetOutputDirectory, _AssetFileName);
				using (FileStream fileStream = File.OpenRead(text))
				{
					_AssetHeader = new AssetHeader();
					_AssetHeader.LoadFromStream(fileStream, Settings.Current.BigEndian);
					fileLength = fileStream.Length;
				}
				if (!_AssetHeader.IsValidFileLength(fileLength))
				{
					_Tracer.TraceError("Invalid asset file detected. Deleting {0}.", text);
					File.Delete(text);
					location = GetLocation(AssetLocation.Cache, AssetLocationOption.None);
					if ((location & AssetLocation.Cache) == 0)
					{
						throw new BinaryAssetBuilderException(ErrorCode.UnexpectedSize, "Can't recover from last error. Please restart to rebuild asset.");
					}
					_Tracer.TraceInfo("Checking build cache copy of asset.");
					string text2 = Path.Combine(_CacheDirectory, _AssetFileName);
					using (FileStream fileStream2 = File.OpenRead(text2))
					{
						_AssetHeader = new AssetHeader();
						_AssetHeader.LoadFromStream(fileStream2, Settings.Current.BigEndian);
						fileLength = fileStream2.Length;
					}
					if (!_AssetHeader.IsValidFileLength(fileLength))
					{
						_Tracer.TraceError("Invalid cached asset file detected. Deleting {0}.", text2);
						File.Delete(text2);
						throw new BinaryAssetBuilderException(ErrorCode.UnexpectedSize, "Can't recover from last error. Please restart to rebuild asset.");
					}
					CommitFromCache();
				}
			}
			else
			{
				_AssetHeader = null;
			}
		}

		private void UpdateAssetHeaderFromOldInstance(OutputAsset oldInstance)
		{
			if (_AssetHeader == null && GetLocation(AssetLocation.All, AssetLocationOption.None) != 0)
			{
				if (oldInstance.InstanceFileSize == 0 && oldInstance.RelocationFileSize == 0 && oldInstance.ImportsFileSize == 0)
				{
					_Tracer.TraceWarning("Suspicious file sizes for {0}. Reloading info from disk.", _Instance);
					UpdateAssetHeader(forceUpdate: true);
					return;
				}
				_AssetHeader = new AssetHeader();
				_AssetHeader.ImportsDataSize = oldInstance.ImportsFileSize;
				_AssetHeader.InstanceDataSize = oldInstance.InstanceFileSize;
				_AssetHeader.RelocationDataSize = oldInstance.RelocationFileSize;
				_AssetHeader.TypeHash = oldInstance.Handle.TypeHash;
				_AssetHeader.TypeId = oldInstance.Handle.TypeId;
				_AssetHeader.InstanceId = oldInstance.Handle.InstanceId;
				_AssetHeader.InstanceHash = oldInstance.Handle.InstanceHash;
			}
		}

		public BinaryAsset(OutputManager parent, OutputAsset oldInstance, InstanceDeclaration instance)
		{
			_Parent = parent;
			if (Settings.Current.OldOutputFormat)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Old Output Format no longer supported");
			}
			_Instance = instance;
			_CustomDataOutputDirectory = Path.Combine(_Parent.OutputDirectory, "cdata");
			_AssetOutputDirectory = Path.Combine(_Parent.IntermediateOutputDirectory, "assets");
			_CustomDataFileName = FileBase + ".cdata";
			_AssetFileName = FileBase + ".asset";
			if (!string.IsNullOrEmpty(_Parent.TargetPlatformCacheRoot))
			{
				ExtendedTypeInformation extendedTypeInformation = _Parent.DocumentProcessor.Plugins.GetExtendedTypeInformation(instance.Handle.TypeId);
				if (extendedTypeInformation.UseBuildCache)
				{
					_CacheDirectory = Path.Combine(_Parent.TargetPlatformCacheRoot, $"{instance.Handle.TypeName}\\{instance.Handle.TypeHash:x8}\\{(byte)(instance.Handle.InstanceHash >> 24):x2}");
				}
			}
			AssetLocation location = GetLocation(AssetLocation.All, AssetLocationOption.None);
			if (oldInstance != null && location != 0)
			{
				UpdateAssetHeaderFromOldInstance(oldInstance);
			}
		}

		public AssetLocation Commit()
		{
			AssetLocation location = GetLocation(AssetLocation.All, AssetLocationOption.None);
			switch (location)
			{
			case AssetLocation.Output:
				UpdateAssetHeader(forceUpdate: false);
				break;
			case AssetLocation.Memory:
				CommitFromMemory();
				break;
			case AssetLocation.Local:
				CommitFromLocal();
				break;
			case AssetLocation.Cache:
				CommitFromCache();
				break;
			default:
				throw new BinaryAssetBuilderException(ErrorCode.DependencyCacheFailure, "Attempted to commit non-existing asset {0}", _Instance);
			case AssetLocation.BasePatchStream:
				break;
			}
			if (location != AssetLocation.BasePatchStream)
			{
				UpdateOutputAvailability(forceUpdate: true);
			}
			if (_OutputAvailability == Availability.Missing)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Failure to commit asset {0}", _Instance);
			}
			if (location == AssetLocation.Memory)
			{
				CopyToCache();
			}
			else if (location == AssetLocation.Cache || Settings.Current.AlwaysTouchCache)
			{
				TouchCache();
			}
			return location;
		}

		private bool CopyAsset(string sourceAssetPath, string sourceCustomDataPath)
		{
			string text = Path.Combine(_AssetOutputDirectory, _AssetFileName);
			string text2 = text + ".tmp";
			string text3 = Path.Combine(_CustomDataOutputDirectory, _CustomDataFileName);
			_AssetHeader = null;
			try
			{
				if (!Directory.Exists(_AssetOutputDirectory))
				{
					Directory.CreateDirectory(_AssetOutputDirectory);
				}
				lock (_CopyBuffer)
				{
					using FileStream fileStream = File.OpenRead(sourceAssetPath);
					using (FileStream fileStream2 = File.Open(text2, FileMode.Create, FileAccess.Write))
					{
						long num = fileStream.Length;
						while (num > 0)
						{
							int num2 = fileStream.Read(_CopyBuffer, 0, _CopyBuffer.Length);
							if (num2 < 16)
							{
								throw new Exception();
							}
							if (_AssetHeader == null)
							{
								_AssetHeader = new AssetHeader();
								_AssetHeader.LoadFromBuffer(_CopyBuffer, Settings.Current.BigEndian);
								if (_AssetHeader.InstanceId != Instance.Handle.InstanceId || _AssetHeader.TypeId != Instance.Handle.TypeId || _AssetHeader.InstanceHash != Instance.Handle.InstanceHash || _AssetHeader.TypeHash != Instance.Handle.TypeHash)
								{
									throw new Exception();
								}
							}
							fileStream2.Write(_CopyBuffer, 0, num2);
							num -= num2;
						}
						fileStream2.Flush();
					}
					if (File.Exists(text))
					{
						File.Delete(text);
					}
					File.Move(text2, text);
				}
				if (Instance.HasCustomData)
				{
					if (!Directory.Exists(_CustomDataOutputDirectory))
					{
						Directory.CreateDirectory(_CustomDataOutputDirectory);
					}
					File.Copy(sourceCustomDataPath, text3, overwrite: true);
				}
			}
			catch (Exception)
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				if (File.Exists(text3))
				{
					File.Delete(text3);
				}
				return false;
			}
			return true;
		}

		private void CommitFromLocal()
		{
			UpdateLocalAvailability(forceUpdate: false);
			if (_LocalAvailability == Availability.Missing)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Failure to commit asset {0} from last output", _Instance);
			}
			string sourceAssetPath = Path.Combine(_LastLocationInfo.AssetOutputDirectory, _AssetFileName);
			string sourceCustomDataPath = Path.Combine(_LastLocationInfo.CustomDataOutputDirectory, _CustomDataFileName);
			if (CopyAsset(sourceAssetPath, sourceCustomDataPath))
			{
				_Tracer.Message("{0} copied from previous output: {1}", DocumentProcessor.CurrentDocument, Instance);
				return;
			}
			throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Failure to commit asset {0} from previous output", _Instance);
		}

		private void CommitFromCache()
		{
			UpdateCacheAvailability(forceUpdate: false);
			if (_CacheAvailability == Availability.Missing)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Asset not available. Failure to commit asset {0} from network", _Instance);
			}
			string text = Path.Combine(_CacheDirectory, _AssetFileName);
			string text2 = Path.Combine(_CacheDirectory, _CustomDataFileName);
			bool flag = false;
			int num = 0;
			do
			{
				try
				{
					if (CopyAsset(text, text2))
					{
						_Tracer.Message("{0} copied from cache: {1}", DocumentProcessor.CurrentDocument, Instance);
						flag = true;
						break;
					}
				}
				catch (Exception ex)
				{
					_Tracer.Message("{0} in use ({1}), re-attempting grab: {2}", DocumentProcessor.CurrentDocument, ex.Message, Instance);
					flag = false;
				}
				Thread.Sleep(500);
				num++;
			}
			while (num < 20);
			if (!flag)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Asset copy failed. Failure to commit asset {0} from network ({1}, {2})", _Instance, text, text2);
			}
		}

		private void CommitFromMemory()
		{
			UpdateMemoryAvailability(forceUpdate: false);
			if (_MemoryAvailability == Availability.Missing)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Failure to commit asset {0} from memory", _Instance);
			}
			if (!Directory.Exists(_AssetOutputDirectory))
			{
				Directory.CreateDirectory(_AssetOutputDirectory);
			}
			try
			{
				string text = Path.Combine(_AssetOutputDirectory, _AssetFileName);
				string text2 = text + ".tmp";
				using (FileStream fileStream = File.Open(text2, FileMode.Create, FileAccess.Write))
				{
					_AssetHeader = new AssetHeader();
					_AssetHeader.ImportsDataSize = _Buffer.ImportsData.Length;
					_AssetHeader.InstanceDataSize = _Buffer.InstanceData.Length;
					_AssetHeader.RelocationDataSize = _Buffer.RelocationData.Length;
					_AssetHeader.TypeId = Instance.Handle.TypeId;
					_AssetHeader.TypeHash = Instance.Handle.TypeHash;
					_AssetHeader.InstanceHash = Instance.Handle.InstanceHash;
					_AssetHeader.InstanceId = Instance.Handle.InstanceId;
					_AssetHeader.SaveToStream(fileStream, Settings.Current.BigEndian);
					fileStream.Write(_Buffer.InstanceData, 0, _Buffer.InstanceData.Length);
					fileStream.Write(_Buffer.RelocationData, 0, _Buffer.RelocationData.Length);
					fileStream.Write(_Buffer.ImportsData, 0, _Buffer.ImportsData.Length);
					fileStream.Flush();
				}
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Move(text2, text);
				_Buffer = null;
				_MemoryAvailability = Availability.Missing;
			}
			catch (Exception innerException)
			{
				throw new BinaryAssetBuilderException(innerException, ErrorCode.InternalError, "Failure to commit asset {0} from memory", _Instance);
			}
		}

		private void ValidateAssetCopies()
		{
		}

		private void TouchCache()
		{
			if (string.IsNullOrEmpty(_CacheDirectory) && !Directory.Exists(_CacheDirectory))
			{
				return;
			}
			try
			{
				FileInfo fileInfo = new FileInfo(Path.Combine(_CacheDirectory, _AssetFileName));
				if (fileInfo.Exists)
				{
					FileInfo fileInfo2 = fileInfo;
					FileInfo fileInfo3 = fileInfo;
					DateTime dateTime = (fileInfo.LastAccessTime = DateTime.Now);
					DateTime creationTime = (fileInfo3.LastWriteTime = dateTime);
					fileInfo2.CreationTime = creationTime;
				}
				if (Instance.HasCustomData)
				{
					fileInfo = new FileInfo(Path.Combine(_CacheDirectory, _CustomDataFileName));
					if (fileInfo.Exists)
					{
						FileInfo fileInfo4 = fileInfo;
						FileInfo fileInfo5 = fileInfo;
						DateTime dateTime3 = (fileInfo.LastAccessTime = DateTime.Now);
						DateTime creationTime2 = (fileInfo5.LastWriteTime = dateTime3);
						fileInfo4.CreationTime = creationTime2;
					}
				}
			}
			catch (Exception)
			{
			}
		}

		private void CopyToCache()
		{
			if (string.IsNullOrEmpty(_CacheDirectory))
			{
				return;
			}
			if (GetLocation(AssetLocation.Output, AssetLocationOption.None) == AssetLocation.None)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Failure to copy asset {0} to network", _Instance);
			}
			_Tracer.Message("{0} Submitting to cache {1}", DocumentProcessor.CurrentDocument, Instance);
			try
			{
				if (!Directory.Exists(_CacheDirectory))
				{
					Directory.CreateDirectory(_CacheDirectory);
				}
				string text = Path.Combine(_CacheDirectory, _AssetFileName);
				FileInfo fileInfo = new FileInfo(text);
				if (!fileInfo.Exists)
				{
					File.Copy(Path.Combine(_AssetOutputDirectory, _AssetFileName), text);
				}
				else
				{
					FileInfo fileInfo2 = fileInfo;
					FileInfo fileInfo3 = fileInfo;
					DateTime dateTime = (fileInfo.LastAccessTime = DateTime.Now);
					DateTime creationTime = (fileInfo3.LastWriteTime = dateTime);
					fileInfo2.CreationTime = creationTime;
				}
				if (Instance.HasCustomData)
				{
					text = Path.Combine(_CacheDirectory, _CustomDataFileName);
					fileInfo = new FileInfo(text);
					if (!fileInfo.Exists)
					{
						File.Copy(Path.Combine(_CustomDataOutputDirectory, _CustomDataFileName), text);
						return;
					}
					FileInfo fileInfo4 = fileInfo;
					FileInfo fileInfo5 = fileInfo;
					DateTime dateTime3 = (fileInfo.LastAccessTime = DateTime.Now);
					DateTime creationTime2 = (fileInfo5.LastWriteTime = dateTime3);
					fileInfo4.CreationTime = creationTime2;
				}
			}
			catch (Exception)
			{
				_Tracer.Message("Failed to copy asset {0} to network", _Instance);
			}
		}
	}
}
