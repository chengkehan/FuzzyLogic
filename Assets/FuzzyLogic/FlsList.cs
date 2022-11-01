using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace FuzzyLogicSystem
{
	// In fuzzy logic system, we can predict the size of list is fixed and changed infrequently.
	// Using FlsList, memory allocation is minimized.
	public class FlsList<T>
	{
		public delegate int CompareFunc(T left, T right);

		#region Recyclable Buffers

		/*
		 * Reuse buffers 
		 */

		// How many recyclable buffers can be stored
		public int recyclableBuffersCapacity
		{
			set
			{
				recyclableBuffers.Capacity = value;
			}
			get
			{
				return recyclableBuffers.Capacity;
			}
		}
		// Store not using buffers in this, reuse them when need.
		private List<T[]> _recyclableBuffers = null;
		private List<T[]> recyclableBuffers
		{
			get
			{
				if (_recyclableBuffers == null)
				{
					_recyclableBuffers = new List<T[]>(5);
				}
				return _recyclableBuffers;
			}
		}

		public void ReleaseRecyclableBuffers()
		{
			recyclableBuffers.Clear();
		}

		private void RecycleBuffer(ref T[] buffer)
		{
			if (buffer != null)
			{
				if (recyclableBuffers.Count < recyclableBuffersCapacity)
				{
					recyclableBuffers.Add(buffer);
				}
				else
				{
					// discard a buffer
				}
				buffer = null;
			}
		}

		private T[] FetchBuffer(int size)
		{
			for (int i = 0; i < recyclableBuffers.Count; i++)
			{
				var buffer = recyclableBuffers[i];
				if (buffer.Length == size)
				{
					for (int bufferIndex = 0; bufferIndex < buffer.Length; bufferIndex++)
					{
						buffer[bufferIndex] = default(T);
					}
					return buffer;
				}
			}
			return new T[size];
		}

		#endregion

		private T[] _buffer;

		private int _size = 0;
		public int size
		{
			private set
			{
				_size = value;
			}
			get
			{
				return _size;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (_buffer != null)
			{
				for (int i = 0; i < size; ++i)
				{
					yield return _buffer[i];
				}
			}
		}

		public T this[int i]
		{
			get
			{
				return _buffer[i];
			}
			set
			{
				_buffer[i] = value;
			}
		}

		private void AllocateMore()
		{
			if (_buffer == null)
			{
				_buffer = FetchBuffer(32);
			}
			else
			{
				T[] newBuffer = FetchBuffer(Mathf.Max(_buffer.Length << 1, 32));
				if (size > 0)
				{
					_buffer.CopyTo(newBuffer, 0);
				}
				RecycleBuffer(ref _buffer);
				_buffer = newBuffer;
			}
		}

		private void Trim()
		{
			if (size > 0)
			{
				if (size < _buffer.Length)
				{
					T[] newBuffer = FetchBuffer(size);
					for (int i = 0; i < size; i++)
					{
						newBuffer[i] = _buffer[i];
					}
					RecycleBuffer(ref _buffer);
					_buffer = newBuffer;
				}
			}
			else
			{
				RecycleBuffer(ref _buffer);
			}
		}

		public void Clear()
		{
			size = 0;
		}

		public void Release()
		{
			size = 0;
			RecycleBuffer(ref _buffer);
		}

		public void Add(T item)
		{
			if (_buffer == null || size == _buffer.Length)
			{
				AllocateMore();
			}
			_buffer[size++] = item;
		}

		public void Insert(int index, T item)
		{
			if (_buffer == null || size == _buffer.Length)
			{
				AllocateMore();
			}

			if (index > -1 && index < size)
			{
				for (int i = size; i > index; --i)
				{
					_buffer[i] = _buffer[i - 1];
				}
				_buffer[index] = item;
				++size;
			}
			else
			{
				Add(item);
			}
		}

		public bool Contains(T item)
		{
			if (_buffer == null)
			{
				return false;
			}
			for (int i = 0; i < size; ++i)
			{
				if (_buffer[i].Equals(item))
				{
					return true;
				}
			}
			return false;
		}

		public int IndexOf(T item)
		{
			if (_buffer == null)
			{
				return -1;
			}
			for (int i = 0; i < size; ++i)
			{
				if (_buffer[i].Equals(item))
				{
					return i;
				}
			}
			return -1;
		}

		public bool Remove(T item)
		{
			if (_buffer != null)
			{
				EqualityComparer<T> comp = EqualityComparer<T>.Default;

				for (int i = 0; i < size; ++i)
				{
					if (comp.Equals(_buffer[i], item))
					{
						--size;
						_buffer[i] = default(T);
						for (int b = i; b < size; ++b)
						{
							_buffer[b] = _buffer[b + 1];
						}
						_buffer[size] = default(T);
						return true;
					}
				}
			}
			return false;
		}

		public void RemoveAt(int index)
		{
			if (_buffer != null && index > -1 && index < size)
			{
				--size;
				_buffer[index] = default(T);
				for (int b = index; b < size; ++b)
				{
					_buffer[b] = _buffer[b + 1];
				}
				_buffer[size] = default(T);
			}
		}

		public T Pop()
		{
			if (_buffer != null && size != 0)
			{
				T val = _buffer[--size];
				_buffer[size] = default(T);
				return val;
			}
			return default(T);
		}

		public T[] ToArray()
		{
			Trim();
			return _buffer;
		}

		public void Sort(CompareFunc comparer)
		{
			Sort_Internal<object>(comparer, null);
		}

		// When sorting itself, meanwhile, apply the new order to secondaryList.
		public void Sort<T2>(CompareFunc comparer, FlsList<T2> secondaryList)
        {
			Sort_Internal(comparer, secondaryList);
        }

		private void Sort_Internal<T2>(CompareFunc comparer, FlsList<T2> secondaryList)
        {
			if (comparer == null)
			{
				throw new ArgumentNullException("Null comparer is not allowed.");
			}

			if (secondaryList != null && secondaryList.size != size)
            {
				throw new Exception("secondaryList.size must be equal to this.size");
			}

			for (int i = 0; i < size - 1; i++)
			{
				bool changed = false;
				for (int j = 0; j < size - 1; j++)
				{
					if (comparer(_buffer[j], _buffer[j + 1]) > 0)
					{
						Swap(this, j, j + 1);

                        if (secondaryList != null)
						{
							Swap(secondaryList, j, j + 1);
						}

						changed = true;
					}
				}
				if (changed == false)
				{
					break;
				}
			}
		}

		private void Swap<T3>(FlsList<T3> list, int i, int j)
        {
			T3 temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}
	}
}