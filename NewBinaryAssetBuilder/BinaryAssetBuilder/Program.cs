using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BinaryAssetBuilder.Core;
using BinaryAssetBuilder.Remote;

namespace BinaryAssetBuilder
{
	internal static class Program
	{
		private class ServerCommandHandler : MarshalByRefObject, IServerCommand
		{
			private TextWriter _originalConsole;

			public virtual ServerState State => ResidentInstance.State;

			public virtual void StartBuild(string[] args)
			{
				_theResidentServer.StartBuild(args);
			}

			public virtual void EndBuild()
			{
				if (_originalConsole != null)
				{
					Console.SetOut(_originalConsole);
					_originalConsole = null;
				}
			}

			public virtual void RedirectConsoleOutput()
			{
				IClientCommand clientCommand = (IClientCommand)Activator.GetObject(typeof(IClientCommand), "ipc://BinaryAssetBuilderClientChannel/ClientCommand");
				if (_originalConsole == null)
				{
					_originalConsole = Console.Out;
					TextWriter outputHandle = clientCommand.OutputHandle;
					ILease lease = (ILease)RemotingServices.GetLifetimeService(outputHandle);
					lease.Renew(TimeSpan.FromDays(31.0));
					Console.SetOut(outputHandle);
				}
				else
				{
					clientCommand.OutputHandle.WriteLine("BabServer: BinaryAssetBuilder Server instance appears to be bungled, please manually exit it from the system tray and rebuild.");
				}
			}

			public override object InitializeLifetimeService()
			{
				return null;
			}
		}

		public class ResidentInstance : IDisposable
		{
			private Mutex _processSync;

			private bool _owned;

			private IpcChannel _ipcChannel;

			private BinaryAssetBuilder _bab;

			private Thread _babThread;

			private Mutex _ResidentBabReadyMutex;

			private static ServerState _state;

			private static SessionCache _theSessionCache;

			private static PathMonitor _thePathMonitor;

			public static SessionCache TheSessionCache => _theSessionCache;

			public static PathMonitor ThePathMonitor => _thePathMonitor;

			public bool IsFirstInstance => _owned;

			public static ServerState State => _state;

			public ResidentInstance()
			{
				_processSync = new Mutex(initiallyOwned: true, Assembly.GetExecutingAssembly().GetName().Name, out _owned);
			}

			~ResidentInstance()
			{
				try
				{
					Release();
				}
				catch
				{
				}
			}

			private void Release()
			{
				if (_owned)
				{
					_processSync.ReleaseMutex();
					_owned = false;
					ChannelServices.UnregisterChannel(_ipcChannel);
					_ipcChannel = null;
				}
			}

			public void Dispose()
			{
				Release();
				GC.SuppressFinalize(this);
			}

			public void CreateRemotingObjects()
			{
				_ipcChannel = new IpcChannel("BinaryAssetBuilderChannel");
				ChannelServices.RegisterChannel(_ipcChannel, ensureSecurity: false);
				RemotingConfiguration.RegisterWellKnownServiceType(typeof(ServerCommandHandler), "ServerCommand", WellKnownObjectMode.Singleton);
			}

			public void Initialize()
			{
				_state = ServerState.Loading;
				_thePathMonitor = new PathMonitor(Settings.Current.ProcessedMonitorPaths);
				_theSessionCache = new SessionCache();
				_bab = new BinaryAssetBuilder();
				_bab.Cache = TheSessionCache;
				_bab.Monitor = ThePathMonitor;
				if (Settings.Current.Resident)
				{
					CreateRemotingObjects();
					_ResidentBabReadyMutex = new Mutex(initiallyOwned: true, "BinaryAssetBuilderResidentReady");
				}
				_state = ServerState.Ready;
			}

			public bool SetupSettings(string[] args)
			{
				if (args.Length > 0 && args[0] == "/?")
				{
					CommandLineOptionProcessor commandLineOptionProcessor = new CommandLineOptionProcessor(Settings.Current);
					Console.WriteLine(BinaryAssetBuilder.GetApplicationVersionString());
					Console.WriteLine("Usage: BinaryAssetBuilder {0}\n", commandLineOptionProcessor.GetCommandLineHintText());
					Console.WriteLine(commandLineOptionProcessor.GetCommandLineHelpText(Console.LargestWindowWidth));
					return false;
				}
				Settings settings = ConfigurationManager.GetSection("assetbuilder") as Settings;
				Settings.Current = (Settings)settings.Clone();
				if (Settings.Current == null)
				{
					throw new ApplicationException("BinaryAssetBuilder configuration not found.");
				}
				if (args.Length != 0)
				{
					CommandLineOptionProcessor commandLineOptionProcessor2 = new CommandLineOptionProcessor(Settings.Current);
					if (!commandLineOptionProcessor2.ProcessOptions(args, out var messages))
					{
						Console.WriteLine(string.Join("\n", messages));
						return false;
					}
				}
				SettingsLoader.PostProcessSettings(Path.GetDirectoryName(Application.ExecutablePath));
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string arg in args)
				{
					stringBuilder.AppendFormat("{0} ", arg);
				}
				Console.WriteLine("Command Line: {0}", stringBuilder.ToString());
				return true;
			}

			public void StartBuild(string[] args)
			{
				if (SetupSettings(args))
				{
					_babThread = new Thread(_bab.Run);
					_babThread.Name = "BabBuildThread";
					_babThread.Start();
				}
			}
		}

		private static ResidentInstance _theResidentServer;

		public static SystemTrayForm _systemTrayForm;

		[STAThread]
		private static void Main(string[] args)
		{
			Logger.init();
			Logger.info($"args: {string.Join(",", args)}");
			using ResidentInstance residentInstance = new ResidentInstance();
			if (!residentInstance.IsFirstInstance)
			{
				return;
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(defaultValue: false);
			_theResidentServer = residentInstance;
			if (residentInstance.SetupSettings(args))
			{
				residentInstance.Initialize();
				if (Settings.Current.Resident)
				{
					_systemTrayForm = new SystemTrayForm();
					_systemTrayForm.Hide();
					Application.Run(_systemTrayForm);
				}
				else
				{
					residentInstance.StartBuild(args);
				}
			}
		}
	}
}
