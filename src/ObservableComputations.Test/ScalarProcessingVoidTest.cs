﻿using NUnit.Framework;

namespace ObservableComputations.Test
{
	[TestFixture]
	public class ScalarProcessingVoidTest
	{
        Consumer consumer = new Consumer();

		public class Item
		{
			public bool ProcessedAsNew;
		}


		[Test]
		public void  ScalarProcessingVoid_Test()
		{
			Item item = new Item();
			Scalar<Item> itemScalar = new Scalar<Item>(item);
			itemScalar.ScalarProcessing((i, current) =>
			{
				i.ProcessedAsNew = true;
			}).IsNeededFor(consumer);
			Assert.IsTrue(item.ProcessedAsNew);
			item = new Item();
			itemScalar.Change(item);
			itemScalar.Change(item);
            consumer.Dispose();
		}
	}
}