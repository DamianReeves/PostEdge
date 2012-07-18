namespace PostEdge.Reactive.ChangeTracking {
    using System;
    using System.ComponentModel;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using ReactiveUI;

    public class ChangeTracker: IChangeTracker, INotifyPropertyChanged, INotifyPropertyChanging {
        private bool _isChanged;
        long _changeCountSuppressed = 0;

        readonly ISubject<IObservedChange<object, object>> _changed =
            new ScheduledSubject<IObservedChange<object, object>>(RxApp.DeferredScheduler);

        public event EventHandler AcceptedChanges;
        public event CancelEventHandler AcceptingChanges;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;


        public ChangeTracker(INotifyPropertyChanged target) {
            if (target == null) throw new ArgumentNullException("target");
            var reactiveTarget = target as IReactiveNotifyPropertyChanged ?? new MakeObjectReactiveHelper(target);
            this.Target = reactiveTarget;
            //_changed.OnNext();
            this.Target.Changed.Subscribe(x => this.MarkAsChanged(x.PropertyName,x.GetValue()));
        }

        public void AcceptChanges() {
            var cancelArgs = new CancelEventArgs();
            this.OnAcceptingChanges(cancelArgs);
            if(cancelArgs.Cancel) return;
            this.IsChanged = false;
            this.OnAcceptedChanges(EventArgs.Empty);
        }        

        public bool IsChanged {
            get { return this._isChanged; } 
            private set {
                if (this._isChanged != value) {
                    this.RaisePropertyChanging("IsChanged");
                    this._isChanged = value;
                    this.RaisePropertyChanged("IsChanged");
                }
            }
        }

        public IObservable<IObservedChange<object, object>> Changed {
#if SILVERLIGHT
            get { return _Changed.Where(_ => _changeCountSuppressed == 0); }
#else
            get { return this._changed.Where(_ => Interlocked.Read(ref this._changeCountSuppressed) == 0); }
#endif
        }

        public IDisposable SuppressChangeNotifications() {
            Interlocked.Increment(ref this._changeCountSuppressed);
            return Disposable.Create(() => Interlocked.Decrement(ref this._changeCountSuppressed));
        }

        public IReactiveNotifyPropertyChanged Target { get; private set; }

        public virtual void MarkAsChanged<TValue>(IObservedChange<object, TValue> change) {

            this.IsChanged = true;
        }

        protected virtual void OnAcceptingChanges(CancelEventArgs args) {
            var handler = this.AcceptingChanges;
            if (handler != null) handler(this, args);
        }

        protected virtual void OnAcceptedChanges(EventArgs args) {
            var handler = this.AcceptedChanges;
            if (handler != null) handler(this, args);
        }
        protected virtual void RaisePropertyChanged(string propertyName) {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, args);
        }        

        protected virtual void RaisePropertyChanging(string propertyName) {
            this.OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs args) {
            var handler = this.PropertyChanging;
            if (handler != null) handler(this, args);
        }
    }
}