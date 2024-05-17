using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class List2<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
	{
		private class ComparisonComparer : IComparer<T>
		{
			private Comparison<T> _comparison;

			public ComparisonComparer(Comparison<T> comparison)
			{
				_comparison = comparison;
			}

			public int Compare(T left, T right)
			{
				return _comparison(left, right);
			}
		}

		[Serializable]
		public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			private List2<T> list;

			private int index;

			private int version;

			private T current;

			public T Current => current;

			object IEnumerator.Current
			{
				get
				{
					if (index == 0 || index == list._size + 1)
					{
						throw new Exception("Internal error");
					}
					return Current;
				}
			}

			internal Enumerator(List2<T> list)
			{
				this.list = list;
				index = 0;
				version = list._version;
				current = default(T);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (version != list._version)
				{
					throw new Exception("Internal error");
				}
				if (index < list._size)
				{
					current = list[index];
					index++;
					return true;
				}
				index = list._size + 1;
				current = default(T);
				return false;
			}

			void IEnumerator.Reset()
			{
				if (version != list._version)
				{
					throw new Exception("Internal error");
				}
				index = 0;
				current = default(T);
			}
		}

		private const int _defaultCapacity = 4;

		private const int _maxBinCapacity = 16384;

		private List<List<T>> _binList;

		private int _size;

		[NonSerialized]
		private object _syncRoot;

		private int _version;

		public int Capacity
		{
			get
			{
				return (_binList.Count - 1) * 16384 + _binList[_binList.Count - 1].Capacity;
			}
			set
			{
			}
		}

		public int Count => _size;

		public T this[int index]
		{
			get
			{
				int num = index / 16384;
				int index2 = index - num * 16384;
				return _binList[num][index2];
			}
			set
			{
				int num = index / 16384;
				int index2 = index - num * 16384;
				_binList[num][index2] = value;
				_version++;
			}
		}

		bool ICollection<T>.IsReadOnly => false;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if (_syncRoot == null)
				{
					Interlocked.CompareExchange(ref _syncRoot, new object(), null);
				}
				return _syncRoot;
			}
		}

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				VerifyValueType(value);
				this[index] = (T)value;
			}
		}

		public List2()
		{
			_binList = new List<List<T>>(0);
		}

		public List2(IEnumerable<T> collection)
			: this()
		{
			if (collection == null)
			{
				throw new Exception("Null collection");
			}
			foreach (T item in collection)
			{
				Add(item);
			}
		}

		public void Add(T item)
		{
			if (_binList.Count == 0)
			{
				_binList.Add(new List<T>());
			}
			List<T> list = _binList[_binList.Count - 1];
			if (list.Capacity > 8192 && list.Capacity != 16384)
			{
				list.Capacity = 16384;
			}
			if (list.Count >= 16384)
			{
				list = new List<T>();
				_binList.Add(list);
			}
			list.Add(item);
			_size++;
			_version++;
		}

		public void AddRange(IEnumerable<T> collection)
		{
			foreach (T item in collection)
			{
				Add(item);
			}
		}

		public ReadOnlyCollection<T> AsReadOnly()
		{
			return new ReadOnlyCollection<T>(this);
		}

		public int BinarySearch(T item)
		{
			return BinarySearch(0, Count, item, null);
		}

		public int BinarySearch(T item, IComparer<T> comparer)
		{
			return BinarySearch(0, Count, item, comparer);
		}

		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			if (index != 0)
			{
				throw new Exception("Sorry, List2.BinarySearch() does not support starting from a non-zero index");
			}
			int num = 0;
			int num2 = -1;
			foreach (List<T> bin in _binList)
			{
				num2 = bin.BinarySearch(item, comparer);
				if (num2 < 0)
				{
					num++;
					continue;
				}
				break;
			}
			if (num2 >= 0)
			{
				return num * 16384 + num2;
			}
			throw new Exception("Sorry, List2.BinarySearch() was unable to find the input item, and the return value in this case does not properly follow the List.BinarySearch() contract at this time.");
		}

		public void Clear()
		{
			_binList.Clear();
			_size = 0;
			_version++;
		}

		public bool Contains(T item)
		{
			foreach (List<T> bin in _binList)
			{
				if (bin.Contains(item))
				{
					return true;
				}
			}
			return false;
		}

		public IList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
		{
			List2<TOutput> list = new List2<TOutput>();
			int num = 0;
			foreach (List<T> bin in _binList)
			{
				foreach (T item in bin)
				{
					list[num] = converter(item);
					num++;
				}
			}
			return list;
		}

		public void CopyTo(T[] array)
		{
			CopyTo(array, 0);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(0, array, arrayIndex, Count);
		}

		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			for (int i = 0; i < count; i++)
			{
				array[arrayIndex + i] = this[index + i];
			}
		}

		public bool Exists(Predicate<T> match)
		{
			return FindIndex(match) != -1;
		}

		public T Find(Predicate<T> match)
		{
			foreach (List<T> bin in _binList)
			{
				foreach (T item in bin)
				{
					if (match(item))
					{
						return item;
					}
				}
			}
			return default(T);
		}

		public IList<T> FindAll(Predicate<T> match)
		{
			List2<T> list = new List2<T>();
			foreach (List<T> bin in _binList)
			{
				foreach (T item in bin)
				{
					if (match(item))
					{
						list.Add(item);
					}
				}
			}
			return list;
		}

		public int FindIndex(Predicate<T> match)
		{
			return FindIndex(0, _size, match);
		}

		public int FindIndex(int startIndex, Predicate<T> match)
		{
			return FindIndex(startIndex, _size - startIndex, match);
		}

		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			for (int i = 0; i < count; i++)
			{
				if (match(this[i + startIndex]))
				{
					return i + startIndex;
				}
			}
			return -1;
		}

		public T FindLast(Predicate<T> match)
		{
			for (int num = Count - 1; num >= 0; num--)
			{
				if (match(this[num]))
				{
					return this[num];
				}
			}
			return default(T);
		}

		public int FindLastIndex(Predicate<T> match)
		{
			return FindLastIndex(Count - 1, Count, match);
		}

		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return FindLastIndex(startIndex, startIndex + 1, match);
		}

		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			int num = startIndex - count;
			for (int num2 = startIndex; num2 > num; num2--)
			{
				if (match(this[num2]))
				{
					return num2;
				}
			}
			return -1;
		}

		public void ForEach(Action<T> action)
		{
			foreach (List<T> bin in _binList)
			{
				foreach (T item in bin)
				{
					action(item);
				}
			}
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		public IList<T> GetRange(int index, int count)
		{
			List2<T> list = new List2<T>();
			for (int i = 0; i < count; i++)
			{
				list.Add(this[index + i]);
			}
			return list;
		}

		public int IndexOf(T item)
		{
			return IndexOf(item, 0, Count);
		}

		public int IndexOf(T item, int index)
		{
			return IndexOf(item, index, Count - index);
		}

		public int IndexOf(T item, int index, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (this[i + index].Equals(item))
				{
					return i + index;
				}
			}
			return -1;
		}

		public void Insert(int index, T item)
		{
			if (index == Count)
			{
				Add(item);
				return;
			}
			Add(this[Count - 1]);
			for (int num = Count - 2; num > index; num--)
			{
				this[num] = this[num - 1];
			}
			this[index] = item;
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			foreach (T item in collection)
			{
				Insert(index, item);
				index++;
			}
		}

		private static bool IsCompatibleObject(object value)
		{
			if (!(value is T) && (value != null || typeof(T).IsValueType))
			{
				return false;
			}
			return true;
		}

		public int LastIndexOf(T item)
		{
			return LastIndexOf(item, Count - 1, Count);
		}

		public int LastIndexOf(T item, int index)
		{
			return LastIndexOf(item, index, index + 1);
		}

		public int LastIndexOf(T item, int index, int count)
		{
			for (int num = index; num > index - count; num--)
			{
				if (this[num].Equals(item))
				{
					return num;
				}
			}
			return -1;
		}

		public bool Remove(T item)
		{
			int num = IndexOf(item);
			if (num >= 0)
			{
				RemoveAt(num);
				return true;
			}
			return false;
		}

		public int RemoveAll(Predicate<T> match)
		{
			int num = 0;
			for (int i = 0; i < Count; i++)
			{
				if (match(this[i]))
				{
					RemoveAt(i);
					num++;
				}
			}
			return num;
		}

		public void RemoveAt(int index)
		{
			int num = index / 16384;
			int index2 = index - num * 16384;
			List<T> list = _binList[num];
			list.RemoveAt(index2);
			if (num != _binList.Count - 1)
			{
				for (num++; num < _binList.Count; num++)
				{
					List<T> list2 = _binList[num];
					T item = list2[0];
					list.Add(item);
					list2.RemoveAt(0);
					list = list2;
				}
			}
			if (_binList[_binList.Count - 1].Count == 0)
			{
				_binList.RemoveAt(_binList.Count - 1);
			}
			_size--;
			_version++;
		}

		public void RemoveRange(int index, int count)
		{
			for (int i = 0; i < count; i++)
			{
				RemoveAt(i + index);
			}
		}

		public void Reverse()
		{
			foreach (List<T> bin in _binList)
			{
				bin.Reverse();
			}
			_binList.Reverse();
			_version++;
		}

		public void Reverse(int index, int count)
		{
			for (int i = 0; i < count / 2; i++)
			{
				Swap(i + index, index + count - i);
			}
			_version++;
		}

		private void Swap(int index1, int index2)
		{
			T value = this[index1];
			this[index1] = this[index2];
			this[index2] = value;
		}

		public void Sort()
		{
			Sort(0, Count, null);
		}

		public void Sort(IComparer<T> comparer)
		{
			Sort(0, Count, comparer);
		}

		public void Sort(Comparison<T> comparison)
		{
			Sort(new ComparisonComparer(comparison));
		}

		public void Sort(int index, int count, IComparer<T> comparer)
		{
			if (index != 0 || count != Count)
			{
				throw new Exception("Sorry, List2 does not support partial sorting");
			}
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			foreach (List<T> bin in _binList)
			{
				bin.Sort(comparer);
			}
			List2<T> list = new List2<T>();
			int[] array = new int[_binList.Count];
			for (int i = 0; i < Count; i++)
			{
				T smallestItem = default(T);
				int num = SortHelp_GetSmallest(array, comparer, out smallestItem);
				list.Add(smallestItem);
				array[num]++;
			}
			_binList = list._binList;
			_version++;
		}

		private int SortHelp_GetSmallest(int[] runningIndicies, IComparer<T> comparer, out T smallestItem)
		{
			int num = -1;
			smallestItem = default(T);
			for (int i = 0; i < runningIndicies.Length; i++)
			{
				if (runningIndicies[i] >= _binList[i].Count)
				{
					continue;
				}
				T val = _binList[i][runningIndicies[i]];
				if (smallestItem == null)
				{
					smallestItem = val;
					num = i;
					continue;
				}
				int num2 = comparer.Compare(smallestItem, val);
				if (num2 > 0)
				{
					smallestItem = val;
					num = i;
				}
			}
			if (num == -1)
			{
				throw new Exception("Internal error");
			}
			return num;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new Enumerator(this);
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
			{
				array.SetValue(this[i], arrayIndex + i);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		int IList.Add(object item)
		{
			VerifyValueType(item);
			Add((T)item);
			return Count - 1;
		}

		bool IList.Contains(object item)
		{
			if (IsCompatibleObject(item))
			{
				return Contains((T)item);
			}
			return false;
		}

		int IList.IndexOf(object item)
		{
			if (IsCompatibleObject(item))
			{
				return IndexOf((T)item);
			}
			return -1;
		}

		void IList.Insert(int index, object item)
		{
			VerifyValueType(item);
			Insert(index, (T)item);
		}

		void IList.Remove(object item)
		{
			if (IsCompatibleObject(item))
			{
				Remove((T)item);
			}
		}

		public T[] ToArray()
		{
			T[] array = new T[Count];
			for (int i = 0; i < Count; i++)
			{
				array[i] = this[i];
			}
			return array;
		}

		public void TrimExcess()
		{
		}

		public bool TrueForAll(Predicate<T> match)
		{
			for (int i = 0; i < _size; i++)
			{
				if (!match(this[i]))
				{
					return false;
				}
			}
			return true;
		}

		private static void VerifyValueType(object value)
		{
			if (!IsCompatibleObject(value))
			{
				throw new Exception("Incompatible value type");
			}
		}
	}
}
