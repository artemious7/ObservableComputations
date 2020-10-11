﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace ObservableComputations.Test
{
	[TestFixture]
	public class JoiningTests
	{
		public class Item : INotifyPropertyChanged
		{

			public Item(int id)
			{
				_id = id;
				Num = LastNum;
				LastNum++;
			}

			public static int LastNum;
			public int Num;

			private int _id;

			public int Id
			{
				get { return _id; }
				set { updatePropertyValue(ref _id, value); }
			}

			#region INotifyPropertyChanged imlementation

			public event PropertyChangedEventHandler PropertyChanged;

			protected virtual void onPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
				if (onPropertyChanged != null) onPropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}

			protected bool updatePropertyValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
			{
				if (EqualityComparer<T>.Default.Equals(field, value)) return false;
				field = value;
				this.onPropertyChanged(propertyName);
				return true;
			}

			#endregion

			public override string ToString()
			{
				return $"Num={Num}  Id={Id}";
			}
		}

		TextFileOutput _textFileOutputLog = new TextFileOutput(@"D:\Projects\NevaPolimer\Joining_Deep.log");
		TextFileOutput _textFileOutputTime = new TextFileOutput(@"D:\Projects\NevaPolimer\Joining_Deep_Time.log");

		[Test]
		public void Joining_Deep()
		{			
			test(new int[0], new int[0]);

			for (int v1 = -1; v1 <= 3; v1++)
			{
				for (int v2 = -1; v2 <= 3; v2++)
				{
                    test(new []{v1, v2}, new int[0]);
                    test(new int[0], new []{v1, v2});
					for (int v3 = -1; v3 <= 3; v3++)
					{
						for (int v4 = -1; v4 <= 3; v4++)
						{
							test(new []{v1, v2, v3, v4}, new []{v1, v2, v3, v4});
                            test(new []{v1, v2, v3, v4}, new []{v1, v2});
                            test(new []{v1, v2}, new []{v1, v2, v3, v4});
						}
					}
				}
			}
		}

		private void test(int[] ids1, int[] ids2)
		{
			string testNum = string.Empty;
			int index = 0;
			int indexOld = 0;
			int indexNew = 0;
			int index1 = 0;
			int newItemId = 0;
			ObservableCollection<Item> items1;
			ObservableCollection<Item> items2;
			Joining<Item, Item> joining;

			try
			{
				trace(testNum = "1", ids1, ids2, newItemId, index, indexOld, indexNew);
				items1 = getObservableCollection(ids1);
				items2 = getObservableCollection(ids2);
                Consumer consumer1 = new Consumer();
				joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer1);
				joining.ValidateConsistency();
                consumer1.Dispose();

				for (index = 0; index < ids1.Length; index++)
				{
					trace(testNum = "2", ids1, ids2, newItemId, index, indexOld, indexNew);
					items1 = getObservableCollection(ids1);
					items2 = getObservableCollection(ids2);
                    Consumer consumer2 = new Consumer();
					joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer2);
					items1.RemoveAt(index);
					joining.ValidateConsistency();
                    consumer2.Dispose();
				}

				for (index = 0; index < ids2.Length; index++)
				{
					trace(testNum = "3", ids1, ids2, newItemId, index, indexOld, indexNew);
					items1 = getObservableCollection(ids1);
					items2 = getObservableCollection(ids2);
                    Consumer consumer3 = new Consumer();
					joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer3);
					items2.RemoveAt(index);
					joining.ValidateConsistency();
                    consumer3.Dispose();
				}

				for (index = 0; index <= ids1.Length; index++)
				{
					for (newItemId = 0; newItemId <= ids1.Length; newItemId++)
					{
						trace(testNum = "4", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer4 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer4);
						items1.Insert(index, new Item(newItemId));
						joining.ValidateConsistency();
                        consumer4.Dispose();
					}

				}

				for (index = 0; index <= ids2.Length; index++)
				{
					for (newItemId = 0; newItemId <= ids2.Length; newItemId++)
					{
						trace(testNum = "5", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer5 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer5);
						items2.Insert(index, new Item(newItemId));
						joining.ValidateConsistency();
                        consumer5.Dispose();
					}
				}

				for (index = 0; index < ids1.Length; index++)
				{
					for (newItemId = 0; newItemId <= ids1.Length; newItemId++)
					{
						trace(testNum = "6", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer6 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer6);
						items1[index] = new Item(newItemId);
						joining.ValidateConsistency();
                        consumer6.Dispose();
					}

                    for (newItemId = 0; newItemId <= ids1.Length; newItemId++)
                    {
                        trace(testNum = "10", ids1, ids2, newItemId, index, indexOld, indexNew);
                        items1 = getObservableCollection(ids1);
                        if (items1[index] == null) continue;                      
                        items2 = getObservableCollection(ids2);
                        Consumer consumer10 = new Consumer();
                        joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer10);
                        items1[index].Id = newItemId;
                        joining.ValidateConsistency();
                        consumer10.Dispose();
                    }
				}

				for (index = 0; index < ids2.Length; index++)
				{
					for (newItemId = 0; newItemId <= ids2.Length; newItemId++)
					{
						trace(testNum = "7", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer7 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer7);
						items2[index] = new Item(newItemId);
						joining.ValidateConsistency();
                        consumer7.Dispose();
					}

                    for (newItemId = 0; newItemId <= ids2.Length; newItemId++)
                    {
                        trace(testNum = "11", ids1, ids2, newItemId, index, indexOld, indexNew);
                        items1 = getObservableCollection(ids1);
                        items2 = getObservableCollection(ids2);
                        if (items2[index] == null) continue; 
                        Consumer consumer11 = new Consumer();
                        joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer11);
                        items2[index].Id = newItemId;
                        joining.ValidateConsistency();
                        consumer11.Dispose();
                    }
				}

				for (indexOld = 0; indexOld < ids1.Length; indexOld++)
				{
					for (indexNew = 0; indexNew < ids1.Length; indexNew++)
					{
						trace(testNum = "8", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer8 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer8);
						items1.Move(indexOld, indexNew);
						joining.ValidateConsistency();
                        consumer8.Dispose();
					}
				}

				for (indexOld = 0; indexOld < ids2.Length; indexOld++)
				{
					for (indexNew = 0; indexNew < ids2.Length; indexNew++)
					{
						trace(testNum = "9", ids1, ids2, newItemId, index, indexOld, indexNew);
						items1 = getObservableCollection(ids1);
						items2 = getObservableCollection(ids2);
                        Consumer consumer9 = new Consumer();
						joining = items1.Joining(items2, (item1, item2) => item1 != null && item2 != null && item1.Id == item2.Id).IsNeededFor(consumer9);
						items2.Move(indexOld, indexNew);
						joining.ValidateConsistency();
                        consumer9.Dispose();
					}
				}

			}
			catch (Exception e)
			{
				string traceString = getTraceString( 
					testNum, ids1, ids2, newItemId, index, indexOld, indexNew);
				_textFileOutputLog.AppentLine(traceString);
				_textFileOutputLog.AppentLine(e.Message);
				_textFileOutputLog.AppentLine(e.StackTrace);
				throw new Exception(traceString, e);
			}

		}

		private void trace(string num, int[] ids1, int[] ids2, int newId, int index, int indexOld, int indexNew)
		{
			string traceString = getTraceString(num, ids1, ids2, newId, index, indexOld, indexNew);
			if (traceString == "#6. ids1=-1,0  ids2=-1,0  index=1  newId=0  indexOld=0  indexNew=0")
			{
				Debugger.Break();
			}
		}

		private static string getTraceString(string num, int[] ids1, int[] ids2, int newId, int index, int indexOld, int indexNew)
		{
			return string.Format(
				"#{0}. ids1={1}  ids2={2}  index={3}  newId={6}  indexOld={4}  indexNew={5}",
				num,
				string.Join(",", ids1),
				string.Join(",", ids2),
				index,
				indexOld,
				indexNew,
				newId);
		}


		private static ObservableCollection<Item> getObservableCollection(int[] ids)
		{	
			return new ObservableCollection<Item>(Enumerable.Range(0, ids.Length).Select(i => ids[i] >= 0 ? new Item(ids[i]) : null));
		}


		public class Order : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public int Num {get; set;}

			private decimal _price;
			public decimal Price
			{
				get => _price;
				set
				{
					_price = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price)));
				}
			}

			public Order(int num, decimal price)
			{
				Num = num;
				_price = price;
			}


			public Order(int num, decimal price, string type)
			{
				Num = num;
				_price = price;
				_type = type;
			}

			private string _type;
			public string Type
			{
				get => _type;
				set
				{
					_type = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
				}
			}
		}

		[Test]
		public void Joining_Test1()
		{			
			ObservableCollection<Order> orders = 
				new ObservableCollection<Order>(new []
				{
					new Order(1, 15, "A"),
					new Order(2, 15, "A"),
					new Order(3, 25, "B"),
					new Order(4, 27, "C"),
					new Order(5, 30, "A"),
					new Order(6, 75, "C"),
					new Order(7, 80, "A"),
				});

			ObservableCollection<string> selectedOrderTypes = new ObservableCollection<string>(){"A", "B"};

			ObservableCollection<Order> filteredOrders1 = orders
				.Joining(selectedOrderTypes, (o, ot) => o.Type.Equals(ot))
				.Selecting(oot => oot.LeftItem);

			selectedOrderTypes.Add("C");
			orders[0].Type = "C";		
		}
	}
}