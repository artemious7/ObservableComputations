﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq.Expressions;

namespace ObservableComputations
{
    public class Selecting<TSourceItem, TResultItem> : CollectionComputing<TResultItem>, IHasSourceCollections, ISourceItemChangeProcessor, ISourceCollectionChangeProcessor
    {
		// ReSharper disable once MemberCanBePrivate.Global
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;

		// ReSharper disable once MemberCanBePrivate.Global
		public Expression<Func<TSourceItem, TResultItem>> SelectorExpression => _selectorExpressionOriginal;

		// ReSharper disable once MemberCanBePrivate.Global
		public INotifyCollectionChanged Source => _source;

		// ReSharper disable once MemberCanBePrivate.Global
		public Func<TSourceItem, TResultItem> SelectorFunc => _selectorFunc;

		public ReadOnlyCollection<INotifyCollectionChanged> SourceCollections => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceCollectionScalars => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		private Positions<ItemInfo> _sourcePositions;
		private List<ItemInfo> _itemInfos;

		private Expression<Func<TSourceItem, TResultItem>> _selectorExpression;
		private ExpressionWatcher.ExpressionInfo _selectorExpressionInfo;
        private int _selectorExpressionСallCount;

		private bool _selectorContainsParametrizedObservableComputationsCalls;

		private ObservableCollectionWithChangeMarker<TSourceItem> _sourceAsList;
		bool _rootSourceWrapper;
		private bool _lastProcessedSourceChangeMarker;

		private bool _sourceInitialized;
		private readonly IReadScalar<INotifyCollectionChanged> _sourceScalar;
		private Expression<Func<TSourceItem, TResultItem>> _selectorExpressionOriginal;
		internal INotifyCollectionChanged _source;
		private Func<TSourceItem, TResultItem> _selectorFunc;

        private List<IComputingInternal> _nestedComputings;

        private ISourceCollectionChangeProcessor _thisAsSourceCollectionChangeProcessor;
        private ISourceItemChangeProcessor _thisAsSourceItemChangeProcessor;

		private sealed class ItemInfo : ExpressionItemInfo
		{
			public Func<TResultItem> SelectorFunc;
        }

		[ObservableComputationsCall]
		public Selecting(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			Expression<Func<TSourceItem, TResultItem>> selectorExpression) : this(selectorExpression, Utils.getCapacity(sourceScalar))
		{
			_sourceScalar = sourceScalar;
		}

		[ObservableComputationsCall]
		public Selecting(
			INotifyCollectionChanged source,
			Expression<Func<TSourceItem, TResultItem>> selectorExpression) : this(selectorExpression, Utils.getCapacity(source))
		{
			_source = source;
		}

		private Selecting(Expression<Func<TSourceItem, TResultItem>> selectorExpression, int capacity) : base(capacity)
        {
            Utils.construct(
                selectorExpression, 
                capacity, 
                out _itemInfos, 
                out _sourcePositions, 
                out _selectorExpressionOriginal, 
                out _selectorExpression, 
                out _selectorContainsParametrizedObservableComputationsCalls, 
                ref _selectorExpressionInfo, 
                ref _selectorExpressionСallCount, 
                ref _selectorFunc, 
                ref _nestedComputings);

            _deferredQueuesCount = 3;
            _thisAsSourceCollectionChangeProcessor = this;
            _thisAsSourceItemChangeProcessor = this;
        }

        protected override void initializeFromSource()
        {
            int originalCount = _items.Count;

            if (_sourceInitialized)
            {
                Utils.disposeExpressionItemInfos(_itemInfos, _selectorExpressionСallCount, this);
                Utils.RemoveDownstreamConsumedComputing(_itemInfos, this);

                Utils.disposeSource(
                    _sourceScalar, 
                    _source,
                    out _itemInfos,
                    out _sourcePositions, 
                    _sourceAsList, 
                    handleSourceCollectionChanged);

                _sourceInitialized = false;
            }

            Utils.changeSource(ref _source, _sourceScalar, _downstreamConsumedComputings, _consumers, this, ref _sourceAsList, false);

            if (_source != null && _isActive)
            {
                Utils.initializeFromObservableCollectionWithChangeMarker(
                    _source, 
                    ref _sourceAsList, 
                    ref _rootSourceWrapper, 
                    ref _lastProcessedSourceChangeMarker);

                int count = _sourceAsList.Count;

                TSourceItem[] sourceCopy = new TSourceItem[count];
                _sourceAsList.CopyTo(sourceCopy, 0);

                _sourceAsList.CollectionChanged += handleSourceCollectionChanged;

                int sourceIndex;
                for (sourceIndex = 0; sourceIndex < count; sourceIndex++)
                {
                    TSourceItem sourceItem = sourceCopy[sourceIndex];
                    ItemInfo itemInfo = registerSourceItem(sourceItem, sourceIndex);

                    if (originalCount > sourceIndex)
                        _items[sourceIndex] = applySelector(itemInfo, sourceItem);
                    else
                        _items.Insert(sourceIndex, applySelector(itemInfo, sourceItem));
                }

                for (int index = originalCount - 1; index >= sourceIndex; index--)
                {
                    _items.RemoveAt(index);
                }

                _sourceInitialized = true;
            }
            else
            {
                _items.Clear();
            }

            reset();
        }

