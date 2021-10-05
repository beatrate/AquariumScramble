using System;
using System.Collections.Generic;

namespace Beatrate.Core
{
	public class GenericObjectPool<T> where T : new()
	{
		private readonly Stack<T> objects = new Stack<T>();
		private readonly Action<T> resetCallback;

		public GenericObjectPool(Action<T> resetCallback)
		{
			this.resetCallback = resetCallback;
		}

		public T Get()
		{
			if(objects.Count == 0)
			{
				return new T();
			}

			return objects.Pop();
		}

		public void Return(T item)
		{
			resetCallback(item);
			objects.Push(item);
		}
	}

	public class ListPool<T>
	{
		private static GenericObjectPool<List<T>> pool = new GenericObjectPool<List<T>>(item => item.Clear());

		public static List<T> Get() => pool.Get();
		public static void Return(List<T> item) => pool.Return(item);
	}

	public class QueuePool<T>
	{
		private static GenericObjectPool<Queue<T>> pool = new GenericObjectPool<Queue<T>>(item => item.Clear());

		public static Queue<T> Get() => pool.Get();
		public static void Return(Queue<T> item) => pool.Return(item);
	}

	public class DictionaryPool<T, U>
	{
		private static GenericObjectPool<Dictionary<T, U>> pool = new GenericObjectPool<Dictionary<T, U>> (item => item.Clear());

		public static Dictionary<T, U> Get() => pool.Get();
		public static void Return(Dictionary<T, U> item) => pool.Return(item);
	}
}