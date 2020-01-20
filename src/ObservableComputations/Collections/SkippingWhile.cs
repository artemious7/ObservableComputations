﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using ObservableComputations;
using ObservableComputations.ExtentionMethods;

namespace ObservableComputations
{
	public class SkippingWhile<TSourceItem> : Selecting<ZipPair<int, TSourceItem>, TSourceItem>, IHasSources
	{
		public new IReadScalar<INotifyCollectionChanged> SourceScalar => _sourceScalarSkippingWhile;

		// ReSharper disable once MemberCanBePrivate.Global
		public new INotifyCollectionChanged Source => _sourceSkippingWhile;

		// ReSharper disable once MemberCanBePrivate.Global
		public Expression<Func<TSourceItem, int, bool>> PredicateExpression => _predicateExpression;

		public new ReadOnlyCollection<INotifyCollectionChanged> SourcesCollection => new ReadOnlyCollection<INotifyCollectionChanged>(new []{Source});
		public new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>> SourceScalarsCollection => new ReadOnlyCollection<IReadScalar<INotifyCollectionChanged>>(new []{SourceScalar});

		private readonly IReadScalar<INotifyCollectionChanged> _sourceScalarSkippingWhile;
		private readonly INotifyCollectionChanged _sourceSkippingWhile;
		private readonly Expression<Func<TSourceItem, int, bool>> _predicateExpression;

		// ReSharper disable once MemberCanBePrivate.Global

		[ObservableComputationsCall]
		public SkippingWhile(			
			IReadScalar<INotifyCollectionChanged> sourceScalar, 
			Expression<Func<TSourceItem, int, bool>> predicateExpression,
			int capacity = 0)
			: base(
				getSource(sourceScalar, predicateExpression, capacity),
				zipPair => zipPair.ItemRight)
		{
			_sourceScalarSkippingWhile = sourceScalar;
			_predicateExpression = predicateExpression;
		}

		[ObservableComputationsCall]
		public SkippingWhile(			
			INotifyCollectionChanged source, 
			Expression<Func<TSourceItem, int, bool>> predicateExpression,
			int capacity = 0)
			: base(
				getSource(source, predicateExpression, capacity),
				zipPair => zipPair.ItemRight)
		{
			_sourceSkippingWhile = source;
			_predicateExpression = predicateExpression;
		}

		[ObservableComputationsCall]
		public SkippingWhile(
			IReadScalar<INotifyCollectionChanged> sourceScalar, 
			Expression<Func<TSourceItem, bool>> predicateExpression,
			int capacity = 0) : this(sourceScalar, predicateExpression.getIndexedPredicate(), capacity)
		{
		}

		[ObservableComputationsCall]
		public SkippingWhile(
			INotifyCollectionChanged source, 
			Expression<Func<TSourceItem, bool>> predicateExpression,
			int capacity = 0) : this(source, predicateExpression.getIndexedPredicate(), capacity)
		{
		}

		private static INotifyCollectionChanged getSource(
			IReadScalar<INotifyCollectionChanged> sourceScalar, 
			Expression<Func<TSourceItem, int, bool>> predicateExpression,
			int capacity)
		{
			Expression<Func<ZipPair<int, TSourceItem>, bool>> zipPairNotPredicateExpression = getZipPairNotPredicateExpression(predicateExpression);

			Computing<int> countComputing = Expr.Is(() => sourceScalar.Value != null ? ((IList) sourceScalar.Value).Count : 0).Computing();

			Zipping<int, TSourceItem> zipping = countComputing.SequenceComputing()
				.Zipping<int, TSourceItem>(sourceScalar);

			return getFiltering(zipping, zipPairNotPredicateExpression, countComputing, capacity);

			//return () => (INotifyCollectionChanged)Expr.Is(() => (INotifyCollectionChanged)getSource.Computing().Using(sc =>
			//			Expr.Is(() => ((IList) sc.Value).Count).SequenceComputing()
			//				.Zipping<int, TSourceItem>(() => sc.Value)).Value).Computing().Using(zipping => zipping.Value.Filtering<ZipPair<int, TSourceItem>>(zp => zp.ItemLeft < zipping.Value.Filtering(zipPairNotPredicateExpression).Selecting(zp1 => zp1.ItemLeft).Minimazing(() => (((IList)zipping.Value).Count)).Value)).Value;
		}

		private static Filtering<ZipPair<int, TSourceItem>> getFiltering(
			Zipping<int, TSourceItem> zipping, Expression<Func<ZipPair<int, TSourceItem>, bool>> zipPairNotPredicateExpression, 
			Computing<int> countComputing,
			int capacity)
		{
			return zipping.Filtering(zp => zp.ItemLeft >= zipping.Filtering(zipPairNotPredicateExpression, capacity).Selecting(zp1 => zp1.ItemLeft).Minimazing<int>(countComputing).Value, capacity);
		}

		private static INotifyCollectionChanged getSource(
			INotifyCollectionChanged source, 
			Expression<Func<TSourceItem, int, bool>> predicateExpression,
			int capacity)
		{
			Expression<Func<ZipPair<int, TSourceItem>, bool>> zipPairNotPredicateExpression = getZipPairNotPredicateExpression(predicateExpression);

			Computing<int> countComputing = Expr.Is(() => ((IList)source).Count).Computing();

			Zipping<int, TSourceItem> zipping = countComputing.SequenceComputing()
				.Zipping<int, TSourceItem>(source);

			return getFiltering(zipping, zipPairNotPredicateExpression, countComputing, capacity);
		}

		private static Expression<Func<ZipPair<int, TSourceItem>, bool>> getZipPairNotPredicateExpression(Expression<Func<TSourceItem, int, bool>> predicateExpression)
		{
			ParameterExpression zipPairParameterExpression
				= Expression.Parameter(typeof(ZipPair<int, TSourceItem>), "zipPair");
			Expression zipPairIndexExpression
				= Expression.PropertyOrField(
					zipPairParameterExpression,
					nameof(ZipPair<int, TSourceItem>.ItemLeft));
			Expression zipPairItemExpression
				= Expression.PropertyOrField(
					zipPairParameterExpression,
					nameof(ZipPair<int, TSourceItem>.ItemRight));
			ReplaceParameterVisitor replaceParameterVisitor
				= new ReplaceParameterVisitor(
					predicateExpression.Parameters,
					new[] {zipPairItemExpression, zipPairIndexExpression});
			Expression<Func<ZipPair<int, TSourceItem>, bool>> zipPairNotPredicateExpression
				= Expression.Lambda<Func<ZipPair<int, TSourceItem>, bool>>(
					// ReSharper disable once AssignNullToNotNullAttribute
					Expression.Not(replaceParameterVisitor.Visit(predicateExpression.Body)),
					zipPairParameterExpression);
			return zipPairNotPredicateExpression;
		}

		public new void ValidateConsistency()
		{
			IList<TSourceItem> source = _sourceScalarSkippingWhile.getValue(_sourceSkippingWhile, new ObservableCollection<TSourceItem>()) as IList<TSourceItem>;
			Func<TSourceItem, int, bool> predicate = _predicateExpression.Compile();

			// ReSharper disable once AssignNullToNotNullAttribute
			if (!this.SequenceEqual(source.SkipWhile(predicate)))
			{
				throw new ObservableComputationsException(this, "Consistency violation: SkippingWhile.1");
			}
		}
	}
}
