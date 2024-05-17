using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using BinaryAssetBuilder.Core;

namespace BinaryAssetBuilder
{
	[Serializable]
	public class BinaryAssetBuilderException : ApplicationException
	{
		private ErrorCode _ErrorCode;

		public ErrorCode ErrorCode => _ErrorCode;

		public BinaryAssetBuilderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			_ErrorCode = (ErrorCode)info.GetValue("_ErrorCode", typeof(ErrorCode));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("_ErrorCode", _ErrorCode);
			base.GetObjectData(info, context);
		}

		public BinaryAssetBuilderException(ErrorCode e, string message, params object[] args)
			: base(string.Format(message, args))
		{
			_ErrorCode = e;
		}

		public BinaryAssetBuilderException(Exception innerException, ErrorCode e, string message, params object[] args)
			: base(string.Format(message, args), innerException)
		{
			_ErrorCode = e;
		}

		public BinaryAssetBuilderException(Exception innerException, ErrorCode e)
			: base(e.ToString(), innerException)
		{
			_ErrorCode = e;
		}

		public void Trace(Tracer tracer)
		{
			if (tracer == null)
			{
				tracer = Tracer.GetTracer("Default Tracer", string.Empty);
			}
			if (base.InnerException != null)
			{
				if (base.InnerException is XmlSchemaValidationException)
				{
					XmlSchemaValidationException ex = base.InnerException as XmlSchemaValidationException;
					tracer.TraceException("{4}:\n   XML validation error encountered in {0} (line {1}, position {2}):\n      {3}", ex.SourceUri.ToString(), ex.LineNumber, ex.LinePosition, ex.Message, Message);
				}
				else if (base.InnerException is XmlException)
				{
					XmlException ex2 = base.InnerException as XmlException;
					tracer.TraceException("{4}:\n   XML formatting error encountered in {0} (line {1}, position {2}):\n      {3}", ex2.SourceUri.ToString(), ex2.LineNumber, ex2.LinePosition, ex2.Message, Message);
				}
				else if (base.InnerException is XmlSchemaException)
				{
					XmlSchemaException ex3 = base.InnerException as XmlSchemaException;
					string text = "<Name Not Available>";
					if (ex3.SourceSchemaObject != null)
					{
						XmlSchemaAttribute xmlSchemaAttribute = ex3.SourceSchemaObject as XmlSchemaAttribute;
						XmlSchemaElement xmlSchemaElement = ex3.SourceSchemaObject as XmlSchemaElement;
						if (xmlSchemaAttribute != null)
						{
							text = xmlSchemaAttribute.Name;
						}
						else if (xmlSchemaElement != null)
						{
							text = xmlSchemaElement.Name;
						}
					}
					tracer.TraceException("{5}:\n   Schema error encountered in {0} (object: {4}, line {1}, position {2}):\n      {3}", ex3.SourceUri.ToString(), ex3.LineNumber, ex3.LinePosition, ex3.Message, text, Message);
				}
				else
				{
					tracer.TraceException($"{Message}:\n   {base.InnerException.Message}");
				}
			}
			else
			{
				tracer.TraceException(Message);
			}
		}
	}
}
