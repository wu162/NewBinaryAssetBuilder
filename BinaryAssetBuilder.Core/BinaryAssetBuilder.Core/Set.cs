using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class Set<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct Nop
		{
		}

		private static Nop _TheNop = default(Nop);

		private IDictionary<T, Nop> _Dictionary;

		public int Count => _Dictionary.Count;

		public bool IsReadOnly => false;

		public static Set<T> Empty => new Set<T>(0);

		object ICollection.SyncRoot => ((ICollection)_Dictionary.Keys).SyncRoot;

		bool ICollection.IsSynchronized => ((ICollection)_Dictionary.Keys).IsSynchronized;

		public Set()
		{
			_Dictionary = new SortedDictionary<T, Nop>();
		}

		public Set(int capacity)
		{
			_Dictionary = new SortedDictionary<T, Nop>();
		}

		public Set(Set<T> other)
		{
			_Dictionary = new SortedDictionary<T, Nop>(other._Dictionary);
		}

		public Set(IEnumerable<T> original)
		{
			_Dictionary = new SortedDictionary<T, Nop>();
			AddRange(original);
		}

		public void Add(T a)
		{
			_Dictionary[a] = _TheNop;
		}

		public void AddRange(IEnumerable<T> range)
		{
			foreach (T item in range)
			{
				Add(item);
			}
		}

		public Set<U> ConvertAll<U>(Converter<T, U> converter)
		{
			Set<U> set = new Set<U>(Count);
			using IEnumerator<T> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				set.Add(converter(current));
			}
			return set;
		}

		public void Clear()
		{
			_Dictionary.Clear();
		}

		public bool Contains(T a)
		{
			return _Dictionary.ContainsKey(a);
		}

		public void CopyTo(T[] array, int index)
		{
			_Dictionary.Keys.CopyTo(array, index);
		}

		public T[] ToArray()
		{
			T[] array = new T[_Dictionary.Keys.Count];
			_Dictionary.Keys.CopyTo(array, 0);
			return array;
		}

		public bool Remove(T a)
		{
			return _Dictionary.Remove(a);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _Dictionary.Keys.GetEnumerator();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Set<T> set))
			{
				return false;
			}
			return this == set;
		}

		public override int GetHashCode()
		{
			int num = 0;
			using IEnumerator<T> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				num ^= enumerator.Current.GetHashCode();
			}
			return num;
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)_Dictionary.Keys).CopyTo(array, index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_Dictionary.Keys).GetEnumerator();
		}
	}
}
