﻿using System.Collections.ObjectModel;
using NUnit.Framework;

namespace ObservableComputations.Test
{
	[TestFixture]
	public class CastingTests
	{
        Consumer consumer = new Consumer();

		class BaseItem{}
		class DerivedItem : BaseItem{}

		[Test]
		public void Casting_Initialization_01()
		{
			ObservableCollection<DerivedItem> items = new ObservableCollection<DerivedItem>();

			Casting<BaseItem> casting = items.Casting<BaseItem>().IsNeededFor(consumer);
			casting.ValidateConsistency();			
			consumer.Dispose();
		}

		[Test, Combinatorial]
		public void Casting_Set(
			[Range(-2, 0, 1)] int item1,
			[Range(-2, 0, 1)] int item2,
			[Range(0, 1, 1)] int index,
			[Range(-1, 0, 1)] int newItem)
		{
			ObservableCollection<DerivedItem> items = new ObservableCollection<DerivedItem>();
			if (item1 >= -1) items.Add(item1 == 0 ? new DerivedItem() : null);
			if (item2 >= -1) items.Add(item2 == 0 ? new DerivedItem() : null);

			if (index >= items.Count) return;

			Casting<BaseItem> casting = items.Casting<BaseItem>().IsNeededFor(consumer);
			casting.ValidateConsistency();
			if (index < items.Count) items[index] = newItem == 0 ? new DerivedItem() : null;
			casting.ValidateConsistency();			
			consumer.Dispose();
		}

		[Test, Combinatorial]
		public void Casting_Remove(
			[Range(-2, 0, 1)] int item1,
			[Range(-2, 0, 1)] int item2,
			[Range(0, 1, 1)] int index)
		{
			ObservableCollection<DerivedItem> items = new ObservableCollection<DerivedItem>();
			if (item1 >= -1) items.Add(item1 == 0 ? new DerivedItem() : null);
			if (item2 >= -1) items.Add(item2 == 0 ? new DerivedItem() : null);

			if (index >= items.Count) return;

			Casting<BaseItem> casting = items.Casting<BaseItem>().IsNeededFor(consumer);
			casting.ValidateConsistency();
			items.RemoveAt(index);
			casting.ValidateConsistency();			
			consumer.Dispose();
		}

		[Test, Combinatorial]
		public void Casting_Insert(
			[Range(-2, 0, 1)] int item1,
			[Range(-2, 0, 1)] int item2,
			[Range(0, 2, 1)] int index,
			[Range(-1, 0, 1)] int newItem)
		{
			ObservableCollection<DerivedItem> items = new ObservableCollection<DerivedItem>();
			if (item1 >= -1) items.Add(item1 == 0 ? new DerivedItem() : null);
			if (item2 >= -1) items.Add(item2 == 0 ? new DerivedItem() : null);

			if (index > items.Count) return;

			Casting<BaseItem> casting = items.Casting<BaseItem>().IsNeededFor(consumer);
			casting.ValidateConsistency();
			items.Insert(index, newItem == 0 ? new DerivedItem() : null);
			casting.ValidateConsistency();			
			consumer.Dispose();
		}

		[Test, Combinatorial]
		public void Casting_Move(
			[Range(-2, 0, 1)] int item1,
			[Range(-2, 0, 1)] int item2,
			[Range(0, 4, 1)] int oldIndex,
			[Range(0, 4, 1)] int newIndex)
		{
			ObservableCollection<DerivedItem> items = new ObservableCollection<DerivedItem>();
			if (item1 >= -1) items.Add(item1 == 0 ? new DerivedItem() : null);
			if (item2 >= -1) items.Add(item2 == 0 ? new DerivedItem() : null);

			if (oldIndex >= items.Count || newIndex >= items.Count) return;

			Casting<BaseItem> casting = items.Casting<BaseItem>().IsNeededFor(consumer);
			casting.ValidateConsistency();
			items.Move(oldIndex, newIndex);
			casting.ValidateConsistency();			
			consumer.Dispose();
		}	
	}
}