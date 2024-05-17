using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class Collection2<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
	{
		[NonSerialized]
		private object _syncRoot;

		private IList<T> items;

		public int Count => items.Count;

		public T this[int index]
		{
			get
			{
				return items[index];
			}
			set
			{
				if (items.IsReadOnly)
				{
					throw new Exception("Internal error");
				}
				if (index < 0 || index >= items.Count)
				{
					throw new Exception("Internal error");
				}
				SetItem(index, value);
			}
		}

		protected IList<T> Items => items;

		bool ICollection<T>.IsReadOnly => items.IsReadOnly;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if (_syncRoot == null)
				{
					if (items is ICollection collection)
					{
						_syncRoot = collection.SyncRoot;
					}
					else
					{
						Interlocked.CompareExchange(ref _syncRoot, new object(), null);
					}
				}
				return _syncRoot;
			}
		}

		bool IList.IsFixedSize
		{
			get
			{
				if (items is IList list)
				{
					return list.IsFixedSize;
				}
				return false;
			}
		}

		bool IList.IsReadOnly => items.IsReadOnly;

		object IList.this[int index]
		{
			get
			{
				return items[index];
			}
			set
			{
				VerifyValueType(value);
				this[index] = (T)value;
			}
		}

		public Collection2()
		{
			items = new List2<T>();
		}

		public Collection2(IList<T> list)
		{
			if (list == null)
			{
				throw new Exception("Internal error");
			}
			items = list;
		}

		public void Add(T item)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			int count = items.Count;
			InsertItem(count, item);
		}

		public void Clear()
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			ClearItems();
		}

		protected virtual void ClearItems()
		{
			items.Clear();
		}

		public bool Contains(T item)
		{
			return items.Contains(item);
		}

		public void CopyTo(T[] array, int index)
		{
			items.CopyTo(array, index);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return items.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			if (index < 0 || index > items.Count)
			{
				throw new Exception("Internal error");
			}
			InsertItem(index, item);
		}

		protected virtual void InsertItem(int index, T item)
		{
			items.Insert(index, item);
		}

		private static bool IsCompatibleObject(object value)
		{
			if (!(value is T) && (value != null || typeof(T).IsValueType))
			{
				return false;
			}
			return true;
		}

		public bool Remove(T item)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			int num = items.IndexOf(item);
			if (num < 0)
			{
				return false;
			}
			RemoveItem(num);
			return true;
		}

		public void RemoveAt(int index)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			if (index < 0 || index >= items.Count)
			{
				throw new Exception("Internal error");
			}
			RemoveItem(index);
		}

		protected virtual void RemoveItem(int index)
		{
			items.RemoveAt(index);
		}

		protected virtual void SetItem(int index, T item)
		{
			items[index] = item;
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new Exception("Internal error");
			}
			if (array.Rank != 1)
			{
				throw new Exception("Internal error");
			}
			if (array.GetLowerBound(0) != 0)
			{
				throw new Exception("Internal error");
			}
			if (index < 0)
			{
				throw new Exception("Internal error");
			}
			if (array.Length - index < Count)
			{
				throw new Exception("Internal error");
			}
			if (array is T[] array2)
			{
				items.CopyTo(array2, index);
				return;
			}
			Type elementType = array.GetType().GetElementType();
			Type typeFromHandle = typeof(T);
			if (!elementType.IsAssignableFrom(typeFromHandle) && !typeFromHandle.IsAssignableFrom(elementType))
			{
				throw new Exception("Internal error");
			}
			if (!(array is object[] array3))
			{
				throw new Exception("Internal error");
			}
			int count = items.Count;
			try
			{
				for (int i = 0; i < count; i++)
				{
					array3[index++] = items[i];
				}
			}
			catch (ArrayTypeMismatchException)
			{
				throw new Exception("Internal error");
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}

		int IList.Add(object value)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			VerifyValueType(value);
			Add((T)value);
			return Count - 1;
		}

		bool IList.Contains(object value)
		{
			if (IsCompatibleObject(value))
			{
				return Contains((T)value);
			}
			return false;
		}

		int IList.IndexOf(object value)
		{
			if (IsCompatibleObject(value))
			{
				return IndexOf((T)value);
			}
			return -1;
		}

		void IList.Insert(int index, object value)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			VerifyValueType(value);
			Insert(index, (T)value);
		}

		void IList.Remove(object value)
		{
			if (items.IsReadOnly)
			{
				throw new Exception("Internal error");
			}
			if (IsCompatibleObject(value))
			{
				Remove((T)value);
			}
		}

		private static void VerifyValueType(object value)
		{
			if (!IsCompatibleObject(value))
			{
				throw new Exception("Internal error");
			}
		}
	}
}
