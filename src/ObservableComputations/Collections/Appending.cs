﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ObservableComputations;

namespace ObservableComputations
{
	public class Appending<TSourceItem> : Concatenating<TSourceItem>, IHasSources
	{
		public IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalar;

		// ReSharper disable once MemberCanBePrivate.Global\
		// ReSharper disable once ConvertToAutoProperty
		public IReadScalar<TSourceItem> ItemScalar => _itemScalar;

		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once ConvertToAutoProperty
		public INotifyCollectionChanged Source => _source;

		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once ConvertToAutoProperty
		public TSourceItem Item => _item;

		public new ReadOnlyCollection<INotifyCollectionChanged> SourcesCollection => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceScalarsCollection => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		private readonly IReadScalar<INotifyCollectionChanged> _sourceScalar;
		private readonly IReadScalar<TSourceItem> _itemScalar;
		private readonly INotifyCollectionChanged _source;
		private readonly TSourceItem _item;

		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once ConvertToAutoProperty

		[ObservableComputationsCall]
		public Appending(
			INotifyCollectionChanged source,
			TSourceItem item) : base(getSources(source, item))
		{
			_source = source;
			_item = item;
		}

		[ObservableComputationsCall]
		public Appending(
			INotifyCollectionChanged source,
			IReadScalar<TSourceItem> itemScalar) : base(getSources(source, itemScalar))
		{
			_source = source;
			_itemScalar = itemScalar;
		}

		[ObservableComputationsCall]
		public Appending(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			TSourceItem item) : base(getSources(sourceScalar, item))
		{
			_sourceScalar = sourceScalar;
			_item = item;
		}

		[ObservableComputationsCall]
		public Appending(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			IReadScalar<TSourceItem> itemScalar) : base(getSources(sourceScalar, itemScalar))
		{
			_sourceScalar = sourceScalar;
			_itemScalar = itemScalar;
		}

		private static ReadOnlyObservableCollection<object> getSources(
			INotifyCollectionChanged source,
			TSourceItem item) =>
			new ReadOnlyObservableCollection<object>(
				new object[]{source, new ReadOnlyObservableCollection<TSourceItem>(item)});

		private static INotifyCollectionChanged getSources(
			INotifyCollectionChanged source,
			IReadScalar<TSourceItem> itemScalar) =>
				new ReadOnlyObservableCollection<object>(new object[]{source, new Computing<ReadOnlyObservableCollection<TSourceItem>>(() => new ReadOnlyObservableCollection<TSourceItem>(itemScalar.Value))});

		private static INotifyCollectionChanged getSources(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			TSourceItem item) =>
				new ReadOnlyObservableCollection<object>(new object[]{sourceScalar, new ReadOnlyObservableCollection<TSourceItem>(item)});

		private static INotifyCollectionChanged getSources(
			IReadScalar<INotifyCollectionChanged> sourceScalar,
			IReadScalar<TSourceItem> itemScalar) =>
				new ReadOnlyObservableCollection<object>(new object[]{sourceScalar, new Computing<ReadOnlyObservableCollection<TSourceItem>>(() => new ReadOnlyObservableCollection<TSourceItem>(itemScalar.Value))});

		public new void ValidateConsistency()
		{
			IList<TSourceItem> source = _sourceScalar.getValue(_source, new ObservableCollection<TSourceItem>()) as IList<TSourceItem>;

			TSourceItem item = _itemScalar.getValue(Item);

			List<TSourceItem> result = new List<TSourceItem>(source);
			result.Add(item);

			if (!this.SequenceEqual(result))
				throw new ObservableComputationsException(this, "Consistency violation: Appending.1");
		}
	}
}
