﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableComputations
{
	public class CollectionDispatching<TSourceItem> : CollectionComputing<TSourceItem>, IHasSourceCollections
	{
		public INotifyCollectionChanged Source => _source;
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;
		public ICollectionDestinationDispatcher CollectionDestinationDispatcher => _collectionDestinationDispatcher;
		public IDispatcher DestinationDispatcher => _destinationDispatcher;
		public IDispatcher SourceDispatcher => _sourceDispatcher;

		public ReadOnlyCollection<INotifyCollectionChanged> SourceCollections => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceCollectionScalars => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});


		private INotifyCollectionChanged _source;
		private IList<TSourceItem> _sourceAsList;
		private IReadScalar<INotifyCollectionChanged> _sourceScalar;
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private PropertyChangedEventHandler _sourceScalarPropertyChangedEventHandler;
		private WeakPropertyChangedEventHandler _sourceScalarWeakPropertyChangedEventHandler;

		private NotifyCollectionChangedEventHandler _sourceNotifyCollectionChangedEventHandler;
		private WeakNotifyCollectionChangedEventHandler _sourceWeakNotifyCollectionChangedEventHandler;

		private PropertyChangedEventHandler _sourcePropertyChangedEventHandler;
		private WeakPropertyChangedEventHandler _sourceWeakPropertyChangedEventHandler;
		private bool _indexerPropertyChangedEventRaised;
		private INotifyPropertyChanged _sourceAsINotifyPropertyChanged;

		private readonly IDispatcher _destinationDispatcher;
		private ICollectionDestinationDispatcher _collectionDestinationDispatcher;
		private IDispatcher _sourceDispatcher;

		private IHasChangeMarker _sourceAsIHasChangeMarker;
		private bool _lastProcessedSourceChangeMarker;


		[ObservableComputationsCall]
		public CollectionDispatching(
			INotifyCollectionChanged source,
			IDispatcher destinationDispatcher,
			IDispatcher sourceDispatcher = null)
		{
			_destinationDispatcher = destinationDispatcher;
			initialize(source, sourceDispatcher);
		}

		[ObservableComputationsCall]
		public CollectionDispatching(
			INotifyCollectionChanged source,
			ICollectionDestinationDispatcher destinationDispatcher,
			IDispatcher sourceDispatcher = null)
		{
			_collectionDestinationDispatcher = destinationDispatcher;
			initialize(source, sourceDispatcher);
		}

		private void initialize(INotifyCollectionChanged source, IDispatcher sourceDispatcher)
		{
			_sourceDispatcher = sourceDispatcher;
			_source = source;

			if (_sourceDispatcher != null)
				_sourceDispatcher.Invoke(initializeFromSource, this);
			else
				initializeFromSource();
		}

		[ObservableComputationsCall]
		public CollectionDispatching(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			IDispatcher destinationDispatcher,
			IDispatcher sourceDispatcher = null)
		{
			_destinationDispatcher = destinationDispatcher;
			initialize(sourceScalar, sourceDispatcher);
		}

		[ObservableComputationsCall]
		public CollectionDispatching(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			ICollectionDestinationDispatcher destinationDispatcher,
			IDispatcher sourceDispatcher = null)
		{
			_collectionDestinationDispatcher = destinationDispatcher;
			initialize(sourceScalar, sourceDispatcher);
		}

		private void initialize(IReadScalar<INotifyCollectionChanged> sourceScalar, IDispatcher sourceDispatcher)
		{
			_sourceDispatcher = sourceDispatcher;
			_sourceScalar = sourceScalar;
			_sourceScalarPropertyChangedEventHandler = handleSourceScalarValueChanged;
			_sourceScalarWeakPropertyChangedEventHandler =
				new WeakPropertyChangedEventHandler(_sourceScalarPropertyChangedEventHandler);
			_sourceScalar.PropertyChanged += _sourceScalarWeakPropertyChangedEventHandler.Handle;

			if (_sourceDispatcher != null)
				_sourceDispatcher.Invoke(initializeFromSource, this);
			else
				initializeFromSource(null, null);
		}


		private void handleSourceScalarValueChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IReadScalar<object>.Value)) return;

			_processingEventSender = sender;
			_processingEventArgs = e;

			initializeFromSource(null, e);

			_processingEventSender = null;
			_processingEventArgs = null;
		}

		private void initializeFromSource()
		{
			initializeFromSource(null, null);
		}

		private void initializeFromSource(
			NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs,
			PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (_sourceNotifyCollectionChangedEventHandler != null)
			{
				if (_destinationDispatcher != null) _destinationDispatcher.Invoke(baseClearItems, this);
				else _collectionDestinationDispatcher.Invoke(baseClearItems, this, notifyCollectionChangedEventArgs, propertyChangedEventArgs, true);
				
				_source.CollectionChanged -= _sourceWeakNotifyCollectionChangedEventHandler.Handle;
				_sourceNotifyCollectionChangedEventHandler = null;
				_sourceWeakNotifyCollectionChangedEventHandler = null;
			}

			if (_sourceAsINotifyPropertyChanged != null)
			{
				_sourceAsINotifyPropertyChanged.PropertyChanged -=
					_sourceWeakPropertyChangedEventHandler.Handle;

				_sourceAsINotifyPropertyChanged = null;
				_sourcePropertyChangedEventHandler = null;
				_sourceWeakPropertyChangedEventHandler = null;
			}

			if (_sourceScalar != null) _source = _sourceScalar.Value;
			_sourceAsList = _source as IList<TSourceItem>;

			if (_sourceAsList != null)
			{
				_sourceAsIHasChangeMarker = _sourceAsList as IHasChangeMarker;

				if (_sourceAsIHasChangeMarker != null)
				{
					_lastProcessedSourceChangeMarker = _sourceAsIHasChangeMarker.ChangeMarker;
				}
				else
				{
					_sourceAsINotifyPropertyChanged = (INotifyPropertyChanged) _sourceAsList;

					_sourcePropertyChangedEventHandler = (sender1, args1) =>
					{
						if (args1.PropertyName == "Item[]")
							_indexerPropertyChangedEventRaised =
								true; // ObservableCollection raises this before CollectionChanged event raising
					};

					_sourceWeakPropertyChangedEventHandler =
						new WeakPropertyChangedEventHandler(_sourcePropertyChangedEventHandler);

					_sourceAsINotifyPropertyChanged.PropertyChanged += _sourceWeakPropertyChangedEventHandler.Handle;
				}

				int index = 0;
				int count = _sourceAsList.Count;
				for (var sourceIndex = 0; sourceIndex < count; sourceIndex++)
				{
					TSourceItem sourceItem = _sourceAsList[sourceIndex];
					int indexCopy = index++;

					void insertItem() => baseInsertItem(indexCopy, sourceItem);

					if (_destinationDispatcher != null) _destinationDispatcher.Invoke(insertItem, this);
					else _collectionDestinationDispatcher.Invoke(insertItem, this, notifyCollectionChangedEventArgs, propertyChangedEventArgs, true);

				}

				_sourceNotifyCollectionChangedEventHandler = handleSourceCollectionChanged;
				_sourceWeakNotifyCollectionChangedEventHandler =
					new WeakNotifyCollectionChangedEventHandler(_sourceNotifyCollectionChangedEventHandler);
				_source.CollectionChanged += _sourceWeakNotifyCollectionChangedEventHandler.Handle;
			}
		}

		private void handleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			_processingEventSender = sender;
			_processingEventArgs = e;

			if (_indexerPropertyChangedEventRaised || _lastProcessedSourceChangeMarker != _sourceAsIHasChangeMarker.ChangeMarker)
			{
				_lastProcessedSourceChangeMarker = !_lastProcessedSourceChangeMarker;
				_indexerPropertyChangedEventRaised = false;

				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						//if (e.NewItems.Count > 1) throw new ObservableComputationsException("Adding of multiple items is not supported");
						void add()
						{
							baseInsertItem(e.NewStartingIndex, (TSourceItem) e.NewItems[0]);
						}

						if (_destinationDispatcher != null) _destinationDispatcher.Invoke(add, this);
						else _collectionDestinationDispatcher.Invoke(add, this, false, e, null, NotifyCollectionChangedAction.Add, null, 0);

						break;
					case NotifyCollectionChangedAction.Remove:
						// (e.OldItems.Count > 1) throw new ObservableComputationsException("Removing of multiple items is not supported");
						
						
						void remove()
						{
							baseRemoveItem(e.OldStartingIndex);
						}

						if (_destinationDispatcher != null) _destinationDispatcher.Invoke(remove, this);
						else _collectionDestinationDispatcher.Invoke(remove, this, false, e, null, NotifyCollectionChangedAction.Remove, null, 0);	

						break;
					case NotifyCollectionChangedAction.Replace:
						//if (e.NewItems.Count > 1) throw new ObservableComputationsException("Replacing of multiple items is not supported");
						void replace()
						{
							baseSetItem(e.NewStartingIndex, (TSourceItem) e.NewItems[0]);
						}

						if (_destinationDispatcher != null) _destinationDispatcher.Invoke(replace, this);
						else _collectionDestinationDispatcher.Invoke(replace, this, false, e, null, NotifyCollectionChangedAction.Replace, null, 0);	
						break;
					case NotifyCollectionChangedAction.Move:
						int oldStartingIndex1 = e.OldStartingIndex;
						int newStartingIndex1 = e.NewStartingIndex;
						if (oldStartingIndex1 == newStartingIndex1) return;

						void move()
						{
							baseMoveItem(oldStartingIndex1, newStartingIndex1);
						}

						if (_destinationDispatcher != null) _destinationDispatcher.Invoke(move, this);
						else _collectionDestinationDispatcher.Invoke(move, this, false, e, null, NotifyCollectionChangedAction.Move, null, 0);	

						break;
					case NotifyCollectionChangedAction.Reset:
						initializeFromSource(e, null);
						break;
				}
			}

			_processingEventSender = null;
			_processingEventArgs = null;
		}

		~CollectionDispatching()
		{
			if (_sourceWeakNotifyCollectionChangedEventHandler != null)
			{
				_source.CollectionChanged -= _sourceWeakNotifyCollectionChangedEventHandler.Handle;			
			}

			if (_sourceScalarWeakPropertyChangedEventHandler != null)
			{
				_sourceScalar.PropertyChanged -= _sourceScalarWeakPropertyChangedEventHandler.Handle;			
			}

		}
	}
}
