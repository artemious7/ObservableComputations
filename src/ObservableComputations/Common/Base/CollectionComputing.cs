﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using ObservableComputations;

namespace ObservableComputations
{
	public abstract class CollectionComputing<TItem> : ObservableCollectionWithChangeMarker<TItem>, ICollectionComputing
	{
		public string DebugTag {get; set;}
		public object Tag {get; set;}

		public CollectionComputing(int capacity = 0) : base(new List<TItem>(capacity))
		{
			_initialCapacity = capacity;

			if (Configuration.SaveInstantiatingStackTrace)
			{
				_instantiatingStackTrace = Environment.StackTrace;
			}
		}

		public event EventHandler PreCollectionChanged;
		public event EventHandler PostCollectionChanged;

		object _lockModifyChangeActionsKey;

		public void LockModifyChangeActions(object key)
		{
			if (key == null) throw new ArgumentNullException("key");

			if (_lockModifyChangeActionsKey == null)
				_lockModifyChangeActionsKey = key;
			else
				throw new ObservableComputationsException(this,
					"Modifying of change actions (InsertItemAction, RemoveItemAction, SetItemAction, MoveItemAction, ClearItemsAction)  is already locked. Unlock first.");
		}

		public void UnlockModifyChangeActions(object key)
		{
			if (key == null) throw new ArgumentNullException("key");

			if (_lockModifyChangeActionsKey == null)
				throw new ObservableComputationsException(this,
					"Modifying of change actions (InsertItemAction, RemoveItemAction, SetItemAction, MoveItemAction, ClearItemsAction) is not locked. Lock first.");

			if (_lockModifyChangeActionsKey == key)
				_lockModifyChangeActionsKey = null;
			else
				throw new ObservableComputationsException(this,
					"Wrong key to unlock modifying of change actions  (InsertItemAction, RemoveItemAction, SetItemAction, MoveItemAction, ClearItemsAction).");
		}


		private Action<int, TItem> _insertItemAction;
		public Action<int, TItem> InsertItemAction
		{
			// ReSharper disable once MemberCanBePrivate.Global
			get => _insertItemAction;
			set
			{
				if (_insertItemAction != value)
				{
					checkLockModifyChangeActions();

					_insertItemAction = value;
					OnPropertyChanged(Utils.InsertItemActionPropertyChangedEventArgs);
				}

			}
		}

		private void checkLockModifyChangeActions()
		{
			if (_lockModifyChangeActionsKey != null)
				throw new ObservableComputationsException(this,
					"Modifying of change actions  (InsertItemAction, RemoveItemAction, SetItemAction, MoveItemAction, ClearItemsAction) is locked. Unlock first.");
		}

		private Action<int> _removeItemAction;
		public Action<int> RemoveItemAction
		{
			// ReSharper disable once MemberCanBePrivate.Global
			get => _removeItemAction;
			set
			{
				if (_removeItemAction != value)
				{
					checkLockModifyChangeActions();

					_removeItemAction = value;
					OnPropertyChanged(Utils.RemoveItemActionPropertyChangedEventArgs);
				}
			}
		}

		private Action<int, TItem> _setItemAction;
		// ReSharper disable once MemberCanBePrivate.Global
		public Action<int, TItem> SetItemAction
		{
			get => _setItemAction;
			set
			{
				if (_setItemAction != value)
				{
					checkLockModifyChangeActions();

					_setItemAction = value;
					OnPropertyChanged(Utils.SetItemActionPropertyChangedEventArgs);
				}
			}
		}

		private Action<int, int> _moveItemAction;
		// ReSharper disable once MemberCanBePrivate.Global
		public Action<int, int> MoveItemAction
		{
			get => _moveItemAction;
			set
			{
				if (_moveItemAction != value)
				{
					checkLockModifyChangeActions();

					_moveItemAction = value;
					OnPropertyChanged(Utils.MoveItemActionPropertyChangedEventArgs);
				}
			}
		}

		private Action _clearItemsAction;

		// ReSharper disable once MemberCanBePrivate.Global
		public Action ClearItemsAction
		{
			get => _clearItemsAction;
			set
			{
				if (_clearItemsAction != value)
				{
					checkLockModifyChangeActions();

					_clearItemsAction = value;
					OnPropertyChanged(Utils.ClearItemsActionPropertyChangedEventArgs);
				}
			}
		}

		#region Overrides of ObservableCollection<TResult>
		protected override void InsertItem(int index, TItem item)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				_insertItemAction(index, item);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				
				return;
			}