        protected override void initialize()
        {
            Utils.initializeSourceScalar(_sourceScalar, ref _source, scalarValueChangedHandler);
            Utils.initializeNestedComputings(_nestedComputings, this);
        }

        protected override void uninitialize()
        {
            Utils.uninitializeSourceScalar(_sourceScalar, scalarValueChangedHandler, ref _source);
            Utils.uninitializeNestedComputings(_nestedComputings, this);
        }

        protected void handleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!Utils.preHandleSourceCollectionChanged(
                    sender, 
                    e, 
                    _rootSourceWrapper, 
                    ref _lastProcessedSourceChangeMarker, 
                    _sourceAsList, 
                    ref _isConsistent,
                    this,
                    ref _handledEventSender,
                    ref _handledEventArgs,
                    ref _deferredProcessings,
                    1, _deferredQueuesCount, 
                    this)) return;
	
			_thisAsSourceCollectionChangeProcessor.processSourceCollectionChanged(sender,e);      
           
            Utils.postHandleChange(
                ref _handledEventSender,
                ref _handledEventArgs,
                _deferredProcessings,
                ref _isConsistent,
                this);
		}

        void ISourceCollectionChangeProcessor.processSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ItemInfo itemInfo;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    int newStartingIndex = e.NewStartingIndex;
                    TSourceItem addedItem = (TSourceItem) e.NewItems[0];
                    itemInfo = registerSourceItem(addedItem, newStartingIndex);
                    baseInsertItem(newStartingIndex, applySelector(itemInfo, addedItem));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    int oldStartingIndex = e.OldStartingIndex;
                    unregisterSourceItem(oldStartingIndex);
                    baseRemoveItem(oldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    int newStartingIndex1 = e.NewStartingIndex;
                    TSourceItem newItem = (TSourceItem) e.NewItems[0];
                    ItemInfo replacingItemInfo = _itemInfos[newStartingIndex1];
                    unregisterSourceItem(newStartingIndex1, true);
                    itemInfo = registerSourceItem(newItem, newStartingIndex1, replacingItemInfo);
                    baseSetItem(newStartingIndex1, applySelector(itemInfo, newItem));
                    break;
                case NotifyCollectionChangedAction.Move:
                    int oldStartingIndex2 = e.OldStartingIndex;
                    int newStartingIndex2 = e.NewStartingIndex;
                    if (oldStartingIndex2 != newStartingIndex2)
                    {
                        _sourcePositions.Move(oldStartingIndex2, newStartingIndex2);
                        baseMoveItem(oldStartingIndex2, newStartingIndex2);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    initializeFromSource();
                    break;
            }
        }

        internal override void addToUpstreamComputings(IComputingInternal computing)
        {
            (_source as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
            (_sourceScalar as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
            Utils.AddDownstreamConsumedComputing(_itemInfos, this);
        }


        internal override void removeFromUpstreamComputings(IComputingInternal computing)        
        {
            (_source as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
            (_sourceScalar as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
            Utils.RemoveDownstreamConsumedComputing(_itemInfos, this);
        }

        private void expressionWatcher_OnValueChanged(ExpressionWatcher expressionWatcher, object sender, EventArgs eventArgs)
		{
            Utils.ProcessSourceItemChange(
                expressionWatcher, 
                sender, 
                eventArgs, 
                _rootSourceWrapper, 
                _sourceAsList, 
                _lastProcessedSourceChangeMarker, 
                _thisAsSourceItemChangeProcessor,
                ref _isConsistent,
                ref _handledEventSender,
                ref _handledEventArgs,
                ref _deferredProcessings, 
                2, _deferredQueuesCount, this);
		}

        private ItemInfo registerSourceItem(TSourceItem sourceItem, int index, ItemInfo itemInfo = null)
		{
			itemInfo = itemInfo == null ? _sourcePositions.Insert(index) : _itemInfos[index];

			ExpressionWatcher watcher;

            Utils.getItemInfoContent(
                new object[]{sourceItem}, 
                out watcher, 
                out Func<TResultItem> predicateFunc, 
                out List<IComputingInternal> nestedComputings,
                _selectorExpression,
                out _selectorExpressionСallCount,
                this,
                _selectorContainsParametrizedObservableComputationsCalls,
                _selectorExpressionInfo);

            itemInfo.SelectorFunc = predicateFunc;
            itemInfo.NestedComputings = nestedComputings;
			watcher.ValueChanged = expressionWatcher_OnValueChanged;
			itemInfo.ExpressionWatcher = watcher;
			watcher._position = itemInfo;

			return itemInfo;
		}

        void ISourceItemChangeProcessor.ProcessSourceItemChange(ExpressionWatcher expressionWatcher)
        {
            if (expressionWatcher._disposed) return;
            int sourceIndex = expressionWatcher._position.Index;
            baseSetItem(sourceIndex, applySelector((ItemInfo)expressionWatcher._position, _sourceAsList[sourceIndex]));
        }

        private void unregisterSourceItem(int index, bool replacing = false)
        {
            Utils.disposeExpressionWatcher(_itemInfos[index].ExpressionWatcher, _itemInfos[index].NestedComputings, this,
                _selectorContainsParametrizedObservableComputationsCalls);

            if (!replacing) _sourcePositions.Remove(index);
        }

        private TResultItem applySelector(ItemInfo itemInfo, TSourceItem sourceItem)
		{
            TResultItem getValue() =>
                _selectorContainsParametrizedObservableComputationsCalls
                    ? itemInfo.SelectorFunc()
                    : _selectorFunc(sourceItem);

            if (Configuration.TrackComputingsExecutingUserCode)
			{
                var currentThread = Utils.startComputingExecutingUserCode(out var computing, out _userCodeIsCalledFrom, this);		
				TResultItem result = getValue();
                Utils.endComputingExecutingUserCode(computing, currentThread, out _userCodeIsCalledFrom);
				return result;
			}

			return getValue();
		}

		// ReSharper disable once InconsistentNaming
		internal void ValidateConsistency()
		{
			_sourcePositions.ValidateConsistency();
			IList<TSourceItem> source = _sourceScalar.getValue(_source, new ObservableCollection<TSourceItem>()) as IList<TSourceItem>;
			// ReSharper disable once PossibleNullReferenceException
			if (_itemInfos.Count != source.Count)
				throw new ObservableComputationsException(this, "Consistency violation: Selecting.1");
			Func<TSourceItem, TResultItem> selector = _selectorExpressionOriginal.Compile();

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (source != null)
			{
				if (_itemInfos.Count != source.Count)
					throw new ObservableComputationsException(this, "Consistency violation: Selecting.6");

				for (int sourceIndex = 0; sourceIndex < source.Count; sourceIndex++)
				{
					TSourceItem sourceItem = source[sourceIndex];
					ItemInfo itemInfo = _itemInfos[sourceIndex];
					
					if (!EqualityComparer<TResultItem>.Default.Equals(this[sourceIndex], selector(sourceItem)))
						throw new ObservableComputationsException(this, "Consistency violation: Selecting.2");

					if (_itemInfos[sourceIndex].Index != sourceIndex)
						throw new ObservableComputationsException(this, "Consistency violation: Selecting.3");
					if (itemInfo.ExpressionWatcher._position != _itemInfos[sourceIndex])
						throw new ObservableComputationsException(this, "Consistency violation: Selecting.4");

					if (!_itemInfos.Contains((ItemInfo) itemInfo.ExpressionWatcher._position))
						throw new ObservableComputationsException(this, "Consistency violation: Selecting.5");

					if (itemInfo.ExpressionWatcher._position.Index != sourceIndex)
						throw new ObservableComputationsException(this, "Consistency violation: Selecting.7");

				}
			}
		}
    }
}
