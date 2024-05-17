using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace BinaryAssetBuilder
{
	public class CommandLineOptionProcessor
	{
		private class OrderedOptionInfo : IComparable<OrderedOptionInfo>
		{
			public PropertyDescriptor Descriptor;

			public OrderedCommandLineOptionAttribute OptionAttribute;

			public int CompareTo(OrderedOptionInfo other)
			{
				if (OptionAttribute.Ordinal >= other.OptionAttribute.Ordinal)
				{
					if (OptionAttribute.Ordinal <= other.OptionAttribute.Ordinal)
					{
						return 0;
					}
					return 1;
				}
				return -1;
			}
		}

		private class OptionalOptionInfo : IComparable<OptionalOptionInfo>
		{
			public PropertyDescriptor Descriptor;

			public OptionalCommandLineOptionAttribute OptionAttribute;

			public int CompareTo(OptionalOptionInfo other)
			{
				return string.Compare(Descriptor.DisplayName, other.Descriptor.DisplayName);
			}
		}

		private List<OrderedOptionInfo> _OrderedOptions = new List<OrderedOptionInfo>();

		private List<OptionalOptionInfo> _OptionalOptions = new List<OptionalOptionInfo>();

		private Dictionary<string, OptionalOptionInfo> _OptionalOptionLookup = new Dictionary<string, OptionalOptionInfo>();

		private int _DisplayNameMaxLength;

		private object _SettingsObject;

		public object SettingsObject
		{
			get
			{
				return _SettingsObject;
			}
			set
			{
				SetSettingsObject(value);
			}
		}

		private void SetSettingsObject(object settingsObject)
		{
			_SettingsObject = settingsObject;
			_OrderedOptions.Clear();
			_OptionalOptions.Clear();
			_OptionalOptionLookup.Clear();
			_DisplayNameMaxLength = 0;
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(_SettingsObject);
			foreach (PropertyDescriptor item in properties)
			{
				OrderedCommandLineOptionAttribute orderedCommandLineOptionAttribute = item.Attributes[typeof(OrderedCommandLineOptionAttribute)] as OrderedCommandLineOptionAttribute;
				OptionalCommandLineOptionAttribute optionalCommandLineOptionAttribute = item.Attributes[typeof(OptionalCommandLineOptionAttribute)] as OptionalCommandLineOptionAttribute;
				if (orderedCommandLineOptionAttribute != null && optionalCommandLineOptionAttribute != null)
				{
					throw new InvalidOperationException($"Command line option property {item.Name} cannot be ordered and optional at the same time");
				}
				if (orderedCommandLineOptionAttribute != null)
				{
					ValidateOption(orderedCommandLineOptionAttribute, item);
					OrderedOptionInfo orderedOptionInfo = new OrderedOptionInfo();
					orderedOptionInfo.OptionAttribute = orderedCommandLineOptionAttribute;
					orderedOptionInfo.Descriptor = item;
					_DisplayNameMaxLength = Math.Max(_DisplayNameMaxLength, item.DisplayName.Length);
					_OrderedOptions.Add(orderedOptionInfo);
				}
				else
				{
					if (optionalCommandLineOptionAttribute == null)
					{
						continue;
					}
					ValidateOption(optionalCommandLineOptionAttribute, item);
					OptionalOptionInfo optionalOptionInfo = new OptionalOptionInfo();
					optionalOptionInfo.OptionAttribute = optionalCommandLineOptionAttribute;
					optionalOptionInfo.Descriptor = item;
					_OptionalOptions.Add(optionalOptionInfo);
					_OptionalOptionLookup.Add(item.DisplayName.ToLower(), optionalOptionInfo);
					_DisplayNameMaxLength = Math.Max(_DisplayNameMaxLength, item.DisplayName.Length + 1);
					if (!string.IsNullOrEmpty(optionalCommandLineOptionAttribute.Alias))
					{
						string[] array = optionalCommandLineOptionAttribute.Alias.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						string[] array2 = array;
						foreach (string text in array2)
						{
							_OptionalOptionLookup.Add(text.Trim().ToLower(), optionalOptionInfo);
						}
					}
				}
			}
			_OptionalOptions.Sort();
			_OrderedOptions.Sort();
		}

		private static void ValidateOption(CommandLineOptionAttribute attribute, PropertyDescriptor descriptor)
		{
			if (descriptor.IsReadOnly)
			{
				throw new InvalidOperationException($"Property '{descriptor.Name}' is read only.");
			}
			if ((attribute.MinValue != null || attribute.MaxValue != null) && descriptor.PropertyType.GetInterface("IComparable") == null)
			{
				throw new InvalidCastException(string.Format("Min and Max are specified but type of option property {1} is not comparable", attribute.MinValue, descriptor.Name));
			}
			if (attribute.MinValue != null && !descriptor.PropertyType.IsAssignableFrom(attribute.MinValue.GetType()))
			{
				throw new InvalidCastException($"Min value {attribute.MinValue} is not assignable to option property {descriptor.Name}");
			}
			if (attribute.MaxValue != null && !descriptor.PropertyType.IsAssignableFrom(attribute.MaxValue.GetType()))
			{
				throw new InvalidCastException($"Max value {attribute.MaxValue} is not assignable to option property {descriptor.Name}");
			}
			if (attribute.ValidValueSet == null)
			{
				return;
			}
			object[] validValueSet = attribute.ValidValueSet;
			foreach (object obj in validValueSet)
			{
				if (!descriptor.PropertyType.IsAssignableFrom(obj.GetType()))
				{
					throw new InvalidCastException($"Specified valid value {obj} is not assignable to option property {descriptor.Name}");
				}
			}
		}

		public CommandLineOptionProcessor(object settingsObject)
		{
			SettingsObject = settingsObject;
		}

		public string GetCommandLineHintText()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (OrderedOptionInfo orderedOption in _OrderedOptions)
			{
				stringBuilder.AppendFormat("{0} ", orderedOption.Descriptor.DisplayName);
			}
			foreach (OptionalOptionInfo optionalOption in _OptionalOptions)
			{
				stringBuilder.AppendFormat("[/{0}", optionalOption.Descriptor.DisplayName);
				if (!string.IsNullOrEmpty(optionalOption.OptionAttribute.Alias))
				{
					stringBuilder.Append("|");
					string[] value = optionalOption.OptionAttribute.Alias.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					stringBuilder.Append(string.Join("|", value));
				}
				optionalOption.Descriptor.GetValue(_SettingsObject);
				stringBuilder.Append(":");
				if (optionalOption.Descriptor.PropertyType.IsEnum)
				{
					stringBuilder.Append(string.Join("|", Enum.GetNames(optionalOption.Descriptor.PropertyType)));
				}
				else if (optionalOption.OptionAttribute.ValidValueSet != null)
				{
					bool flag = true;
					object[] validValueSet = optionalOption.OptionAttribute.ValidValueSet;
					foreach (object value2 in validValueSet)
					{
						if (!flag)
						{
							stringBuilder.Append("|");
						}
						flag = false;
						stringBuilder.Append(value2);
					}
				}
				else if (optionalOption.OptionAttribute.MinValue != null && optionalOption.OptionAttribute.MaxValue != null)
				{
					stringBuilder.AppendFormat("{0}-{1}", optionalOption.OptionAttribute.MinValue, optionalOption.OptionAttribute.MaxValue);
				}
				else if (optionalOption.Descriptor.PropertyType == typeof(bool))
				{
					stringBuilder.Append("true|false");
				}
				else
				{
					stringBuilder.Append(optionalOption.Descriptor.PropertyType.Name);
				}
				stringBuilder.Append("] ");
			}
			return stringBuilder.ToString().Trim();
		}

		public string GetCommandLineHelpText(int width)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (OrderedOptionInfo orderedOption in _OrderedOptions)
			{
				stringBuilder.AppendFormat("  {0}", orderedOption.Descriptor.DisplayName);
				stringBuilder.Append(' ', _DisplayNameMaxLength + 5 - orderedOption.Descriptor.DisplayName.Length);
				stringBuilder.Append(orderedOption.Descriptor.Description);
				object value = orderedOption.Descriptor.GetValue(_SettingsObject);
				if (value != null)
				{
					stringBuilder.AppendFormat(" (default: {0})", value);
				}
				stringBuilder.Append("\n");
			}
			foreach (OptionalOptionInfo optionalOption in _OptionalOptions)
			{
				stringBuilder.AppendFormat("  /{0}", optionalOption.Descriptor.DisplayName);
				stringBuilder.Append(' ', _DisplayNameMaxLength + 4 - optionalOption.Descriptor.DisplayName.Length);
				stringBuilder.Append(optionalOption.Descriptor.Description);
				object value2 = optionalOption.Descriptor.GetValue(_SettingsObject);
				if (value2 != null)
				{
					stringBuilder.AppendFormat(" (default: {0})", value2);
				}
				stringBuilder.AppendLine("");
			}
			return stringBuilder.ToString();
		}

		private static object ValidateValue(CommandLineOptionAttribute attribute, PropertyDescriptor descriptor, string value, List<string> messages)
		{
			object obj = null;
			try
			{
				obj = ((!descriptor.PropertyType.IsEnum) ? Convert.ChangeType(value.Trim('"'), descriptor.PropertyType) : Enum.Parse(descriptor.PropertyType, value, ignoreCase: true));
			}
			catch (Exception)
			{
				messages.Add($"Error: Value '{value}' is not valid for option '{descriptor.DisplayName}'");
				return null;
			}
			if (attribute.MaxValue != null && attribute.MinValue != null)
			{
				IComparable comparable = obj as IComparable;
				if (comparable.CompareTo(attribute.MinValue) == -1 || comparable.CompareTo(attribute.MaxValue) == 1)
				{
					messages.Add($"Error: Value '{value}' for option '{descriptor.DisplayName}' is out of bounds");
					return null;
				}
			}
			if (attribute.ValidValueSet != null)
			{
				bool flag = false;
				object[] validValueSet = attribute.ValidValueSet;
				foreach (object obj2 in validValueSet)
				{
					if (obj.Equals(obj2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					messages.Add($"Error: Value '{value}' is not valid for option '{descriptor.DisplayName}'");
					return null;
				}
			}
			return obj;
		}

		public bool ProcessOptions(string[] options, out string[] messages)
		{
			if (options.Length == 1 && !string.IsNullOrEmpty(options[0]) && options[0][0] == '@')
			{
				string text = options[0].Substring(1);
				if (!File.Exists(text))
				{
					throw new BinaryAssetBuilderException(ErrorCode.FileNotFound, "Response file '{0}' not found.", text);
				}
				options = File.ReadAllLines(text);
			}
			return ProcessOptionsInternal(options, out messages);
		}

		private bool ProcessOptionsInternal(string[] options, out string[] messages)
		{
			int num = 0;
			bool flag = false;
			List<string> list = new List<string>();
			int num2 = 0;
			while (num2 < _OrderedOptions.Count)
			{
				OrderedOptionInfo orderedOptionInfo = _OrderedOptions[num2];
				if (num == options.Length)
				{
					list.Add($"Error: Command line option '{orderedOptionInfo.Descriptor.DisplayName}' not specified");
					flag = true;
				}
				else
				{
					string text = options[num++].Trim();
					if (string.IsNullOrEmpty(text) || text[0] == '#')
					{
						continue;
					}
					object obj = ValidateValue(orderedOptionInfo.OptionAttribute, orderedOptionInfo.Descriptor, text, list);
					if (obj != null)
					{
						orderedOptionInfo.Descriptor.SetValue(_SettingsObject, obj);
					}
					else
					{
						flag = true;
					}
				}
				num2++;
			}
			while (num < options.Length)
			{
				string text2 = options[num++].Trim();
				if (string.IsNullOrEmpty(text2) || text2[0] == '#')
				{
					continue;
				}
				if (text2[0] != '/')
				{
					flag = true;
					list.Add($"Command line option '{text2}' does not start with '/'");
					continue;
				}
				string[] array = text2.Substring(1).Split(new char[1] { ':' }, 2);
				OptionalOptionInfo value = null;
				if (!_OptionalOptionLookup.TryGetValue(array[0].ToLower(), out value))
				{
					flag = true;
					list.Add($"Error: Unknown command line option '{array[0]}'");
					continue;
				}
				string text3 = null;
				if (array.Length == 2)
				{
					text3 = array[1].Trim();
				}
				else
				{
					if (value.Descriptor.PropertyType != typeof(bool))
					{
						flag = true;
						list.Add($"Error: No value for command line option '{value.Descriptor.DisplayName}' specified");
						continue;
					}
					text3 = "true";
				}
				object obj2 = ValidateValue(value.OptionAttribute, value.Descriptor, text3, list);
				if (obj2 != null)
				{
					value.Descriptor.SetValue(_SettingsObject, obj2);
				}
				else
				{
					flag = true;
				}
			}
			messages = list.ToArray();
			return !flag;
		}
	}
}
