﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace ObservableComputations
{
	public class ItemsProcessingVoid<TSourceItem> : CollectionComputing<TSourceItem>, IHasSourceCollections
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;

		// ReSharper disable once MemberCanBePrivate.Global
		public INotifyCollectionChanged Source => _source;

		public ReadOnlyCollection<INotifyCollectionChanged> SourceCollections => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceCollectionScalars => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		public Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> NewItemProcessor => _newItemProcessor;
		public Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> OldItemProcessor => _oldItemProcessor;
		public Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> MoveItemProcessor => _moveItemProcessor;

		private readonly Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> _newItemProcessor;
		private readonly Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> _oldItemProcessor;
		private readonly Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> _moveItemProcessor;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly PropertyChangedEventHandler _sourceScalarPropertyChangedEventHandler;
		private readonly WeakPropertyChangedEventHandler _sourceScalarWeakPropertyChangedEventHandler;

		private ObservableCollectionWithChangeMarker<TSourceItem> _sourceAsList;
		bool _rootSourceWrapper;
		private bool _lastProcessedSourceChangeMarker;

		private NotifyCollectionChangedEventHandler _sourceNotifyCollectionChangedEventHandler;
		private WeakNotifyCollectionChangedEventHandler _sourceWeakNotifyCollectionChangedEventHandler;
		private readonly IReadScalar<INotifyCollectionChanged> _sourceScalar;
		private INotifyCollectionChanged _source;

		[ObservableComputationsCall]
		public ItemsProcessingVoid(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> newItemProcessor = null,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> oldItemProcessor = null,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> moveItemProcessor = null) : this(newItemProcessor, oldItemProcessor, moveItemProcessor, Utils.getCapacity(sourceScalar))
		{
			_sourceScalar = sourceScalar;
			_sourceScalarPropertyChangedEventHandler = handleSourceScalarValueChanged;
			_sourceScalarWeakPropertyChangedEventHandler = new WeakPropertyChangedEventHandler(_sourceScalarPropertyChangedEventHandler);
			_sourceScalar.PropertyChanged += _sourceScalarWeakPropertyChangedEventHandler.Handle;

			initializeFromSource(null, null);
		}

		[ObservableComputationsCall]
		public ItemsProcessingVoid(
			INotifyCollectionChanged source,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> newItemProcessor = null,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> oldItemProcessor = null,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> moveItemProcessor = null) : this(newItemProcessor, oldItemProcessor, moveItemProcessor, Utils.getCapacity(source))
		{
			_source = source;
			initializeFromSource(null, null);
		}

		private ItemsProcessingVoid(
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> newItemProcessor,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> oldItemProcessor,
			Action<TSourceItem, ItemsProcessingVoid<TSourceItem>, object, EventArgs> moveItemProcessor, 
			int capacity) : base(capacity)
		{
			_newItemProcessor = newItemProcessor;
			_oldItemProcessor = oldItemProcessor;
			_moveItemProcessor = moveItemProcessor;
		}

		private void initializeFromSource(object sender, EventArgs eventArgs)
		{
			if (_sourceNotifyCollectionChangedEventHandler != null)
			{
				int capacity = _sourceScalar != null ? Utils.getCapacity(_sourceScalar) : Utils.getCapacity(_source);

				int count = Count;
				for (int i = 0; i < count; i++)
				{
					TSourceItem sourceItem = _sourceAsList[i];
					baseRemoveItem(0);
					if (_oldItemProcessor!= null) processOldItem(sourceItem, sender, eventArgs);
				}

				if (_rootSourceWrapper)
				{
					_sourceAsList.CollectionChanged -= _sourceNotifyCollectionChangedEventHandler;
				}
				else
				{
					_sourceAsList.CollectionChanged -= _sourceWeakNotifyCollectionChangedEventHandler.Handle;
					_sourceWeakNotifyCollectionChangedEventHandler = null;
				}

				_sourceNotifyCollectionChangedEventHandler = null;
			}

			if (_sourceScalar != null)
				_source = _sourceScalar.Value;
			_sourceAsList = null;

			if (_source != null)
			{
				if (_source is ObservableCollectionWithChangeMarker<TSourceItem> sourceAsList)
				{
					_sourceAsList = sourceAsList;
					_rootSourceWrapper = false;
				}
				else
				{
					_sourceAsList = new RootSourceWrapper<TSourceItem>(_source);
					_rootSourceWrapper = true;
				}

				_lastProcessedSourceChangeMarker = _sourceAsList.ChangeMarkerField;

				int count = _sourceAsList.Count;
				for (int index = 0; index < count; index++)
				{
					TSourceItem sourceItem = _sourceAsList[index];
					if (_newItemProcessor != null) processNewItem(sourceItem, sender, eventArgs);
					baseInsertItem(index, sourceItem);
				}

				_sourceNotifyCollectionChangedEventHandler = handleSourceCollectionChanged;

				if (_rootSourceWrapper)
				{
					_sourceAsList.CollectionChanged += _sourceNotifyCollectionChangedEventHandler;
				}
				else
				{
					_sourceWeakNotifyCollectionChangedEventHandler =
						new WeakNotifyCollectionChangedEventHandler(_sourceNotifyCollectionChangedEventHandler);

					_sourceAsList.CollectionChanged += _sourceWeakNotifyCollectionChangedEventHandler.Handle;
				}
			}
		}

		private void handleSourceScalarValueChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IReadScalar<INotifyCollectionChanged>.Value))
				return;
			checkConsistent();
			_isConsistent = false;

			initializeFromSource(sender, e);

			_isConsistent = true;
			raiseConsistencyRestored();
		}


		private void handleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			checkConsistent();
			if (!_rootSourceWrapper && _lastProcessedSourceChangeMarker == _sourceAsList.ChangeMarkerField) return;
			_lastProcessedSourceChangeMarker = !_lastProcessedSourceChangeMarker;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					_isConsistent = false;
					int newStartingIndex = e.NewStartingIndex;
					TSourceItem addedItem = _sourceAsList[newStartingIndex];
					if (_newItemProcessor != null) processNewItem(addedItem, sender, e);
					baseInsertItem(newStartingIndex, addedItem);
					_isConsistent = true;
					raiseConsistencyRestored();
					break;
				case NotifyCollectionChangedAction.Remove:
					_isConsistent = false;
					int oldStartingIndex = e.OldStartingIndex;
					TSourceItem removedItem = (TSourceItem) e.OldItems[0];
					baseRemoveItem(oldStartingIndex);
					if (_oldItemProcessor!= null) processOldItem(removedItem, sender, e);
					_isConsistent = true;
					raiseConsistencyRestored();
					break;
				case NotifyCollectionChangedAction.Replace:
					_isConsistent = false;
					int newStartingIndex1 = e.NewStartingIndex;
					TSourceItem oldItem = (TSourceItem) e.OldItems[0];
					TSourceItem newItem = _sourceAsList[newStartingIndex1];

					if (_newItemProcessor != null) processNewItem(newItem, sender, e);
					baseSetItem(newStartingIndex1, newItem);
					if (_oldItemProcessor!= null) processOldItem(oldItem, sender, e);
					_isConsistent = true;
					raiseConsistencyRestored();
					break;
				case NotifyCollectionChangedAction.Move:
					int oldStartingIndex2 = e.OldStartingIndex;
					int newStartingIndex2 = e.NewStartingIndex;
					if (oldStartingIndex2 != newStartingIndex2)
					{
						baseMoveItem(oldStartingIndex2, newStartingIndex2);
						if (_moveItemProcessor != null) processMovedItem(_sourceAsList[newStartingIndex2], sender, e);
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					_isConsistent = false;
					initializeFromSource(sender, e);
					_isConsistent = true;
					raiseConsistencyRestored();
					break;
			}
		}

		private void processNewItem(TSourceItem sourceItem, object sender, EventArgs eventArgs)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				DebugInfo._computingsExecutingUserCode.TryGetValue(currentThread, out IComputing computing);
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				_userCodeIsCalledFrom = computing;

				_newItemProcessor(sourceItem, this, sender, eventArgs);

				if (computing == null) DebugInfo._computingsExecutingUserCode.TryRemove(currentThread, out IComputing _);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				_userCodeIsCalledFrom = null;
				return;
			}

			_newItemProcessor(sourceItem, this, sender, eventArgs);
		}

		private void processOldItem(TSourceItem sourceItem, object sender, EventArgs eventArgs)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				DebugInfo._computingsExecutingUserCode.TryGetValue(currentThread, out IComputing computing);
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				_userCodeIsCalledFrom = computing;

				_oldItemProcessor(sourceItem, this, sender, eventArgs);

				if (computing == null) DebugInfo._computingsExecutingUserCode.TryRemove(currentThread, out IComputing _);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				_userCodeIsCalledFrom = null;
				return;
			}

			_oldItemProcessor(sourceItem, this, sender, eventArgs);
		}


		private void processMovedItem(TSourceItem sourceItem, object sender, EventArgs eventArgs)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				DebugInfo._computingsExecutingUserCode.TryGetValue(currentThread, out IComputing computing);
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				_userCodeIsCalledFrom = computing;

				_moveItemProcessor(sourceItem, this, sender, eventArgs);

				if (computing == null) DebugInfo._computingsExecutingUserCode.TryRemove(currentThread, out IComputing _);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				_userCodeIsCalledFrom = null;
				return;
			}

			_moveItemProcessor(sourceItem, this, sender, eventArgs);
		}

		~ItemsProcessingVoid()
		{
			if (_sourceWeakNotifyCollectionChangedEventHandler != null)
			{
				_sourceAsList.CollectionChanged -= _sourceWeakNotifyCollectionChangedEventHandler.Handle;
			}

			if (_sourceScalarWeakPropertyChangedEventHandler != null)
			{
				_sourceScalar.PropertyChanged -= _sourceScalarWeakPropertyChangedEventHandler.Handle;
			}
		}
	}
}
