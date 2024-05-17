using System;
using System.Collections.Generic;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class InstanceSet : KeyedCollection2<InstanceHandle, InstanceDeclaration>
	{
		protected override InstanceHandle GetKeyForItem(InstanceDeclaration item)
		{
			return item.Handle;
		}

		public bool TryGetValue(InstanceHandle key, out InstanceDeclaration value)
		{
			if (base.Dictionary == null)
			{
				value = null;
				return false;
			}
			return base.Dictionary.TryGetValue(key, out value);
		}

		public InstanceDeclaration[] ToArray()
		{
			if (base.Count == 0)
			{
				return null;
			}
			List2<InstanceDeclaration> list = new List2<InstanceDeclaration>();
			foreach (InstanceDeclaration item in base.Items)
			{
				list.Add(item);
			}
			return list.ToArray();
		}

		public InstanceSet()
		{
		}

		public InstanceSet(AssetDeclarationDocument document, IEnumerable<InstanceDeclaration> instances)
		{
			if (instances == null)
			{
				return;
			}
			foreach (InstanceDeclaration instance in instances)
			{
				instance.Initialize(document);
				Add(instance);
			}
		}

		public bool TryAdd(InstanceDeclaration declaration)
		{
			if (Contains(declaration.Handle))
			{
				return false;
			}
			Add(declaration);
			return true;
		}

		public void Add(InstanceSet other)
		{
			foreach (InstanceDeclaration item in other)
			{
				TryAdd(item);
			}
		}

		public void Add(IList<InstanceDeclaration> other)
		{
			foreach (InstanceDeclaration item in other)
			{
				TryAdd(item);
			}
		}
	}
}
