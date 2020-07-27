﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ObservableComputations
{
	public class CollectionPausing<TSourceItem> : CollectionComputing<TSourceItem>, IHasSourceCollections, ISourceIndexerPropertyTracker
	{
		public INotifyCollectionChanged Source => _source;
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;
		public IReadScalar<bool> IsPausedScalar => _isPausedScalar;
		public bool Resuming => _resuming;

		public ReadOnlyCollection<INotifyCollectionChanged> SourceCollections => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceCollectionScalars => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		public bool IsPaused
		{
			get => _isPaused;
			set
			{
				if (_isPausedScalar != null) throw new ObservableComputationsException("Modifying of IsPaused property is controlled by IsPausedScalar");
				_resuming = _isPaused != value && value;
				_isPaused = value;
				OnPropertyChanged(Utils.PausedPropertyChangedEventArgs);

				if (_resuming)
				{
					resume();
				}
			}
		}

		private void resume()
		{
			DefferedCollectionAction<TSourceItem> defferedCollectionAction;
			int count = _defferedCollectionActions.Count;

			_isConsistent = false;

			for (int i = 0; i < count; i++)
			{
				defferedCollectionAction = _defferedCollectionActions.Dequeue();
				if (defferedCollectionAction.NotifyCollectionChangedEventAgs != null)
					handleSourceCollectionChanged(defferedCollectionAction.EventSender,
						defferedCollectionAction.NotifyCollectionChangedEventAgs);
				else if (defferedCollectionAction.Clear)
				{
					_items.Clear();
				}
				else if (defferedCollectionAction.Reset)
				{
					_handledEventSender = defferedCollectionAction.EventSender;
					_handledEventArgs = defferedCollectionAction.EventArgs;

					reset();

					_handledEventSender = null;
					_handledEventArgs = null;
				}
				else if (defferedCollectionAction.NewItems != null)
				{
					int originalCount = _items.Count;
					TSourceItem[] newItems = defferedCollectionAction.NewItems;
					int count1 = newItems.Length;
					int sourceIndex;
					for (sourceIndex = 0; sourceIndex < count1; sourceIndex++)
					{
						if (originalCount > sourceIndex)
							_items[sourceIndex] = newItems[sourceIndex];
						else
							_items.Insert(sourceIndex, newItems[sourceIndex]);
					}

					for (int index = originalCount - 1; index >= sourceIndex; index--)
					{
						_items.RemoveAt(index);
					}
				}
			}

			_isConsistent = true;
			raiseConsistencyRestored();

			_resuming = false;
		}

		private INotifyCollectionChanged _source;
		private IList<TSourceItem> _sourceAsList;
		private IReadScalar<INotifyCollectionChanged> _sourceScalar;

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private PropertyChangedEventHandler _sourceScalarPropertyChangedEventHandler;

		private IReadScalar<bool> _isPausedScalar;

		private NotifyCollectionChangedEventHandler _sourceNotifyCollectionChangedEventHandler;

		private bool _indexerPropertyChangedEventRaised;
		private INotifyPropertyChanged _sourceAsINotifyPropertyChanged;

		private IHasChangeMarker _sourceAsIHasChangeMarker;
		private bool _lastProcessedSourceChangeMarker;
		private bool _isPaused;
		private bool _resuming;

		Queue<DefferedCollectionAction<TSourceItem>> _defferedCollectionActions = new Queue<DefferedCollectionAction<TSourceItem>>();

		[ObservableComputationsCall]
		public CollectionPausing(
			INotifyCollectionChanged source,
			bool initialIsPaused = false)
		{
			_isPaused = initialIsPaused;
			_source = source;
		}


		[ObservableComputationsCall]
		public CollectionPausing(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			bool initialIsPaused = false)
		{
			_isPaused = initialIsPaused;
			_sourceScalar = sourceScalar;
		}

		[ObservableComputationsCall]
		public CollectionPausing(
			INotifyCollectionChanged source,
			IReadScalar<bool> isPausedScalar)
		{
			_isPausedScalar = isPausedScalar;
			_source = source;
		}


		[ObservableComputationsCall]
		public CollectionPausing(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			IReadScalar<bool> isPausedScalar)
		{
			_isPausedScalar = isPausedScalar;
			_sourceScalar = sourceScalar;
		}

        private void initializeIsPausedScalar()
        {
            if (_isPausedScalar != null)
            {
                _isPausedScalar.PropertyChanged += handleIsPausedScalarValueChanged;
                _isPaused = _isPausedScalar.Value;
            }
        }

		private void handleIsPausedScalarValueChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IReadScalar<object>.Value)) return;

			checkConsistent(sender, e);

			_handledEventSender = sender;
			_handledEventArgs = e;

			bool newValue = _isPausedScalar.Value;
			_resuming = _isPaused != newValue && newValue;
			_isPaused = newValue;

			if (_resuming) resume();

			_handledEventSender = null;
			_handledEventArgs = null;
		}

        protected override void initializeFromSource()
		{
			int originalCount = _items.Count;

			if (_sourceNotifyCollectionChangedEventHandler != null)
			{		
                _source.CollectionChanged -= _sourceNotifyCollectionChangedEventHandler;
                _sourceNotifyCollectionChangedEventHandler = null;
			}

			if (_sourceAsINotifyPropertyChanged != null)
			{
                _sourceAsINotifyPropertyChanged.PropertyChanged -=
                    ((ISourceIndexerPropertyTracker) this).HandleSourcePropertyChanged;

                _sourceAsINotifyPropertyChanged = null;
			}

            Utils.changeSource(ref _source, _sourceScalar, _downstreamConsumedComputings, _consumers, this,
                ref _sourceAsList, _source as IList<TSourceItem>);

			if (_sourceAsList != null && _isActive)
			{
                Utils.initializeFromHasChangeMarker(
                    ref _sourceAsIHasChangeMarker, 
                    _sourceAsList, 
                    ref _lastProcessedSourceChangeMarker, 
                    ref _sourceAsINotifyPropertyChanged,
                    this);

				int sourceIndex = 0;

				if (_isPaused)
					_defferedCollectionActions.Enqueue(new DefferedCollectionAction<TSourceItem>(_sourceAsList.ToArray()));
				else
				{
					int count = _sourceAsList.Count;
					for (; sourceIndex < count; sourceIndex++)
					{
						if (originalCount > sourceIndex)
							_items[sourceIndex] = _sourceAsList[sourceIndex];
						else
							_items.Insert(sourceIndex, _sourceAsList[sourceIndex]);
					}					
				}

				if (!_isPaused)
				{
					for (int index = originalCount - 1; index >= sourceIndex; index--)
					{
						_items.RemoveAt(index);
					}
				}

                _sourceNotifyCollectionChangedEventHandler = handleSourceCollectionChanged;
                _source.CollectionChanged += _sourceNotifyCollectionChangedEventHandler;
			}			
			else 
			{
				if (_isPaused)
					_defferedCollectionActions.Enqueue(new DefferedCollectionAction<TSourceItem>(true));
				else
					_items.Clear();
			}

			if (_isPaused)
				_defferedCollectionActions.Enqueue(new DefferedCollectionAction<TSourceItem>(true, _handledEventSender, _handledEventArgs));
			else
				reset();
		}

		private void handleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
            if (!Utils.preHandleSourceCollectionChanged(
                sender, 
                e, 
                _isConsistent, 
                this, 
                ref _indexerPropertyChangedEventRaised, 
                ref _lastProcessedSourceChangeMarker, 
                _sourceAsIHasChangeMarker, 
                ref _handledEventSender, 
                ref _handledEventArgs)) return;

			if (!_resuming && !_isPaused)
			{
				_isConsistent = false;
			}

            if (_isPaused && e.Action != NotifyCollectionChangedAction.Reset)
            {
                _defferedCollectionActions.Enqueue(new DefferedCollectionAction<TSourceItem>(sender, e));
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //if (e.NewItems.Count > 1) throw new ObservableComputationsException("Adding of multiple items is not supported");
                    baseInsertItem(e.NewStartingIndex, (TSourceItem) e.NewItems[0]);

                    break;
                case NotifyCollectionChangedAction.Remove:
                    // (e.OldItems.Count > 1) throw new ObservableComputationsException("Removing of multiple items is not supported");
                    baseRemoveItem(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    //if (e.NewItems.Count > 1) throw new ObservableComputationsException("Replacing of multiple items is not supported");
                    baseSetItem(e.NewStartingIndex, (TSourceItem) e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Move:
                    int oldStartingIndex1 = e.OldStartingIndex;
                    int newStartingIndex1 = e.NewStartingIndex;
                    if (oldStartingIndex1 != newStartingIndex1)
                        baseMoveItem(oldStartingIndex1, newStartingIndex1);

                    break;
                case NotifyCollectionChangedAction.Reset:
                    initializeFromSource();
                    break;
            }

            if (!_resuming && !_isPaused)
			{		
				_isConsistent = true;
				raiseConsistencyRestored();
			}

            Utils.postHandleSourceCollectionChanged(
                ref _handledEventSender,
                ref _handledEventArgs);
		}

        internal override void addToUpstreamComputings(IComputingInternal computing)
        {
            (_source as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
        }

        internal override void removeFromUpstreamComputings(IComputingInternal computing)        
        {
            (_source as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
        }

        protected override void initialize()
        {     
            Utils.initializeSourceScalar(_sourceScalar, ref _sourceScalarPropertyChangedEventHandler, ref _source, getScalarValueChangedHandler());
            initializeIsPausedScalar();
        }

        protected override void uninitialize()
        {
            Utils.uninitializeSourceScalar(_sourceScalar, _sourceScalarPropertyChangedEventHandler);
            if (_isPausedScalar != null) _isPausedScalar.PropertyChanged -= handleIsPausedScalarValueChanged;
        }

        #region Implementation of ISourceIndexerPropertyTracker

        void ISourceIndexerPropertyTracker.HandleSourcePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _indexerPropertyChangedEventRaised = true;
        }

        #endregion
	}

	internal struct DefferedCollectionAction<TSourceItem>
	{
		public object EventSender;
		public NotifyCollectionChangedEventArgs NotifyCollectionChangedEventAgs;
		public EventArgs EventArgs;
		public TSourceItem NewItem;
		public int NewItemIndex;
		public TSourceItem[] NewItems;
		public bool Clear;
		public bool Reset;

		public DefferedCollectionAction(object eventSender, NotifyCollectionChangedEventArgs eventAgs) : this()
		{
			EventSender = eventSender;
			NotifyCollectionChangedEventAgs = eventAgs;
		}

		public DefferedCollectionAction(object eventSender, EventArgs eventArgs, TSourceItem newItem, int newItemIndex) : this()
		{
			EventSender = eventSender;
			EventArgs = eventArgs;
			NewItem = newItem;
			NewItemIndex = -newItemIndex;
		}

		public DefferedCollectionAction(TSourceItem[] newItems) : this()
		{
			NewItems = newItems;
		}

		public DefferedCollectionAction(bool clear) : this()
		{
			Clear = clear;
		}

		public DefferedCollectionAction(bool reset, object eventSender, EventArgs eventArgs) : this()
		{
			Reset = reset;
			EventSender = eventSender;
			EventArgs = eventArgs;
		}
	}
}
