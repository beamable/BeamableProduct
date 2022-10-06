using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Common
{
	// TODO: TD000006
	[Serializable]
	public class UndoSystem<T>
	{
		private static readonly List<UndoRecord> _records = new List<UndoRecord>();

		public static void AddRecord(T objectToRecord, string key, int maxUndoHistorySize = 10)
		{
			if (IsKeyRecorded(key))
				return;

			var entry = new UndoRecord(objectToRecord, key, maxUndoHistorySize);
			_records.Add(entry);
		}

		public static void DeleteRecord(string key)
		{
			if (_records.Count == 0)
				return;

			var record = _records.FirstOrDefault(x => x.Key == key);
			if (record == null)
				return;

			DeleteRecord(record);
		}
		public static void DeleteAllRecords()
		{
			if (_records.Count == 0)
				return;

			foreach (var record in _records.ToList())
				DeleteRecord(record);
			_records.Clear();
		}
		public static void Undo(string key)
		{
			if (!IsKeyRecorded(key))
				return;
			_records.First(x => x.Key == key).Undo();
		}
		public static void Update()
		{
			if (_records.Count == 0)
				return;

			foreach (var record in _records.ToList())
				record.Compare();
		}

		private static void DeleteRecord(UndoRecord record) => _records.Remove(record);
		private static bool IsKeyRecorded(string key) => _records.Any(x => x.Key == key);

		[Serializable]
		private class UndoRecord
		{
			public string Key => _key;

			[SerializeField] private string _key;
			private string _previous;
			private readonly T _current;
			private readonly CustomStack<string> _history;

			public UndoRecord(T current, string key, int maxUndoHistorySize)
			{
				_key = key;
				_current = current;
				_previous = EditorJsonUtility.ToJson(_current);
				_history = new CustomStack<string>(maxUndoHistorySize);
			}
			public void Compare()
			{
				var currentJson = EditorJsonUtility.ToJson(_current);
				var areEquals = currentJson == _previous;

				if (areEquals)
					return;

				_history.Push(_previous);
				_previous = EditorJsonUtility.ToJson(_current);
			}
			public void Undo()
			{
				if (_history.Count == 0)
					return;

				_previous = _history.Pop();
				EditorJsonUtility.FromJsonOverwrite(_previous, _current);
			}
		}

		private class CustomStack<T1>
		{
			public int Count => _list.Count;

			private readonly List<T1> _list = new List<T1>();
			private readonly int _maxSize;

			public CustomStack(int maxSize = -1)
			{
				_maxSize = maxSize;
			}
			public void Push(T1 element)
			{
				if (_maxSize == Count)
					_list.RemoveAt(0);
				_list.Add(element);
			}
			public T1 Pop()
			{
				if (_list.Count == 0)
					return default;

				var element = _list[Count - 1];
				_list.Remove(element);
				return element;
			}
		}
	}
}