			_insertItemAction(index, item);
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				_moveItemAction(oldIndex, newIndex);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				
				return;
			}

			_moveItemAction(oldIndex, newIndex);
		}

		protected override void RemoveItem(int index)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				_removeItemAction(index);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				
				return;
			}

			_removeItemAction(index);
		}

		protected override void SetItem(int index, TItem item)
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				_setItemAction(index, item);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				
				return;
			}

			_setItemAction(index, item);
		}

		protected override void ClearItems()
		{
			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				_clearItemsAction();

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
				
				return;
			}

			_clearItemsAction();
		}
		#endregion

		NotifyCollectionChangedAction? _currentChange;
		TItem _newItem;
		int _oldIndex = -1;
		int _newIndex = -1;

		public NotifyCollectionChangedAction? CurrentChange => _currentChange;
		public TItem NewItem => _newItem;
		public object NewItemObject => _newItem;
		public int OldIndex => _oldIndex;
		public int NewIndex => _newIndex;
	
		protected internal void baseInsertItem(int index, TItem item)
		{
			ChangeMarkerField = !ChangeMarkerField;

			_currentChange = NotifyCollectionChangedAction.Add;
			_newIndex = index;
			_newItem = item;

			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				PreCollectionChanged?.Invoke(this, null);
				base.InsertItem(index, item);
				PostCollectionChanged?.Invoke(this, null);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
			}
			else
			{
				PreCollectionChanged?.Invoke(this, null);
				base.InsertItem(index, item);
				PostCollectionChanged?.Invoke(this, null);				
			}

			_currentChange = null;
			_newIndex = -1;
			_newItem = default(TItem);
		}

		
		protected internal void baseMoveItem(int oldIndex, int newIndex)
		{
			ChangeMarkerField = !ChangeMarkerField;

			_currentChange = NotifyCollectionChangedAction.Move;
			_oldIndex = oldIndex;
			_newIndex = newIndex;

			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				PreCollectionChanged?.Invoke(this, null);
				base.MoveItem(oldIndex, newIndex);
				PostCollectionChanged?.Invoke(this, null);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
			}
			else
			{
				PreCollectionChanged?.Invoke(this, null);
				base.MoveItem(oldIndex, newIndex);
				PostCollectionChanged?.Invoke(this, null);
			}

			_currentChange = null;
			_oldIndex = -1;
			_newIndex = -1;
		}

		
		protected internal void baseRemoveItem(int index)
		{
			ChangeMarkerField = !ChangeMarkerField;

			_currentChange = NotifyCollectionChangedAction.Remove;
			_oldIndex = index;

			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				PreCollectionChanged?.Invoke(this, null);
				base.RemoveItem(index);
				PostCollectionChanged?.Invoke(this, null);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
			}
			else
			{
				PreCollectionChanged?.Invoke(this, null);
				base.RemoveItem(index);
				PostCollectionChanged?.Invoke(this, null);
			}

			_currentChange = null;
			_oldIndex = -1;
		}

		
		protected internal void baseSetItem(int index, TItem item)
		{
			ChangeMarkerField = !ChangeMarkerField;
			
			_currentChange = NotifyCollectionChangedAction.Replace;
			_newItem = item;
			_newIndex = index;

			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				PreCollectionChanged?.Invoke(this, null);
				base.SetItem(index, item);
				PostCollectionChanged?.Invoke(this, null);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
			}
			else
			{
				PreCollectionChanged?.Invoke(this, null);
				base.SetItem(index, item);
				PostCollectionChanged?.Invoke(this, null);
			}

			_currentChange = null;
			_newItem = default;
			_newIndex = -1;
		}

		
		protected internal void baseClearItems()
		{
			ChangeMarkerField = !ChangeMarkerField;

			_currentChange = NotifyCollectionChangedAction.Reset;

			if (Configuration.TrackComputingsExecutingUserCode)
			{
				Thread currentThread = Thread.CurrentThread;
				IComputing computing = DebugInfo._computingsExecutingUserCode.ContainsKey(currentThread) ? DebugInfo._computingsExecutingUserCode[currentThread] : null;
				DebugInfo._computingsExecutingUserCode[currentThread] = this;	
				
				PreCollectionChanged?.Invoke(this, null);
				base.ClearItems();
				PostCollectionChanged?.Invoke(this, null);

				if (computing == null) DebugInfo._computingsExecutingUserCode.Remove(currentThread);
				else DebugInfo._computingsExecutingUserCode[currentThread] = computing;
			}
			else
			{
				PreCollectionChanged?.Invoke(this, null);
				base.ClearItems();
				PostCollectionChanged?.Invoke(this, null);
			}

			_currentChange = null;
		}

		public Type ItemType => typeof(TItem);

		protected int _initialCapacity;

		// ReSharper disable once MemberCanBePrivate.Global
		public string InstantiatingStackTrace => _instantiatingStackTrace;

		protected bool _isConsistent = true;
		private readonly string _instantiatingStackTrace;
		public bool IsConsistent => _isConsistent;
		public event EventHandler ConsistencyRestored;

		protected void raiseConsistencyRestored()
		{
			ConsistencyRestored?.Invoke(this, null);
		}

		protected void checkConsistent()
		{
			if (!_isConsistent)
				throw new ObservableComputationsException(this,
					"The source collection has been changed. It is not possible to process this change, as the processing of the previous change is not completed. Make the change on ConsistencyRestored event raising (after IsConsistent property becomes true). This exception is fatal and cannot be handled as the inner state is damaged.");
		}
	}

}
