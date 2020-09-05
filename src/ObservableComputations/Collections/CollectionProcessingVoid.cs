﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableComputations
{
	public class CollectionProcessingVoid<TSourceItem> : CollectionComputing<TSourceItem>, IHasSourceCollections, ISourceIndexerPropertyTracker, ISourceCollectionChangeProcessor
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;

		// ReSharper disable once MemberCanBePrivate.Global
		public INotifyCollectionChanged Source => _source;

		public ReadOnlyCollection<INotifyCollectionChanged> SourceCollections => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceCollectionScalars => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		public Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> NewItemProcessor => _newItemProcessor;
		public Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> OldItemProcessor => _oldItemProcessor;
		public Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> MoveItemProcessor => _moveItemProcessor;

		private readonly Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> _newItemProcessor;
		private readonly Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> _oldItemProcessor;
		private readonly Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> _moveItemProcessor;


		private IList<TSourceItem> _sourceAsList;
        private IHasChangeMarker _sourceAsIHasChangeMarker;
		private bool _lastProcessedSourceChangeMarker;

		private bool _sourceInitialized;
		private readonly IReadScalar<INotifyCollectionChanged> _sourceScalar;
		private INotifyCollectionChanged _source;

        private bool _indexerPropertyChangedEventRaised;
        private INotifyPropertyChanged _sourceAsINotifyPropertyChanged;

        private ISourceCollectionChangeProcessor _thisAsSourceCollectionChangeProcessor;

		[ObservableComputationsCall]
		public CollectionProcessingVoid(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> newItemProcessor = null,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> oldItemProcessor = null,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> moveItemProcessor = null) : this(newItemProcessor, oldItemProcessor, moveItemProcessor, Utils.getCapacity(sourceScalar))
		{
			_sourceScalar = sourceScalar;
            _thisAsSourceCollectionChangeProcessor = this;
		}

		[ObservableComputationsCall]
		public CollectionProcessingVoid(
			INotifyCollectionChanged source,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> newItemProcessor = null,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> oldItemProcessor = null,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> moveItemProcessor = null) : this(newItemProcessor, oldItemProcessor, moveItemProcessor, Utils.getCapacity(source))
		{
			_source = source;
            _thisAsSourceCollectionChangeProcessor = this;
		}

		private CollectionProcessingVoid(
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> newItemProcessor,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> oldItemProcessor,
			Action<TSourceItem, CollectionProcessingVoid<TSourceItem>> moveItemProcessor, 
			int capacity) : base(capacity)
		{
			_newItemProcessor = newItemProcessor;
			_oldItemProcessor = oldItemProcessor;
			_moveItemProcessor = moveItemProcessor;
            _thisAsSourceCollectionChangeProcessor = this;
		}

        protected override void initializeFromSource()
		{
			if (_sourceInitialized)
			{
				int count = Count;
				for (int i = 0; i < count; i++)
				{
					TSourceItem sourceItem = _sourceAsList[i];
					baseRemoveItem(0);
					if (_oldItemProcessor!= null) processOldItem(sourceItem);
				}

                _source.CollectionChanged -= handleSourceCollectionChanged;
                _sourceInitialized = false;
            }

            Utils.changeSource(ref _source, _sourceScalar, _downstreamConsumedComputings, _consumers, this,
                ref _sourceAsList, true);

            if (_sourceAsList != null && _isActive)
            {
                Utils.initializeFromHasChangeMarker(
                    out _sourceAsIHasChangeMarker, 
                    _sourceAsList, 
                    ref _lastProcessedSourceChangeMarker, 
                    ref _sourceAsINotifyPropertyChanged,
                    (ISourceIndexerPropertyTracker)this);

				int count = _sourceAsList.Count;
				for (int index = 0; index < count; index++)
				{
					TSourceItem sourceItem = _sourceAsList[index];
					if (_newItemProcessor != null) processNewItem(sourceItem);
					baseInsertItem(index, sourceItem);
				}

                _source.CollectionChanged += handleSourceCollectionChanged;
                _sourceInitialized = true;
            }
		}

		private void handleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
            if (!Utils.preHandleSourceCollectionChanged(
                sender, 
                e, 
                ref _isConsistent, 
                ref _indexerPropertyChangedEventRaised, 
                ref _lastProcessedSourceChangeMarker, 
                _sourceAsIHasChangeMarker, 
                ref _handledEventSender, 
                ref _handledEventArgs,
                ref _deferredProcessings,
                0, 1, this)) return;

			_thisAsSourceCollectionChangeProcessor.processSourceCollectionChanged(sender, e);

            Utils.postHandleChange(
                ref _handledEventSender,
                ref _handledEventArgs,
                _deferredProcessings,
                out _isConsistent,
                this);
		}

        void ISourceCollectionChangeProcessor.processSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    int newStartingIndex = e.NewStartingIndex;
                    TSourceItem addedItem = (TSourceItem) e.NewItems[0];
                    if (_newItemProcessor != null) processNewItem(addedItem);
                    baseInsertItem(newStartingIndex, addedItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    int oldStartingIndex = e.OldStartingIndex;
                    TSourceItem removedItem = (TSourceItem) e.OldItems[0];
                    baseRemoveItem(oldStartingIndex);
                    if (_oldItemProcessor != null) processOldItem(removedItem);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    int newStartingIndex1 = e.NewStartingIndex;
                    TSourceItem oldItem = (TSourceItem) e.OldItems[0];
                    TSourceItem newItem = _sourceAsList[newStartingIndex1];

                    if (_newItemProcessor != null) processNewItem(newItem);
                    baseSetItem(newStartingIndex1, newItem);
                    if (_oldItemProcessor != null) processOldItem(oldItem);
                    break;
                case NotifyCollectionChangedAction.Move:
                    int oldStartingIndex2 = e.OldStartingIndex;
                    int newStartingIndex2 = e.NewStartingIndex;
                    if (oldStartingIndex2 != newStartingIndex2)
                    {
                        baseMoveItem(oldStartingIndex2, newStartingIndex2);
                        if (_moveItemProcessor != null) processMovedItem((TSourceItem) e.NewItems[0]);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    initializeFromSource();
                    break;
            }
        }

        private void processNewItem(TSourceItem sourceItem)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
                var currentThread = Utils.startComputingExecutingUserCode(out var computing, out _userCodeIsCalledFrom, this);
				_newItemProcessor(sourceItem, this);
                Utils.endComputingExecutingUserCode(computing, currentThread, out _userCodeIsCalledFrom);
				return;
			}

			_newItemProcessor(sourceItem, this);
		}

		private void processOldItem(TSourceItem sourceItem)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
                var currentThread = Utils.startComputingExecutingUserCode(out var computing, out _userCodeIsCalledFrom, this);
				_oldItemProcessor(sourceItem, this);
                Utils.endComputingExecutingUserCode(computing, currentThread, out _userCodeIsCalledFrom);
				return;
			}

			_oldItemProcessor(sourceItem, this);
		}


		private void processMovedItem(TSourceItem sourceItem)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
                var currentThread = Utils.startComputingExecutingUserCode(out var computing, out _userCodeIsCalledFrom, this);
				_moveItemProcessor(sourceItem, this);
                Utils.endComputingExecutingUserCode(computing, currentThread, out _userCodeIsCalledFrom);
				return;
			}

			_moveItemProcessor(sourceItem, this);
		}

        internal override void addToUpstreamComputings(IComputingInternal computing)
        {
            (_source as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
            (_sourceScalar as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
        }

        internal override void removeFromUpstreamComputings(IComputingInternal computing)        
        {
            (_source as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
            (_sourceScalar as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
        }

        protected override void initialize()
        {
            Utils.initializeSourceScalar(_sourceScalar, ref _source, scalarValueChangedHandler);
        }

        protected override void uninitialize()
        {
            Utils.uninitializeSourceScalar(_sourceScalar, scalarValueChangedHandler, ref _source);
        }

        #region Implementation of ISourceIndexerPropertyTracker

        void ISourceIndexerPropertyTracker.HandleSourcePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            Utils.HandleSourcePropertyChanged(propertyChangedEventArgs, ref _indexerPropertyChangedEventRaised);
        }

        #endregion
	}
}
