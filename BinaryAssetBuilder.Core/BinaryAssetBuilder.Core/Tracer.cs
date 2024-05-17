using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BinaryAssetBuilder.Core
{
	public class Tracer
	{
		private class InternalTraceSource : TraceSource
		{
			public InternalTraceSource(string name)
				: base(name)
			{
			}

			protected override string[] GetSupportedAttributes()
			{
				return new string[1] { "mask" };
			}
		}

		private class InternalScopedTrace : IDisposable
		{
			private Tracer _Tracer;

			private string _Text;

			public InternalScopedTrace(Tracer tracer, string text)
			{
				_Text = text;
				_Tracer = tracer;
				_Tracer.Output(TraceEventType.Verbose, "Entering: {0}", _Text);
				IndentLevel++;
			}

			public void Dispose()
			{
				IndentLevel--;
				_Tracer.Output(TraceEventType.Verbose, "Leaving: {0}", _Text);
			}
		}

		public static TraceKind[] VerbosityLevels;

		private static Dictionary<string, Tracer> _Tracers;

		public static TraceWriteHandler TraceWrite;

		private static LocalDataStoreSlot _IndentLevelStore;

		private static TraceKind _TraceMask;

		private TraceSource _TraceSource;

		private string _Name;

		private string _Description;

		private bool _TraceEnabled;

		private static int IndentLevel
		{
			get
			{
				int result = 0;
				object data = Thread.GetData(_IndentLevelStore);
				if (data == null)
				{
					Thread.SetData(_IndentLevelStore, 0);
				}
				else
				{
					result = (int)data;
				}
				return result;
			}
			set
			{
				Thread.SetData(_IndentLevelStore, value);
			}
		}

		private TraceKind Options => _TraceMask;

		private TraceSource TraceSource
		{
			get
			{
				if (_TraceSource == null)
				{
					_TraceSource = new InternalTraceSource(_Name);
				}
				return _TraceSource;
			}
		}

		static Tracer()
		{
			VerbosityLevels = new TraceKind[10]
			{
				TraceKind.None,
				TraceKind.Exception | TraceKind.Assert | TraceKind.Error | TraceKind.Warning | TraceKind.Message,
				TraceKind.Exception | TraceKind.Assert | TraceKind.Error | TraceKind.Warning | TraceKind.Message,
				(TraceKind)255,
				(TraceKind)255,
				(TraceKind)255,
				(TraceKind)511,
				(TraceKind)511,
				(TraceKind)511,
				TraceKind.All
			};
			_Tracers = new Dictionary<string, Tracer>();
			_IndentLevelStore = Thread.AllocateDataSlot();
			_TraceMask = VerbosityLevels[1];
		}

		public static Tracer GetTracer(string name, string description)
		{
			Tracer value = null;
			if (!_Tracers.TryGetValue(name, out value))
			{
				value = new Tracer(name, description);
				_Tracers.Add(name, value);
			}
			return value;
		}

		public static void SetTraceLevel(int level)
		{
			_TraceMask = VerbosityLevels[level];
		}

		private static void OnWrite(string source, TraceEventType type, string message)
		{
			if (TraceWrite != null)
			{
				TraceWrite(source, type, message);
			}
		}

		private void Output(TraceEventType type, string format, params object[] args)
		{
			if (_TraceEnabled)
			{
				StringBuilder stringBuilder = new StringBuilder(new string(' ', IndentLevel * Trace.IndentSize));
				stringBuilder.AppendFormat(format, args);
			}
			else
			{
				OnWrite(TraceSource.Name, type, string.Format(format, args));
			}
		}

		private Tracer(string name, string description)
		{
			_Name = name;
			_Description = description;
			_TraceEnabled = false;
			string environmentVariable = Environment.GetEnvironmentVariable("BabEnableTrace");
			_TraceEnabled = string.Equals(environmentVariable, "True", StringComparison.InvariantCultureIgnoreCase);
		}

		private static string GetCallingMethodInfo(string additionalInfo)
		{
			StackFrame stackFrame = new StackFrame(2);
			MethodBase method = stackFrame.GetMethod();
			return $"{method.DeclaringType.Name}.{method.Name}({additionalInfo})";
		}

		public IDisposable TraceMethod()
		{
			if ((_TraceMask & TraceKind.Method) == 0)
			{
				return null;
			}
			return new InternalScopedTrace(this, GetCallingMethodInfo(string.Empty));
		}

		public IDisposable TraceMethod(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Method) == 0)
			{
				return null;
			}
			return new InternalScopedTrace(this, GetCallingMethodInfo(string.Format(format, args)));
		}

		public void Message(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Message) != 0)
			{
				Output(TraceEventType.Information, format, args);
			}
		}

		public void TraceData(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Data) != 0)
			{
				Output(TraceEventType.Verbose, format, args);
			}
		}

		public void TraceInfo(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Info) != 0)
			{
				Output(TraceEventType.Information, format, args);
			}
		}

		public void TraceNote(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Note) != 0)
			{
				Output(TraceEventType.Verbose, format, args);
			}
		}

		public void TraceError(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Error) != 0)
			{
				Output(TraceEventType.Error, format, args);
			}
		}

		public void TraceWarning(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Warning) != 0)
			{
				Output(TraceEventType.Warning, format, args);
			}
		}

		public void TraceException(string format, params object[] args)
		{
			if ((_TraceMask & TraceKind.Exception) != 0)
			{
				Output(TraceEventType.Critical, format, args);
			}
		}
	}
}
