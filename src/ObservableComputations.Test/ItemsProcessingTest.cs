﻿using System.Collections.ObjectModel;
using NUnit.Framework;

namespace ObservableComputations.Test
{
	[TestFixture]
	public class ItemsProcessingTest
	{
		public class Item
		{
			public bool ProcessedAsNew;
			public bool ProcessedAsOld;
			public object Token = new object();
		}

		private static ItemsProcessing<Item, object> getItemsProcessing(ObservableCollection<Item> items)
		{
			return items.ItemsProcessing(
				(item, current) =>
				{
					item.ProcessedAsNew = true;
					return item.Token;
				},
				(item, current, returnValue) =>
				{
					item.ProcessedAsOld = true;
					Assert.AreEqual(item.Token, returnValue);
				});
		}

		[Test]
		public void ItemsProcessing_Initialization_01()
		{
			ObservableCollection<Item> items = new ObservableCollection<Item>();

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
		}


		[Test, Combinatorial]
		public void ItemsProcessing_Remove(
			[Range(0, 4, 1)] int index)
		{
			Item[] sourceCollection = new[]
			{
				new Item(),
				new Item(),
				new Item(),
				new Item(),
				new Item()
			};

			ObservableCollection<Item> items = new ObservableCollection<Item>(
				sourceCollection);

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			items.RemoveAt(index);
			Assert.IsTrue(sourceCollection[index].ProcessedAsNew);
			Assert.IsTrue(sourceCollection[index].ProcessedAsOld);
		}

		[Test, Combinatorial]
		public void ItemsProcessing_Remove1()
		{
			Item item = new Item();
			ObservableCollection<Item> items = new ObservableCollection<Item>(
				new[]
				{
					item
				}
			);

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			items.RemoveAt(0);
			Assert.IsTrue(item.ProcessedAsNew);
			Assert.IsTrue(item.ProcessedAsOld);
		}

		[Test, Combinatorial]
		public void ItemsProcessing_Insert(
			[Range(0, 4, 1)] int index)
		{
			Item[] sourceCollection = new[]
			{
				new Item(),
				new Item(),
				new Item(),
				new Item(),
				new Item()
			};

			ObservableCollection<Item> items = new ObservableCollection<Item>(
				sourceCollection);

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			Item item = new Item();
			items.Insert(index, item);
			Assert.IsTrue(item.ProcessedAsNew);
			Assert.IsFalse(item.ProcessedAsOld);
		}

		[Test, Combinatorial]
		public void ItemsProcessing_Insert1(
			[Range(-1, 5)] int newValue)
		{
			ObservableCollection<Item> items = new ObservableCollection<Item>();

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			Item item = new Item();
			items.Insert(0, item);
			Assert.IsTrue(item.ProcessedAsNew);
			Assert.IsFalse(item.ProcessedAsOld);
		}

		[Test, Combinatorial]
		public void ItemsProcessing_Move(
			[Range(0, 4, 1)] int oldIndex,
			[Range(0, 4, 1)] int newIndex)
		{
			Item[] sourceCollection = new[]
			{
				new Item(),
				new Item(),
				new Item(),
				new Item(),
				new Item()
			};

			ObservableCollection<Item> items = new ObservableCollection<Item>(
				sourceCollection);

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			items.Move(oldIndex, newIndex);
			Assert.IsTrue(sourceCollection[oldIndex].ProcessedAsNew);
			Assert.IsFalse(sourceCollection[oldIndex].ProcessedAsOld);
		}

		[Test, Combinatorial]
		public void ItemsProcessing_Set(
			[Range(0, 4, 1)] int index)
		{
			Item[] sourceCollection = new[]
			{
				new Item(),
				new Item(),
				new Item(),
				new Item(),
				new Item()
			};

			ObservableCollection<Item> items = new ObservableCollection<Item>(
				sourceCollection);

			ItemsProcessing<Item, object> itemsProcessing = getItemsProcessing(items);
			items[index] = new Item();
			Assert.IsTrue(sourceCollection[index].ProcessedAsNew);
			Assert.IsTrue(sourceCollection[index].ProcessedAsOld);
			Assert.IsTrue(items[index].ProcessedAsNew);
			Assert.IsFalse(items[index].ProcessedAsOld);
		}	
	}
}