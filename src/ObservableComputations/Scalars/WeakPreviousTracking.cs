﻿using System;
using System.ComponentModel;

namespace ObservableComputations
{
	public class WeakPreviousTracking<TResult> : ScalarComputing<TResult>
		where TResult : class
	{
		public IReadScalar<TResult> Scalar => _scalar;
		public bool IsEverChanged => _isEverChanged;

		public bool TryGetPreviousValue(out TResult result)
		{
			if (_previousValueWeakReference == null)
			{
				result = null;
				return false;
			}

			return _previousValueWeakReference.TryGetTarget(out result);
		}

		private WeakReference<TResult> _previousValueWeakReference;
		private TResult _previousValue;
		private bool _isEverChanged;

		private IReadScalar<TResult> _scalar;


		[ObservableComputationsCall]
		public WeakPreviousTracking(
			IReadScalar<TResult> scalar)
		{
			_scalar = scalar;
		}

		private void handleScalarPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IReadScalar<TResult>.Value)) return;

			_handledEventSender = sender;
			_handledEventArgs = e;

			TResult newValue = _scalar.Value;
			_previousValue = _value;
			_previousValueWeakReference = new WeakReference<TResult>(_previousValue);

			if (!_isEverChanged)
			{
				_isEverChanged = true;
				raisePropertyChanged(Utils.IsEverChangedPropertyChangedEventArgs);
			}

			setValue(newValue);
			_previousValue = null;

			_handledEventSender = null;
			_handledEventArgs = null;
		}

        #region Overrides of ScalarComputing<TResult>

        protected override void initializeFromSource()
        {
        }

        protected override void initialize()
        {
            _scalar.PropertyChanged += handleScalarPropertyChanged;
            setValue(_scalar.Value);
        }

        protected override void uninitialize()
        {
            _scalar.PropertyChanged -= handleScalarPropertyChanged;
            _previousValue = default;
            raisePropertyChanged(Utils.PreviousValuePropertyChangedEventArgs);
            setValue(default);
            if (_isEverChanged)
            {
                _isEverChanged = false;
                raisePropertyChanged(Utils.IsEverChangedPropertyChangedEventArgs);
            }
        }

        internal override void addToUpstreamComputings(IComputingInternal computing)
        {
            (_scalar as IComputingInternal)?.AddDownstreamConsumedComputing(computing);
        }

        internal override void removeFromUpstreamComputings(IComputingInternal computing)
        {
            (_scalar as IComputingInternal)?.RemoveDownstreamConsumedComputing(computing);
        }

        #endregion

		~WeakPreviousTracking()
		{
			_scalar.PropertyChanged -= handleScalarPropertyChanged;
		}
	}
}